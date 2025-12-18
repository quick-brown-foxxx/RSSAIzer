using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUsedPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!migrationBuilder.IsSqlite())
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }
            migrationBuilder.AddColumn<string>(
                name: "UsedPrompts",
                table: "Digests",
                type: "json",
                maxLength: 4096,
                nullable: false,
                defaultValue: ""
            );
            // Set default value for all existing rows as a JSON dictionary
            migrationBuilder.Sql(
                "UPDATE Digests SET UsedPrompts = '{\"PostSummary\":\"unknown\",\"PostImportance\":\"unknown\",\"DigestSummary\":\"unknown\"}'"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UsedPrompts", table: "Digests");
        }
    }
}
