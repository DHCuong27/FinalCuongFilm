using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalCuongFilm.Datalayer.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoUrlToMovie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Movies");
        }
    }
}
