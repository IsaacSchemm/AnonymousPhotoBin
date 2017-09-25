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
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Threading;

namespace AnonymousPhotoBin.Controllers {
    public class SlideshowController : Controller {
        private readonly IConfiguration _configuration;
        private readonly PhotoBinDbContext _context;

        public SlideshowController(PhotoBinDbContext context, IConfiguration configuration) {
            _context = context;
            _configuration = configuration;

            _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(130));
        }

        [HttpGet]
        [Route("api/slideshow/{slideshowId}")]
        public async Task<List<Guid>> Get(Guid slideshowId) {
            return await _context.SlideshowSlides
                .Where(s => s.SlideshowId == slideshowId)
                .Select(s => s.FileMetadataId)
                .ToListAsync();
        }

        [HttpGet]
        [Route("api/slideshow/{slideshowId}/add/{fileMetadataId}")]
        public async Task<IActionResult> Add(Guid slideshowId, Guid fileMetadataId) {
            if (!_context.SlideshowSlides.Any(s => s.SlideshowId == slideshowId && s.FileMetadataId == fileMetadataId)) {
                _context.SlideshowSlides.Add(new SlideshowSlide {
                    FileMetadataId = fileMetadataId,
                    SlideshowId = slideshowId
                });
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }

        [HttpGet]
        [Route("api/slideshow/{slideshowId}/delete/{fileMetadataId}")]
        public async Task<IActionResult> Delete(Guid slideshowId, Guid fileMetadataId) {
            var existing = await _context.SlideshowSlides.FirstOrDefaultAsync(s => s.SlideshowId == slideshowId && s.FileMetadataId == fileMetadataId);
            if (existing != null) {
                _context.SlideshowSlides.Remove(existing);
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }
    }
}
