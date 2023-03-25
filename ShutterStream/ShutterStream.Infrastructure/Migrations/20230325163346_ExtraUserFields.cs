using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShutterStream.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtraUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageUploadDailyLimit",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImageUploadsLeftForDay",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "KeepFullSizeImages",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UploadImageSizeLimit",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Expires",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUploadDailyLimit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ImageUploadsLeftForDay",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KeepFullSizeImages",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UploadImageSizeLimit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Expires",
                table: "Sessions");
        }
    }
}
