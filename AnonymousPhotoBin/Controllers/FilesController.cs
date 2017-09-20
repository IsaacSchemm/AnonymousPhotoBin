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

namespace AnonymousPhotoBin.Controllers {
    public class FilesController : Controller {
        private static SHA256 SHA256 = SHA256.Create();
        private static int MAX_WIDTH = 320;
        private static int MAX_HEIGHT = 160;

        private readonly PhotoBinDbContext _context;

        public FilesController(PhotoBinDbContext context) {
            _context = context;
        }
        
        [HttpGet]
        [Route("api/files")]
        public async Task<IEnumerable<FileMetadata>> Get() {
            return await _context.FileMetadata.ToListAsync();
        }

        [HttpGet]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Get(Guid id) {
            var photo = await _context.FileMetadata.Include(nameof(FileMetadata.FileData)).FirstOrDefaultAsync(p => p.FileMetadataId == id);
            if (photo == null) {
                return NotFound();
            } else {
                return File(photo.FileData.Data, photo.ContentType, photo.NewFilename);
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
                    : Redirect("http://www-cdr.stanford.edu/%7Epetrie/blank.gif");
            } else {
                return File(photo.JpegThumbnail.Data, "image/jpeg");
            }
        }

        public class UploadedFile {
            public string url, thumbnailUrl, name;
        }

        [HttpPost]
        [Route("api/files")]
        public async Task<List<UploadedFile>> Post(List<IFormFile> files, string timezone = null) {
            List<UploadedFile> l = new List<UploadedFile>();
            foreach (var file in files) {
                using (var ms = new MemoryStream()) {
                    await file.OpenReadStream().CopyToAsync(ms);
                    byte[] data = ms.ToArray();

                    int? width = null;
                    int? height = null;
                    string takenAt = null;
                    FileData thumbnail = null;
                    try {
                        using (var image = Image.Load(data)) {
                            width = image.Width;
                            height = image.Height;
                            takenAt = image.MetaData?.ExifProfile?.GetValue(ExifTag.DateTimeOriginal)?.Value?.ToString();

                            image.Mutate(x => x.AutoOrient());

                            if (image.Width > MAX_WIDTH || image.Height > MAX_HEIGHT) {
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
                        OriginalFilename = file.FileName,
                        Sha256 = SHA256.ComputeHash(data),
                        ContentType = file.ContentType,
                        FileData = new FileData {
                            Data = data
                        },
                        JpegThumbnail = thumbnail
                    };
                    _context.FileMetadata.Add(f);
                    await _context.SaveChangesAsync();

                    l.Add(new UploadedFile {
                        url = $"/api/files/{f.FileMetadataId}",
                        thumbnailUrl = $"/api/thumbnails/{f.FileMetadataId}",
                        name = file.FileName
                    });
                }
            }
            return l;
        }
        
        [HttpDelete]
        [Route("api/files/{id}")]
        public async Task<StatusCodeResult> Delete(Guid id) {
            var f = await _context.FileMetadata.FirstOrDefaultAsync(p => p.FileMetadataId == id);
            if (f == null) {
                return NotFound();
            } else {
                _context.FileMetadata.Remove(f);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }
    }
}
