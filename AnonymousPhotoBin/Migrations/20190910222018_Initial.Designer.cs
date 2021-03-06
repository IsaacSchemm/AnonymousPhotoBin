﻿// <auto-generated />
using System;
using AnonymousPhotoBin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AnonymousPhotoBin.Migrations
{
    [DbContext(typeof(PhotoBinDbContext))]
    [Migration("20190910222018_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.11-servicing-32099")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("AnonymousPhotoBin.Data.FileMetadata", b =>
                {
                    b.Property<Guid>("FileMetadataId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Category");

                    b.Property<string>("ContentType")
                        .IsRequired();

                    b.Property<int?>("Height");

                    b.Property<string>("OriginalFilename")
                        .IsRequired();

                    b.Property<byte[]>("Sha256")
                        .HasColumnType("binary(32)");

                    b.Property<int>("Size");

                    b.Property<DateTime?>("TakenAt");

                    b.Property<DateTimeOffset>("UploadedAt");

                    b.Property<string>("UserName");

                    b.Property<int?>("Width");

                    b.HasKey("FileMetadataId");

                    b.ToTable("FileMetadata");
                });
#pragma warning restore 612, 618
        }
    }
}
