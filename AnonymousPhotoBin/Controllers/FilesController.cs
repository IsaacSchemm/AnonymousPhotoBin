using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using MetadataExtractor;
using System.Globalization;
using AnonymousPhotoBin.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AnonymousPhotoBin.Controllers {
    public class FilesController : Controller {
        private static SHA256 _sha256 = SHA256.Create();

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
            var photo = await _context.Photos.Include(nameof(Photo.PhotoData)).FirstOrDefaultAsync(p => p.PhotoId == id);
            if (photo == null) {
                return NotFound();
            } else if (photo.PhotoData == null) {
                return await Get(id);
            } else {
                return File(photo.PhotoData.Data, photo.PhotoData.ContentType);
            }
        }

        public class UploadedFile {
            public string url, thumbnailUrl, name, type;
            public long size;
        }

        [HttpPost]
        [Route("api/files")]
        public async Task<object> Post(List<IFormFile> files, string timezone = null) {
            List<UploadedFile> l = new List<UploadedFile>();
            foreach (var file in files) {
                if (file.FileName == "mark.png") throw new Exception("test");
                using (var ms = new MemoryStream()) {
                    await file.OpenReadStream().CopyToAsync(ms);
                    byte[] data = ms.ToArray();

                    Photo photo = new Photo {
                        Width = 10,
                        Height = 10,
                        TakenAt = null,
                        UploadedAt = DateTime.UtcNow,
                        OriginalFilename = file.FileName,
                        SHA256 = _sha256.ComputeHash(data),
                        PhotoData = new PhotoData {
                            ContentType = file.ContentType,
                            Data = data
                        }
                    };
                    _context.Photos.Add(photo);
                    await _context.SaveChangesAsync();

                    l.Add(new UploadedFile {
                        url = $"/api/files/{photo.PhotoId}",
                        thumbnailUrl = $"/api/thumbnails/{photo.PhotoId}",
                        name = file.FileName,
                        type = file.ContentType,
                        size = file.Length
                    });
                }
            }
            return new { files = l };
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
