using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSSAIzer.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultGuidUserIdFromFkInContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PostSummaries",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Feeds",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Digests",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT"
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PostSummaries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT"
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Feeds",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT"
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Digests",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT"
            );
        }
    }
}
