﻿// <auto-generated />
using AnonymousPhotoBin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace AnonymousPhotoBin.Migrations
{
    [DbContext(typeof(PhotoBinDbContext))]
    partial class PhotoBinDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.0-rtm-26452")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("AnonymousPhotoBin.Data.FileData", b =>
                {
                    b.Property<int>("FileDataId")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("Data");

                    b.HasKey("FileDataId");

                    b.ToTable("FileData");
                });

            modelBuilder.Entity("AnonymousPhotoBin.Data.FileMetadata", b =>
                {
                    b.Property<Guid>("FileMetadataId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Category");

                    b.Property<string>("ContentType")
                        .IsRequired();

                    b.Property<int>("FileDataId");

                    b.Property<int?>("Height");

                    b.Property<int?>("JpegThumbnailId");

                    b.Property<string>("OriginalFilename")
                        .IsRequired();

                    b.Property<byte[]>("Sha256")
                        .HasColumnType("binary(32)");

                    b.Property<long>("Size");

                    b.Property<DateTime?>("TakenAt");

                    b.Property<DateTimeOffset>("UploadedAt");

                    b.Property<string>("UserName");

                    b.Property<int?>("Width");

                    b.HasKey("FileMetadataId");

                    b.HasIndex("FileDataId");

                    b.HasIndex("JpegThumbnailId");

                    b.ToTable("FileMetadata");
                });

            modelBuilder.Entity("AnonymousPhotoBin.Data.FileMetadata", b =>
                {
                    b.HasOne("AnonymousPhotoBin.Data.FileData", "FileData")
                        .WithMany()
                        .HasForeignKey("FileDataId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("AnonymousPhotoBin.Data.FileData", "JpegThumbnail")
                        .WithMany()
                        .HasForeignKey("JpegThumbnailId");
                });
#pragma warning restore 612, 618
        }
    }
}
