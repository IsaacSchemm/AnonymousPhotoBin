using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace AnonymousPhotoBin.Migrations
{
    public partial class SizeAsInt32 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Size",
                table: "FileMetadata",
                type: "int",
                nullable: false,
                oldClrType: typeof(long));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Size",
                table: "FileMetadata",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
