﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AnonymousPhotoBin.Data {
    public class PhotoBinDbContext : IdentityDbContext {
        public PhotoBinDbContext(DbContextOptions<PhotoBinDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder) {
            // Cosmos DB - not sure this is really correct
            // https://learn.microsoft.com/en-us/ef/core/providers/cosmos/?tabs=dotnet-core-cli#optimistic-concurrency-with-etags
            builder.Entity<IdentityRole>().Property(d => d.ConcurrencyStamp).IsETagConcurrency();
            builder.Entity<IdentityUser>().Property(d => d.ConcurrencyStamp).IsETagConcurrency();

            base.OnModelCreating(builder);
        }

        public DbSet<FileMetadata> FileMetadata { get; set; } = null!;
    }
}
