using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.AiurDrive.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddVaultConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VaultSaltBase64",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VerifierCipherBase64",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VerifierNonceBase64",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VerifierTagBase64",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VaultSaltBase64",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerifierCipherBase64",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerifierNonceBase64",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerifierTagBase64",
                table: "AspNetUsers");
        }
    }
}
