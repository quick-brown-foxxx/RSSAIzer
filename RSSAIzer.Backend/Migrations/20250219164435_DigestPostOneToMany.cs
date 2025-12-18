using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class DigestPostOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add the new DigestId column
            migrationBuilder.AddColumn<Guid>(
                name: "DigestId",
                table: "PostSummaries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            // Migrate existing relationships based on provider
            if (migrationBuilder.IsSqlite())
            {
                migrationBuilder.Sql(
                    @"
                    UPDATE PostSummaries 
                    SET DigestId = (
                        SELECT DigestEntityId 
                        FROM DigestPosts 
                        WHERE DigestPosts.PostsNavId = PostSummaries.Id
                        LIMIT 1
                    )"
                );
            }
            else
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }

            // Create new index
            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_DigestId",
                table: "PostSummaries",
                column: "DigestId"
            );

            // Add foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_Digests_DigestId",
                table: "PostSummaries",
                column: "DigestId",
                principalTable: "Digests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            // Drop the old junction table
            migrationBuilder.DropTable(name: "DigestPosts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the junction table
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

            // Migrate data back to the junction table based on provider
            if (migrationBuilder.IsSqlite())
            {
                migrationBuilder.Sql(
                    @"
                    INSERT INTO DigestPosts (DigestEntityId, PostsNavId)
                    SELECT DigestId, Id
                    FROM PostSummaries
                    WHERE DigestId IS NOT NULL"
                );
            }
            else
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }

            // Create index for the junction table
            migrationBuilder.CreateIndex(
                name: "IX_DigestPosts_PostsNavId",
                table: "DigestPosts",
                column: "PostsNavId"
            );

            // Remove the DigestId column
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Digests_DigestId",
                table: "PostSummaries"
            );

            migrationBuilder.DropIndex(name: "IX_PostSummaries_DigestId", table: "PostSummaries");

            migrationBuilder.DropColumn(name: "DigestId", table: "PostSummaries");
        }
    }

    public static class MigrationBuilderExtensions
    {
        public static bool IsSqlite(this MigrationBuilder migrationBuilder)
        {
            return migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
        }
    }
}
