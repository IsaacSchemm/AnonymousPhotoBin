﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Globalization;
using AnonymousPhotoBin.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Threading;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net;

namespace AnonymousPhotoBin.Controllers {
    public class FilesController : Controller {
        private static readonly SHA256 SHA256 = SHA256.Create();
        private static readonly int MAX_WIDTH = 320;
        private static readonly int MAX_HEIGHT = 160;

        private readonly IConfiguration _configuration;
        private readonly PhotoBinDbContext _context;

        public FilesController(PhotoBinDbContext context, IConfiguration configuration) {
            _context = context;
            _configuration = configuration;
        }
        
        [HttpGet]
        [Route("api/files")]
        public async Task<IActionResult> Get() {
            var r = BadRequestIfPasswordInvalid();
            if (r != null) return r;

            return Ok(await _context.FileMetadata.ToListAsync());
        }

        private async Task<BlobContainerClient> GetBlobContainerClientAsync() {
            var blobServiceClient = new BlobServiceClient(_configuration["ConnectionStrings:AzureStorageConnectionString"]);
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

        [HttpPost]
        [Route("api/files")]
        [RequestSizeLimit(105000000)]
        public async IAsyncEnumerable<FileMetadata> Post(List<IFormFile> files, string userName = null, string category = null) {
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
                    string takenAt = null;
                    (byte[], string)? thumbnail = null;
                    try {
                        using var image = Image.Load(data);

                        width = image.Width;
                        height = image.Height;
                        if (image.Metadata?.ExifProfile is ExifProfile exif && exif.TryGetValue(ExifTag.DateTimeOriginal, out IExifValue<string> dateTimeStr)) {
                            takenAt = dateTimeStr.Value;
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

        [HttpPost]
        [Route("api/files/legacy")]
        public async Task<IActionResult> LegacyPost(List<IFormFile> files, string userName = null, string category = null) {
            Response.StatusCode = 200;
            Response.ContentType = "text/html";

            using var sw = new StreamWriter(Response.Body);
            await sw.WriteLineAsync("Files uploaded:");
            await foreach (var f in Post(files, userName, category)) {
                await sw.WriteLineAsync(WebUtility.HtmlEncode($"{f.OriginalFilename} -> {f.Url}"));
            }

            await sw.WriteLineAsync("Upload complete.");

            return new EmptyResult();
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

        [HttpPatch]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody]FileMetadataPatch patch) {
            var r = BadRequestIfPasswordInvalid();
            if (r != null) return r;

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
            var r = BadRequestIfPasswordInvalid();
            if (r != null) return r;

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
