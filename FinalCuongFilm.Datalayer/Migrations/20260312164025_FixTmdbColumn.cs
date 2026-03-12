using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalCuongFilm.Datalayer.Migrations
{
    /// <inheritdoc />
    public partial class FixTmdbColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId_Long",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Actors_TmdbId_Long",
                table: "Actors");

            migrationBuilder.RenameColumn(
                name: "TmdbId_Long",
                table: "Movies",
                newName: "TmdbId");

            migrationBuilder.RenameColumn(
                name: "TmdbId_Long",
                table: "Actors",
                newName: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId",
                unique: true,
                filter: "[TmdbId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actors_TmdbId",
                table: "Actors",
                column: "TmdbId",
                unique: true,
                filter: "[TmdbId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Actors_TmdbId",
                table: "Actors");

            migrationBuilder.RenameColumn(
                name: "TmdbId",
                table: "Movies",
                newName: "TmdbId_Long");

            migrationBuilder.RenameColumn(
                name: "TmdbId",
                table: "Actors",
                newName: "TmdbId_Long");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId_Long",
                table: "Movies",
                column: "TmdbId_Long",
                unique: true,
                filter: "[TmdbId_Long] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actors_TmdbId_Long",
                table: "Actors",
                column: "TmdbId_Long",
                unique: true,
                filter: "[TmdbId_Long] IS NOT NULL");
        }
    }
}
