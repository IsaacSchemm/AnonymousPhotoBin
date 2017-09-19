using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace AnonymousPhotoBin.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoData",
                columns: table => new
                {
                    PhotoDataId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoData", x => x.PhotoDataId);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    PhotoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalFilename = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoDataId = table.Column<int>(type: "int", nullable: false),
                    SHA256 = table.Column<byte[]>(type: "binary(32)", nullable: true),
                    TakenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThumbnailDataId = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.PhotoId);
                    table.ForeignKey(
                        name: "FK_Photos_PhotoData_PhotoDataId",
                        column: x => x.PhotoDataId,
                        principalTable: "PhotoData",
                        principalColumn: "PhotoDataId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Photos_PhotoData_ThumbnailDataId",
                        column: x => x.ThumbnailDataId,
                        principalTable: "PhotoData",
                        principalColumn: "PhotoDataId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_PhotoDataId",
                table: "Photos",
                column: "PhotoDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ThumbnailDataId",
                table: "Photos",
                column: "ThumbnailDataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "PhotoData");
        }
    }
}
