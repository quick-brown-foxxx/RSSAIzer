using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ChannelsSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries"
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Channels",
                type: "INTEGER",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.CreateIndex(
                name: "IX_Channels_IsDeleted",
                table: "Channels",
                column: "IsDeleted"
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries"
            );

            migrationBuilder.DropIndex(name: "IX_Channels_IsDeleted", table: "Channels");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Channels");

            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries",
                column: "ChannelTgId",
                principalTable: "Channels",
                principalColumn: "TgId",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
