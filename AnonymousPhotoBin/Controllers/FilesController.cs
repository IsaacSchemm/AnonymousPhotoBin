using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Threading;
using SixLabors.ImageSharp.Processing;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;
using AnonymousPhotoBin.Storage;

namespace AnonymousPhotoBin.Controllers {
    public class FilesController : Controller {
        private static int MAX_WIDTH = 320;
        private static int MAX_HEIGHT = 160;

        private readonly IConfiguration _configuration;

        public FilesController(IConfiguration configuration) {
            _configuration = configuration;
        }
        
        [HttpGet]
        [Route("api/files")]
        public async Task<IActionResult> Get(int? limit = null) {
            var r = BadRequestIfPasswordInvalid();
            if (r != null) return r;

            var container = await GetCloudBlobContainerAsync();
            return Ok(await PhotoAccess.GetExistingPhotosAsync(container, limit ?? int.MaxValue));
        }

        private async Task<CloudBlobContainer> GetCloudBlobContainerAsync() {
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(_configuration["ConnectionStrings:AzureStorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("anonymous-photo-bin");
            await container.CreateIfNotExistsAsync();
            await container.SetPermissionsAsync(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            });
            return container;
        }

        [HttpGet]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Get(Guid id, string filename = null) {
            //var r = BadRequestIfPasswordInvalid();
            //if (r != null) return r;

            var container = await GetCloudBlobContainerAsync();
            CloudBlockBlob full = container.GetBlockBlobReference($"full-{id}");
            return Redirect(full.Uri.AbsoluteUri);
        }

        [HttpGet]
        [Route("api/thumbnails/{id}")]
        public async Task<IActionResult> GetThumbnail(Guid id) {
            //var r = BadRequestIfPasswordInvalid();
            //if (r != null) return r;

            var container = await GetCloudBlobContainerAsync();
            CloudBlockBlob thumb = container.GetBlockBlobReference($"thumb-{id}");
            return Redirect(thumb.Uri.AbsoluteUri);
        }

        [HttpPost]
        [Route("api/files/zip")]
        public async Task<IActionResult> Zip(string ids, bool? compressed, CancellationToken token) {
            //var r = BadRequestIfPasswordInvalid();
            //if (r != null) return r;

            Response.ContentType = "application/zip";
            Response.Headers.Add("Content-Disposition", $"attachment;filename=photobin_{DateTime.UtcNow.ToString("yyyyMMdd-hhmmss")}.zip");

            var container = await GetCloudBlobContainerAsync();
            IEnumerable<Guid> guids = ids.Split('\r', '\n', ',', ';').Where(s => s != "").Select(s => Guid.Parse(s));
            ExistingPhoto[] fileMetadata = await PhotoAccess.GetExistingPhotosByIdsAsync(container, guids);

            var compressionLevel = compressed == true
                ? CompressionLevel.Optimal
                : CompressionLevel.NoCompression;

            if (compressionLevel == CompressionLevel.NoCompression) {
                // The size of a .zip file with no compression can be determined from just the names and sizes of the files.
                using (var s = new LengthStream()) {
                    using (var archive = new ZipArchive(s, ZipArchiveMode.Create, true)) {
                        foreach (var file in fileMetadata) {
                        token.ThrowIfCancellationRequested();
                            byte[] data = new byte[file.Size];

                            var entry = archive.CreateEntry(file.NewFilename, compressionLevel);
                            entry.LastWriteTime = file.UploadedAt ?? DateTimeOffset.UtcNow;
                            using (var zipStream = entry.Open()) {
                                await zipStream.WriteAsync(data, 0, data.Length);
                            }
                        }
                    }
                    Response.Headers.Add("Content-Length", s.Position.ToString());
                }
            }

            using (var s = Response.Body) {
                using (var archive = new ZipArchive(s, ZipArchiveMode.Create, true)) {
                    foreach (var file in fileMetadata) {
                        token.ThrowIfCancellationRequested();

                        CloudBlockBlob full = container.GetBlockBlobReference($"full-{file.Id}");
                        if (!await full.ExistsAsync())
                        {
                            throw new Exception($"{file.Id} not found");
                        }


                        var entry = archive.CreateEntry(file.NewFilename, compressionLevel);
                        entry.LastWriteTime = file.UploadedAt ?? DateTimeOffset.UtcNow;
                        byte[] data = new byte[1024 * 1024 * 8];
                        using (var zipStream = entry.Open())
                        {
                            for (int offset = 0; offset < file.Size; offset += data.Length)
                            {
                                int length = Math.Min(data.Length, checked((int)file.Size - offset));

                                await full.DownloadRangeToByteArrayAsync(data, 0, offset, length);
                                await zipStream.WriteAsync(data, 0, length);
                            }
                        }
                    }
                }
            }

            return new EmptyResult();
        }
        
        [HttpPost]
        [Route("api/files")]
        [RequestSizeLimit(105000000)]
        public async Task<List<ExistingPhoto>> Post(List<IFormFile> files, string userName = null, string category = null) {
            List<ExistingPhoto> l = new List<ExistingPhoto>();
            foreach (var file in files) {
                using (var ms = new MemoryStream()) {
                    await file.OpenReadStream().CopyToAsync(ms);
                    byte[] data = ms.ToArray();

                    /*byte[] hash = SHA256.ComputeHash(data);
                    var existing = await _context.FileMetadata.FirstOrDefaultAsync(f2 => f2.Sha256 == hash);
                    if (existing != null) {
                        existing.UserName = userName ?? existing.UserName;
                        existing.Category = category ?? existing.Category;
                        await _context.SaveChangesAsync();
                        l.Add(existing);
                    } else*/ {
                        int? width = null;
                        int? height = null;
                        string takenAt = null;
                        byte[] thumbnail = null;
                        try {
                            using (var image = Image.Load(data)) {
                                width = image.Width;
                                height = image.Height;
                                takenAt = image.MetaData?.ExifProfile?.GetValue(ExifTag.DateTimeOriginal)?.Value?.ToString();

                                if (data.Length > 1024 * 16) {
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

                                    using (var jpegThumb = new MemoryStream()) {
                                        image.SaveAsJpeg(jpegThumb);
                                        thumbnail = jpegThumb.ToArray();
                                    }
                                }
                            }
                        } catch (NotSupportedException) { }

                        DateTime? takenAtDateTime = null;
                        if (DateTime.TryParseExact(takenAt, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.AssumeUniversal, out DateTime dt)) {
                            takenAtDateTime = dt;
                        }

                        var container = await GetCloudBlobContainerAsync();
                        var f = await PhotoAccess.UploadPhotoAsync(container, new NewPhoto
                        {
                            Photo = new DataAndContentType(data, file.ContentType),
                            Thumbnail = new DataAndContentType(thumbnail, "image/jpeg"),
                            TakenAt = takenAtDateTime,
                            OriginalFilename = Path.GetFileName(file.FileName),
                            UserName = userName,
                            Category = category
                        });
                        if (thumbnail != null)
                        {
                            CloudBlockBlob thumb = container.GetBlockBlobReference($"thumb-{f.Id}");
                            thumb.Properties.ContentType = "image/jpeg";
                            await thumb.UploadFromByteArrayAsync(thumbnail, 0, thumbnail.Length);
                        }
                        CloudBlockBlob full = container.GetBlockBlobReference($"full-{f.Id}");
                        full.Properties.ContentType = file.ContentType;
                        await full.UploadFromByteArrayAsync(data, 0, data.Length);

                        l.Add(f);
                    }
                }
            }
            return l;
        }

        [HttpPost]
        [Route("api/files/legacy")]
        public async Task<ContentResult> LegacyPost(List<IFormFile> files, string userName = null, string category = null) {
            var l = await Post(files, userName, category);
            return Content("Files uploaded:\n" + string.Join("\n", l.Select(f => $"{f.OriginalFilename} -> {f.Id}")));
        }

        private IActionResult BadRequestIfPasswordInvalid() {
            string password = _configuration["FileManagementPassword"];
            if (password == null) {
                return BadRequest("FileManagementPassword is not set. Please set in appsettings.json, user secrets file, or Azure web app settings.");
            }
            if (!Request.Headers["X-FileManagementPassword"].Contains(password)) {
                return BadRequest("X-FileManagementPassword header is incorrect or missing.");
            }

            return null;
        }

        [HttpGet]
        [Route("api/password/check")]
        public IActionResult CheckPassword() {
            return BadRequestIfPasswordInvalid() ?? NoContent();
        }

        public class FileMetadataPatch {
            public string UserName { get; set; }
            public string Category { get; set; }
        }

        [HttpDelete]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Delete(Guid id) {
            var r = BadRequestIfPasswordInvalid();
            if (r != null) return r;

            var container = await GetCloudBlobContainerAsync();
            await PhotoAccess.DeletePhotoAsync(container, id);
            return NoContent();
        }
    }
}
