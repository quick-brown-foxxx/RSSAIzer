using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ConvertChannelsToFeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if using SQLite
            if (!migrationBuilder.IsSqlite())
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }

            // Create new Feeds table
            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    RssUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: false
                    ),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.RssUrl);
                }
            );
            migrationBuilder.CreateIndex(
                name: "IX_Feeds_IsDeleted",
                table: "Feeds",
                column: "IsDeleted"
            );

            // Migrate data from Channels to Feeds
            migrationBuilder.Sql(
                @"INSERT INTO Feeds (RssUrl, Title, Description, ImageUrl, IsDeleted)
                  SELECT 'https://rsshub.app/telegram/channel/' || TgId, Title, Description, ImageUrl, IsDeleted
                  FROM Channels"
            );

            // Create new PostSummaries table
            migrationBuilder.CreateTable(
                name: "PostSummaries_New",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DigestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeedUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSummaries_Digests_DigestId",
                        column: x => x.DigestId,
                        principalTable: "Digests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_PostSummaries_Feeds_FeedUrl",
                        column: x => x.FeedUrl,
                        principalTable: "Feeds",
                        principalColumn: "RssUrl",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            // Copy data to new PostSummaries table
            migrationBuilder.Sql(
                @"INSERT INTO PostSummaries_New (Id, DigestId, FeedUrl, Summary, Url, PublishedAt, Importance)
                  SELECT Id, DigestId, 'https://rsshub.app/telegram/channel/' || ChannelTgId, Summary, Url, PublishedAt, Importance
                  FROM PostSummaries"
            );

            // Drop old PostSummaries table and rename new one
            migrationBuilder.DropTable(name: "PostSummaries");
            migrationBuilder.RenameTable(name: "PostSummaries_New", newName: "PostSummaries");

            // Create indexes for new PostSummaries table
            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_DigestId",
                table: "PostSummaries",
                column: "DigestId"
            );
            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_FeedUrl",
                table: "PostSummaries",
                column: "FeedUrl"
            );

            // Now safe to drop Channels table since nothing references it
            migrationBuilder.DropTable(name: "Channels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Check if using SQLite
            if (!migrationBuilder.IsSqlite())
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }

            // Create Channels table
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
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.TgId);
                }
            );

            // Migrate data from Feeds to Channels
            migrationBuilder.Sql(
                @"INSERT INTO Channels (TgId, Title, Description, ImageUrl, IsDeleted)
                  SELECT SUBSTR(RssUrl, 37), Title, Description, ImageUrl, IsDeleted
                  FROM Feeds
                  WHERE RssUrl LIKE 'https://rsshub.app/telegram/channel/%'"
            );

            // Create new PostSummaries table
            migrationBuilder.CreateTable(
                name: "PostSummaries_New",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DigestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelTgId = table.Column<string>(
                        type: "TEXT",
                        maxLength: 32,
                        nullable: false
                    ),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSummaries_Digests_DigestId",
                        column: x => x.DigestId,
                        principalTable: "Digests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_PostSummaries_Channels_ChannelTgId",
                        column: x => x.ChannelTgId,
                        principalTable: "Channels",
                        principalColumn: "TgId",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            // Copy data to new PostSummaries table
            migrationBuilder.Sql(
                @"INSERT INTO PostSummaries_New (Id, DigestId, ChannelTgId, Summary, Url, PublishedAt, Importance)
                  SELECT Id, DigestId, SUBSTR(FeedUrl, 37), Summary, Url, PublishedAt, Importance
                  FROM PostSummaries
                  WHERE FeedUrl LIKE 'https://rsshub.app/telegram/channel/%'"
            );

            // Drop old PostSummaries table and rename new one
            migrationBuilder.DropTable(name: "PostSummaries");
            migrationBuilder.RenameTable(name: "PostSummaries_New", newName: "PostSummaries");

            // Create indexes for new PostSummaries table
            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_DigestId",
                table: "PostSummaries",
                column: "DigestId"
            );
            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_ChannelTgId",
                table: "PostSummaries",
                column: "ChannelTgId"
            );
            migrationBuilder.CreateIndex(
                name: "IX_Channels_IsDeleted",
                table: "Channels",
                column: "IsDeleted"
            );

            // Now safe to drop Feeds table since nothing references it
            migrationBuilder.DropTable(name: "Feeds");
        }
    }
}
