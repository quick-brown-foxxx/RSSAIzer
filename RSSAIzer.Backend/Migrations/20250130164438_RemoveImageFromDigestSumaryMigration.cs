using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveImageFromDigestSumaryMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ImageUrl", table: "DigestSummaries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "DigestSummaries",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                defaultValue: ""
            );
        }
    }
}
