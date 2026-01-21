using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.AiurDrive.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowAnonymousView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAnonymousView",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
            
            // Data migration: Copy OpenToUpload to AllowAnonymousView
            // This preserves the current behavior where OpenToUpload=true sites were publicly accessible
            migrationBuilder.Sql("UPDATE Sites SET AllowAnonymousView = OpenToUpload");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowAnonymousView",
                table: "Sites");
        }
    }
}
