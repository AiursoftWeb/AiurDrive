using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.AiurDrive.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageSizeLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StorageSizeLimit",
                table: "Sites",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageSizeLimit",
                table: "Sites");
        }
    }
}
