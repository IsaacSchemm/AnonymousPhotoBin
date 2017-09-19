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
        public async Task<IEnumerable<Photo>> Get() {
            return await _context.Photos.ToListAsync();
        }

        [HttpGet]
        [Route("api/files/{id}")]
        public async Task<IActionResult> Get(Guid id) {
            var photo = await _context.Photos.Include(nameof(Photo.PhotoData)).FirstOrDefaultAsync(p => p.PhotoId == id);
            if (photo == null) {
                return NotFound();
            } else {
                return File(photo.PhotoData.Data, photo.PhotoData.ContentType);
            }
        }

        [HttpGet]
        [Route("api/thumbnails/{id}")]
        public async Task<IActionResult> GetThumbnail(Guid id) {
            var photo = await _context.Photos.Include(nameof(Photo.ThumbnailData)).FirstOrDefaultAsync(p => p.PhotoId == id);
            if (photo == null) {
                return NotFound();
            } else if (photo.ThumbnailData == null) {
                return await Get(id);
            } else {
                return File(photo.ThumbnailData.Data, photo.ThumbnailData.ContentType);
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
                if (file.FileName == "mark.png") throw new Exception("test");
                using (var ms = new MemoryStream()) {
                    await file.OpenReadStream().CopyToAsync(ms);
                    byte[] data = ms.ToArray();

                    int? width = null;
                    int? height = null;
                    string takenAt = null;
                    PhotoData thumbnail = null;
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
                                thumbnail = new PhotoData {
                                    ContentType = "image/jpeg",
                                    Data = jpegThumb.ToArray()
                                };
                            }
                        }
                    }

                    DateTime? takenAtDateTime = null;
                    if (DateTime.TryParseExact(takenAt, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.AssumeLocal, out DateTime dt)) {
                        takenAtDateTime = dt;
                    }

                    Photo photo = new Photo {
                        Width = width ?? 10,
                        Height = height ?? 10,
                        TakenAt = takenAtDateTime,
                        UploadedAt = DateTime.UtcNow,
                        OriginalFilename = file.FileName,
                        SHA256 = SHA256.ComputeHash(data),
                        PhotoData = new PhotoData {
                            ContentType = file.ContentType,
                            Data = data
                        },
                        ThumbnailData = thumbnail
                    };
                    _context.Photos.Add(photo);
                    await _context.SaveChangesAsync();

                    l.Add(new UploadedFile {
                        url = $"/api/files/{photo.PhotoId}",
                        thumbnailUrl = $"/api/thumbnails/{photo.PhotoId}",
                        name = file.FileName
                    });
                }
            }
            return l;
        }
        
        [HttpDelete]
        [Route("api/files/{id}")]
        public async Task<StatusCodeResult> Delete(Guid id) {
            var photo = await _context.Photos.FirstOrDefaultAsync(p => p.PhotoId == id);
            if (photo == null) {
                return NotFound();
            } else {
                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }
    }
}
