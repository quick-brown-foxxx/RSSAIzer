using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    TgId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: false
                    ),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.TgId);
                }
            );

            migrationBuilder.CreateTable(
                name: "Digests",
                columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false) },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Digests", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "DigestSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PostsSummary = table.Column<string>(
                        type: "TEXT",
                        maxLength: 8192,
                        nullable: false
                    ),
                    PostsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageImportance = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateTo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigestSummaries_Digests_Id",
                        column: x => x.Id,
                        principalTable: "Digests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PostSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelTgId = table.Column<string>(
                        type: "TEXT",
                        maxLength: 32,
                        nullable: false
                    ),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    DigestIdNav = table.Column<Guid>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSummaries_Channels_ChannelTgId",
                        column: x => x.ChannelTgId,
                        principalTable: "Channels",
                        principalColumn: "TgId",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_PostSummaries_Digests_DigestIdNav",
                        column: x => x.DigestIdNav,
                        principalTable: "Digests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_ChannelTgId",
                table: "PostSummaries",
                column: "ChannelTgId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_DigestIdNav",
                table: "PostSummaries",
                column: "DigestIdNav"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DigestSummaries");

            migrationBuilder.DropTable(name: "PostSummaries");

            migrationBuilder.DropTable(name: "Channels");

            migrationBuilder.DropTable(name: "Digests");
        }
    }
}
