using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PostSummaries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Feeds",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Digests",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_UserId",
                table: "PostSummaries",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(name: "IX_Feeds_UserId", table: "Feeds", column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Digests_UserId",
                table: "Digests",
                column: "UserId"
            );

            // Insert dummy user for Guid.Empty to satisfy FK constraints
            if (migrationBuilder.IsSqlite())
            {
                migrationBuilder.Sql(
                    @"
                    INSERT OR IGNORE INTO AspNetUsers 
                        (
                         Id, 
                         Email,
                         NormalizedEmail,
                         EmailConfirmed,
                         AccessFailedCount,
                         LockoutEnabled,
                         TwoFactorEnabled,
                         PhoneNumberConfirmed
                         ) VALUES 
                               (
                                '00000000-0000-0000-0000-000000000000',
                                'dummy@example.com',
                                'DUMMY@EXAMPLE.COM',
                                0,
                                0,
                                0,
                                0,
                                0
                                );
                "
                );
            }
            else
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }

            migrationBuilder.AddForeignKey(
                name: "FK_Digests_AspNetUsers_UserId",
                table: "Digests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Feeds_AspNetUsers_UserId",
                table: "Feeds",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_AspNetUsers_UserId",
                table: "PostSummaries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Digests_AspNetUsers_UserId",
                table: "Digests"
            );

            migrationBuilder.DropForeignKey(name: "FK_Feeds_AspNetUsers_UserId", table: "Feeds");

            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_AspNetUsers_UserId",
                table: "PostSummaries"
            );

            migrationBuilder.DropIndex(name: "IX_PostSummaries_UserId", table: "PostSummaries");

            migrationBuilder.DropIndex(name: "IX_Feeds_UserId", table: "Feeds");

            migrationBuilder.DropIndex(name: "IX_Digests_UserId", table: "Digests");

            migrationBuilder.DropColumn(name: "UserId", table: "PostSummaries");

            migrationBuilder.DropColumn(name: "UserId", table: "Feeds");

            migrationBuilder.DropColumn(name: "UserId", table: "Digests");
        }
    }
}
