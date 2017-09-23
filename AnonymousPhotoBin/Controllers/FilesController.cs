using System;
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
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace AnonymousPhotoBin.Controllers {
    public class FilesController : Controller {
        private static SHA256 SHA256 = SHA256.Create();
        private static int MAX_WIDTH = 320;
        private static int MAX_HEIGHT = 160;

        private readonly IConfiguration _configuration;
        private readonly PhotoBinDbContext _context;

        public FilesController(PhotoBinDbContext context, IConfiguration configuration) {
            _context = context;
            _configuration = configuration;

            _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(70));
        }
        
        [HttpGet]
        [Route("api/files")]
        public async Task<IEnumerable<FileMetadata>> Get() {
            return await _context.FileMetadata.ToListAsync();
        }

        [HttpGet]
        [Route("api/files/{id}")]
        [Route("api/files/{id}/{filename}")]
        public async Task<IActionResult> Get(Guid id, string filename = null) {
            var photo = await _context.FileMetadata.Include(nameof(FileMetadata.FileData)).FirstOrDefaultAsync(p => p.FileMetadataId == id);
            if (photo == null) {
                return NotFound();
            } else {
                return File(photo.FileData.Data, photo.ContentType);
            }
        }

        [HttpGet]
        [Route("api/thumbnails/{id}")]
        public async Task<IActionResult> GetThumbnail(Guid id) {
            var photo = await _context.FileMetadata.Include(nameof(FileMetadata.JpegThumbnail)).FirstOrDefaultAsync(p => p.FileMetadataId == id);
            if (photo == null) {
                return NotFound();
            } else if (photo.JpegThumbnail == null) {
                return photo.ContentType.StartsWith("image/")
                    ? await Get(id)
                    : Redirect("/images/blank.gif");
            } else {
                return File(photo.JpegThumbnail.Data, "image/jpeg");
            }
        }

        [HttpPost]
        [Route("api/files/zip")]
        public async Task<IActionResult> Zip(string ids) {
            IEnumerable<Guid> guids = ids.Split('\r', '\n', ',', ';').Where(s => s != "").Select(s => Guid.Parse(s));
            using (var ms = new MemoryStream()) {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true)) {
                    foreach (Guid id in guids) {
                        var file = await _context.FileMetadata.Include(nameof(FileMetadata.FileData)).FirstOrDefaultAsync(f => f.FileMetadataId == id);
                        if (file == null) continue;

                        var entry = archive.CreateEntry(file.NewFilename, CompressionLevel.NoCompression);
                        entry.LastWriteTime = file.UploadedAt;
                        using (var zipStream = entry.Open()) {
                            await zipStream.WriteAsync(file.FileData.Data, 0, file.FileData.Data.Length);
                        }
                    }
                }
                return File(ms.ToArray(), "application/zip", $"photobin_{DateTime.UtcNow.ToString("yyyyMMdd-hhmmss")}.zip");
            }
        }
        
        [HttpPost]
        [Route("api/files")]
        [RequestSizeLimit(209715200)]
        public async Task<List<FileMetadata>> Post(List<IFormFile> files, string userName = null, string category = null) {
            List<FileMetadata> l = new List<FileMetadata>();
            foreach (var file in files) {
                using (var ms = new MemoryStream()) {
                    await file.OpenReadStream().CopyToAsync(ms);
                    byte[] data = ms.ToArray();

                    byte[] hash = SHA256.ComputeHash(data);
                    var existing = await _context.FileMetadata.FirstOrDefaultAsync(f2 => f2.Sha256 == hash);
                    if (existing != null) {
                        existing.UserName = userName ?? existing.UserName;
                        existing.Category = category ?? existing.Category;
                        await _context.SaveChangesAsync();
                        l.Add(existing);
                    } else {
                        int? width = null;
                        int? height = null;
                        string takenAt = null;
                        FileData thumbnail = null;
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
                                        thumbnail = new FileData {
                                            Data = jpegThumb.ToArray()
                                        };
                                    }
                                }
                            }
                        } catch (NotSupportedException) { }

                        DateTime? takenAtDateTime = null;
                        if (DateTime.TryParseExact(takenAt, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.AssumeLocal, out DateTime dt)) {
                            takenAtDateTime = dt;
                        }

                        FileMetadata f = new FileMetadata {
                            Width = width,
                            Height = height,
                            TakenAt = takenAtDateTime,
                            UploadedAt = DateTime.UtcNow,
                            OriginalFilename = Path.GetFileName(file.FileName),
                            UserName = userName,
                            Category = category,
                            Size = data.Length,
                            Sha256 = hash,
                            ContentType = file.ContentType,
                            FileData = new FileData {
                                Data = data
                            },
                            JpegThumbnail = thumbnail
                        };
                        _context.FileMetadata.Add(f);
                        await _context.SaveChangesAsync();

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
            return Content("Files uploaded:\n" + string.Join("\n", l.Select(f => $"{f.OriginalFilename} -> {f.Url}")));
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
        [Route("api/files/{id}/{filename}")]
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
                // Database should store null for consistency.
                if (f.UserName == "") f.UserName = null;
                if (f.Category == "") f.Category = null;
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

        [HttpDelete]
        [Route("api/files/{id}")]
        [Route("api/files/{id}/{filename}")]
        public async Task<IActionResult> Delete(Guid id) {
            var r = BadRequestIfPasswordInvalid();
            if (r != null) return r;

            var f = await _context.FileMetadata.FirstOrDefaultAsync(p => p.FileMetadataId == id);
            if (f == null) {
                return NotFound();
            } else {
                _context.FileMetadata.Remove(f);
                _context.FileData.RemoveRange(
                    from d in _context.FileData
                    where new int?[] {
                        f.FileDataId,
                        f.JpegThumbnailId
                    }.Contains(d.FileDataId)
                    select d);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }
    }
}
