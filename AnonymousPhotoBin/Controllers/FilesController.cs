using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using AnonymousPhotoBin.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.IO.Compression;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace AnonymousPhotoBin.Controllers {
    [Authorize("SingletonAdmin")]
    public class FilesController : Controller {
        private static readonly SHA256 SHA256 = SHA256.Create();
        private static readonly int MAX_WIDTH = 320;
        private static readonly int MAX_HEIGHT = 160;

        private readonly StorageAccountCredentials _storageAccountCredentials;
        private readonly PhotoBinDbContext _context;

        public FilesController(PhotoBinDbContext context, StorageAccountCredentials storageAccountCredentials) {
            _storageAccountCredentials = storageAccountCredentials;
            _context = context;
        }

        [HttpGet]
        [Route("api/files")]
        public IAsyncEnumerable<FileMetadata> Get() {
            return _context.FileMetadata.AsAsyncEnumerable();
        }

        private async Task<BlobContainerClient> GetBlobContainerClientAsync() {
            var blobServiceClient = new BlobServiceClient(_storageAccountCredentials.ConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("anonymous-photo-bin");
            await blobContainerClient.CreateIfNotExistsAsync();
            await blobContainerClient.SetAccessPolicyAsync(PublicAccessType.Blob);
            return blobContainerClient;
        }

        [HttpGet]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Get(Guid id) {
            var blobContainerClient = await GetBlobContainerClientAsync();
            var blobClient = blobContainerClient.GetBlobClient($"full-{id}");
            return Redirect(blobClient.Uri.AbsoluteUri);
        }

        [HttpGet]
        [Route("api/thumbnails/{id}")]
        public async Task<IActionResult> GetThumbnail(Guid id) {
            var blobContainerClient = await GetBlobContainerClientAsync();
            var blobClient = blobContainerClient.GetBlobClient($"thumb-{id}");
            return Redirect(blobClient.Uri.AbsoluteUri);
        }

        [HttpPost]
        [Route("api/files/zip")]
        public async Task<IActionResult> Zip(string ids, bool? compressed, CancellationToken token) {
            Response.ContentType = "application/zip";
            Response.Headers.Add("Content-Disposition", $"attachment;filename=photobin_{DateTime.UtcNow:yyyyMMdd-hhmmss}.zip");

            IEnumerable<Guid> guids = ids
                .Split('\r', '\n', ',', ';')
                .Where(s => s != "")
                .Select(s => Guid.Parse(s));
            var fileMetadata = await _context.FileMetadata
                .Where(f => guids.Contains(f.FileMetadataId))
                .ToListAsync(token);

            var compressionLevel = compressed == true
                ? CompressionLevel.Optimal
                : CompressionLevel.NoCompression;

            if (compressionLevel == CompressionLevel.NoCompression) {
                // The size of a .zip file with no compression can be determined from just the names and sizes of the files.
                using var s = new LengthStream();
                using (var archive = new ZipArchive(s, ZipArchiveMode.Create, true)) {
                    foreach (var file in fileMetadata) {
                        token.ThrowIfCancellationRequested();
                        byte[] data = new byte[file.Size];

                        var entry = archive.CreateEntry(file.NewFilename, compressionLevel);
                        entry.LastWriteTime = file.UploadedAt;
                        using var zipStream = entry.Open();
                        await zipStream.WriteAsync(data, token);
                    }
                }
                Response.Headers.Add("Content-Length", s.Position.ToString());
            }

            using (var s = Response.Body) {
                using var archive = new ZipArchive(s, ZipArchiveMode.Create, true);
                foreach (var file in fileMetadata) {
                    token.ThrowIfCancellationRequested();

                    var container = await GetBlobContainerClientAsync();
                    var full = container.GetBlobClient($"full-{file.FileMetadataId}");
                    if (!await full.ExistsAsync(token)) {
                        throw new Exception($"{file.FileMetadataId} not found");
                    }

                    var streamingDownloadResponse = await full.DownloadStreamingAsync(cancellationToken: token);
                    using var streamingDownload = streamingDownloadResponse.Value;

                    var entry = archive.CreateEntry(file.NewFilename, compressionLevel);
                    entry.LastWriteTime = file.UploadedAt;
                    using var zipStream = entry.Open();
                    await streamingDownload.Content.CopyToAsync(zipStream, token);
                }
            }

            return new EmptyResult();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/files")]
        [RequestSizeLimit(105000000)]
        public async IAsyncEnumerable<FileMetadata> Post(List<IFormFile> files, string? userName = null, string? category = null) {
            foreach (var file in files) {
                using var ms = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(ms);
                byte[] data = ms.ToArray();

                byte[] hash = SHA256.ComputeHash(data);
                var existing = await _context.FileMetadata.FirstOrDefaultAsync(f2 => f2.Sha256 == hash);
                if (existing != null) {
                    existing.UserName = userName ?? existing.UserName;
                    existing.Category = category ?? existing.Category;
                    await _context.SaveChangesAsync();
                    yield return existing;
                } else {
                    int? width = null;
                    int? height = null;
                    string? takenAt = null;
                    (byte[], string)? thumbnail = null;
                    try {
                        using var image = Image.Load(data);

                        width = image.Width;
                        height = image.Height;
                        if (image.Metadata?.ExifProfile is ExifProfile exif && exif.TryGetValue(ExifTag.DateTimeOriginal, out IExifValue<string>? dateTimeStr)) {
                            takenAt = dateTimeStr?.Value;
                        }

                        if (data.Length <= 1024 * 16) {
                            thumbnail = (data, file.ContentType);
                        } else {
                            image.Mutate(x => x.AutoOrient());

                            double ratio = (double)image.Width / image.Height;
                            int newW, newH;
                            if (ratio > (double)MAX_WIDTH / MAX_HEIGHT) {
                                // wider
                                newW = MAX_WIDTH;
                                newH = (int)(newW / ratio);
                            } else {
                                // taller
                                newH = MAX_HEIGHT;
                                newW = (int)(newH * ratio);
                            }
                            image.Mutate(x => x.Resize(newW, newH));

                            using var jpegThumb = new MemoryStream();
                            image.SaveAsJpeg(jpegThumb);
                            thumbnail = (jpegThumb.ToArray(), "image/jpeg");
                        }
                    } catch (NotSupportedException) { }

                    DateTime? takenAtDateTime = null;
                    if (DateTime.TryParseExact(takenAt, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.AssumeLocal, out DateTime dt)) {
                        takenAtDateTime = dt;
                    }

                    FileMetadata f = new() {
                        Width = width,
                        Height = height,
                        TakenAt = takenAtDateTime,
                        UploadedAt = DateTime.UtcNow,
                        OriginalFilename = Path.GetFileName(file.FileName),
                        UserName = userName,
                        Category = category,
                        Size = data.Length,
                        Sha256 = hash,
                        ContentType = file.ContentType
                    };
                    _context.FileMetadata.Add(f);
                    await _context.SaveChangesAsync();

                    var container = await GetBlobContainerClientAsync();
                    if (thumbnail is (byte[] thumbnailData, string thumbnailType)) {
                        var thumb = container.GetBlobClient($"thumb-{f.FileMetadataId}");
                        await thumb.UploadAsync(new BinaryData(thumbnailData), new BlobUploadOptions {
                            HttpHeaders = new BlobHttpHeaders {
                                ContentType = thumbnailType
                            }
                        });
                    }
                    var full = container.GetBlobClient($"full-{f.FileMetadataId}");
                    await full.UploadAsync(new BinaryData(data), new BlobUploadOptions {
                        HttpHeaders = new BlobHttpHeaders {
                            ContentType = file.ContentType
                        }
                    });

                    yield return f;
                }
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/files/legacy")]
        public async Task<IActionResult> LegacyPost(List<IFormFile> files, string? userName = null, string? category = null) {
            Response.StatusCode = 200;
            Response.ContentType = "text/html";

            using var sw = new StreamWriter(Response.Body);
            await sw.WriteLineAsync("Files uploaded:");
            await sw.WriteLineAsync("");

            await foreach (var f in Post(files, userName, category)) {
                await sw.WriteLineAsync(WebUtility.HtmlEncode($"* {f.OriginalFilename} -> {f.Url}"));
            }

            await sw.WriteLineAsync("");
            await sw.WriteLineAsync("Upload complete.");

            return new EmptyResult();
        }

        public class FileMetadataPatch {
            public string? UserName { get; set; }
            public string? Category { get; set; }
        }

        [HttpPatch]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody]FileMetadataPatch patch) {
            var f = await _context.FileMetadata.FirstOrDefaultAsync(p => p.FileMetadataId == id);
            if (f == null) {
                return NotFound();
            } else {
                // Null in patch means field was not included.
                if (patch.UserName != null) f.UserName = patch.UserName;
                if (patch.Category != null) f.Category = patch.Category;
                // Database should store null instead of empty string for consistency.
                if (f.UserName == "") f.UserName = null;
                if (f.Category == "") f.Category = null;
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

        [HttpDelete]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Delete(Guid id) {
            var f = await _context.FileMetadata.FirstOrDefaultAsync(p => p.FileMetadataId == id);
            if (f == null) {
                return NotFound();
            } else {
                var container = await GetBlobContainerClientAsync();
                var full = container.GetBlobClient($"full-{id}");
                await full.DeleteIfExistsAsync();
                var thumb = container.GetBlobClient($"thumb-{id}");
                await thumb.DeleteIfExistsAsync();

                _context.FileMetadata.Remove(f);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }
    }
}
