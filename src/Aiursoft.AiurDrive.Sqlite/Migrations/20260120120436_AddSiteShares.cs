using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.AiurDrive.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteId = table.Column<int>(type: "INTEGER", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "TEXT", nullable: true),
                    SharedWithRoleId = table.Column<string>(type: "TEXT", nullable: true),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteShares_AspNetUsers_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SiteShares_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteShares_SharedWithUserId",
                table: "SiteShares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteShares_SiteId",
                table: "SiteShares",
                column: "SiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteShares");
        }
    }
}
