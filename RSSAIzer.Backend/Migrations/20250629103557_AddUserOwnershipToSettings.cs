using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnershipToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_Settings", table: "Settings");

            migrationBuilder.DropColumn(name: "Id", table: "Settings");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_Settings",
                table: "Settings",
                column: "UserId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Settings_AspNetUsers_UserId",
                table: "Settings",
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
                name: "FK_Settings_AspNetUsers_UserId",
                table: "Settings"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_Settings", table: "Settings");

            migrationBuilder.DropColumn(name: "UserId", table: "Settings");

            migrationBuilder
                .AddColumn<int>(
                    name: "Id",
                    table: "Settings",
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: 0
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(name: "PK_Settings", table: "Settings", column: "Id");
        }
    }
}
