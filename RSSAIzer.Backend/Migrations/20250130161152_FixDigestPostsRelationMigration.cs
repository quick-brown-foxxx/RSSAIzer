using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixDigestPostsRelationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Digests_DigestIdNav",
                table: "PostSummaries"
            );

            migrationBuilder.DropIndex(
                name: "IX_PostSummaries_DigestIdNav",
                table: "PostSummaries"
            );

            migrationBuilder.DropColumn(name: "DigestIdNav", table: "PostSummaries");

            migrationBuilder.CreateTable(
                name: "DigestPosts",
                columns: table => new
                {
                    DigestEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PostsNavId = table.Column<Guid>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestPosts", x => new { x.DigestEntityId, x.PostsNavId });
                    table.ForeignKey(
                        name: "FK_DigestPosts_Digests_DigestEntityId",
                        column: x => x.DigestEntityId,
                        principalTable: "Digests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_DigestPosts_PostSummaries_PostsNavId",
                        column: x => x.PostsNavId,
                        principalTable: "PostSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_DigestPosts_PostsNavId",
                table: "DigestPosts",
                column: "PostsNavId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DigestPosts");

            migrationBuilder.AddColumn<Guid>(
                name: "DigestIdNav",
                table: "PostSummaries",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_DigestIdNav",
                table: "PostSummaries",
                column: "DigestIdNav"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_Digests_DigestIdNav",
                table: "PostSummaries",
                column: "DigestIdNav",
                principalTable: "Digests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
