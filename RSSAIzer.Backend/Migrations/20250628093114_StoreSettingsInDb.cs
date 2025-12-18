using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class StoreSettingsInDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailRecipient = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: false
                    ),
                    DigestTimeUtc = table.Column<string>(
                        type: "TEXT",
                        maxLength: 32,
                        nullable: false
                    ),
                    SmtpSettingsHost = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: false
                    ),
                    SmtpSettingsPort = table.Column<int>(type: "INTEGER", nullable: false),
                    SmtpSettingsUsername = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: false
                    ),
                    SmtpSettingsPassword = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: false
                    ),
                    SmtpSettingsUseSsl = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenAiSettingsApiKey = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: false
                    ),
                    OpenAiSettingsModel = table.Column<string>(
                        type: "TEXT",
                        maxLength: 100,
                        nullable: false
                    ),
                    OpenAiSettingsMaxTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenAiSettingsEndpoint = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: false
                    ),
                    PromptSettingsPostSummaryUserPrompt = table.Column<string>(
                        type: "TEXT",
                        maxLength: 8192,
                        nullable: false
                    ),
                    PromptSettingsPostImportanceUserPrompt = table.Column<string>(
                        type: "TEXT",
                        maxLength: 8192,
                        nullable: false
                    ),
                    PromptSettingsDigestSummaryUserPrompt = table.Column<string>(
                        type: "TEXT",
                        maxLength: 8192,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Settings");
        }
    }
}
