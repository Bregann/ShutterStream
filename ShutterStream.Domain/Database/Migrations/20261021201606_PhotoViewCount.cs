using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShutterStream.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class PhotoViewCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Photos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Photos");
        }
    }
}
