using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.AiurDrive.MySql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOpenToUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenToUpload",
                table: "Sites");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OpenToUpload",
                table: "Sites",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
