using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShutterStream.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class PhotoSubFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_AlbumId_FileName",
                table: "Photos");

            migrationBuilder.AddColumn<string>(
                name: "SubFolder",
                table: "Photos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_AlbumId_SubFolder_FileName",
                table: "Photos",
                columns: new[] { "AlbumId", "SubFolder", "FileName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_AlbumId_SubFolder_FileName",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "SubFolder",
                table: "Photos");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_AlbumId_FileName",
                table: "Photos",
                columns: new[] { "AlbumId", "FileName" },
                unique: true);
        }
    }
}
