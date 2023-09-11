using Microsoft.EntityFrameworkCore;

namespace AnonymousPhotoBin.Data {
    public class PhotoBinDbContext : DbContext {
        public PhotoBinDbContext(DbContextOptions<PhotoBinDbContext> options) : base(options) { }

        public DbSet<FileMetadata> FileMetadata { get; set; }
    }
}
