using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnonymousPhotoBin.Data {
    public class PhotoBinDbContext : DbContext {
        public PhotoBinDbContext(DbContextOptions<PhotoBinDbContext> options) : base(options) { }

        public DbSet<FileMetadata> FileMetadata { get; set; }

        public DbSet<FileData> FileData { get; set; }

        public DbSet<SlideshowSlide> SlideshowSlides { get; set; }
    }
}
