using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonymousPhotoBin.Data.Migrations
{
    public partial class BuiltInAdmin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "81976842-8543-4dac-9729-dde8117b994f", 0, "4f1349b0-a55d-4ea9-842e-8552c5a36559", "admin@example.com", true, false, null, "ADMIN@EXAMPLE.COM", "ADMIN", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0=", null, false, "ZASWEWAV55LMV7HKNM2CCSM3JTFVIQDO", false, "admin" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "81976842-8543-4dac-9729-dde8117b994f");
        }
    }
}
