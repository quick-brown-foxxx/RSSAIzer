using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ChannelCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries",
                column: "ChannelTgId",
                principalTable: "Channels",
                principalColumn: "TgId",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries",
                column: "ChannelTgId",
                principalTable: "Channels",
                principalColumn: "TgId",
                onDelete: ReferentialAction.Restrict
            );
        }
    }
}
