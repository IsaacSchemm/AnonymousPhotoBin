using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace AnonymousPhotoBin.Migrations
{
    public partial class Slideshow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlideshowSlides",
                columns: table => new
                {
                    SlideshowSlideId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FileMetadataId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlideshowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlideshowSlides", x => x.SlideshowSlideId);
                    table.ForeignKey(
                        name: "FK_SlideshowSlides_FileMetadata_FileMetadataId",
                        column: x => x.FileMetadataId,
                        principalTable: "FileMetadata",
                        principalColumn: "FileMetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlideshowSlides_FileMetadataId",
                table: "SlideshowSlides",
                column: "FileMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlideshowSlides");
        }
    }
}
