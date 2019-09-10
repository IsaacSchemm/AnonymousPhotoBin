using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AnonymousPhotoBin.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileMetadata",
                columns: table => new
                {
                    FileMetadataId = table.Column<Guid>(nullable: false),
                    Width = table.Column<int>(nullable: true),
                    Height = table.Column<int>(nullable: true),
                    TakenAt = table.Column<DateTime>(nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(nullable: false),
                    OriginalFilename = table.Column<string>(nullable: false),
                    UserName = table.Column<string>(nullable: true),
                    Category = table.Column<string>(nullable: true),
                    Size = table.Column<int>(nullable: false),
                    Sha256 = table.Column<byte[]>(type: "binary(32)", nullable: true),
                    ContentType = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMetadata", x => x.FileMetadataId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileMetadata");
        }
    }
}
