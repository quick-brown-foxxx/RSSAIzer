using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Steps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DigestStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DigestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    Percentage = table.Column<int>(type: "INTEGER", nullable: true),
                    ExceptionJsonSerialized = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorsJsonSerialized = table.Column<string>(type: "TEXT", nullable: true),
                    PostsFound = table.Column<int>(type: "INTEGER", nullable: true),
                    ChannelsJson = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestStatuses", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_DigestStatuses_DigestId",
                table: "DigestStatuses",
                column: "DigestId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DigestStatuses_Type",
                table: "DigestStatuses",
                column: "Type"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DigestStatuses");
        }
    }
}
