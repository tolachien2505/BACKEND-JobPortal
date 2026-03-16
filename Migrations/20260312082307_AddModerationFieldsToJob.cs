using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationFieldsToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAt",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeratedByUserId",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationNote",
                table: "Jobs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationStatus",
                table: "Jobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "ResumePath",
                table: "Applications",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ModerationNote",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "Jobs");

            migrationBuilder.AlterColumn<string>(
                name: "ResumePath",
                table: "Applications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
