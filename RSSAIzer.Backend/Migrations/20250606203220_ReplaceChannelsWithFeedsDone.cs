using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceChannelsWithFeedsDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChannelsJson",
                table: "DigestStatuses",
                newName: "FeedsJson"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FeedsJson",
                table: "DigestStatuses",
                newName: "ChannelsJson"
            );
        }
    }
}
