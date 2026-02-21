using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalCuongFilm.Datalayer.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeleteWithClientSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaFiles_Episodes_EpisodeId",
                table: "MediaFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaFiles_Movies_MovieId",
                table: "MediaFiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WatchedAt",
                table: "WatchHistories",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Favorites",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_MovieId",
                table: "WatchHistories",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_WatchedAt",
                table: "WatchHistories",
                column: "WatchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchSuggestions_CreatedAt",
                table: "SearchSuggestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CreatedAt",
                table: "Reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_CreatedAt",
                table: "Movies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_IsActive",
                table: "Movies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Slug",
                table: "Movies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ViewCount",
                table: "Movies",
                column: "ViewCount");

            migrationBuilder.CreateIndex(
                name: "IX_Movie_Tags_MovieId",
                table: "Movie_Tags",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Movie_Genres_MovieId",
                table: "Movie_Genres",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Movie_Actors_MovieId",
                table: "Movie_Actors",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_FileType",
                table: "MediaFiles",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_IsActive",
                table: "Episodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_IsoCode",
                table: "Countries",
                column: "IsoCode");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Slug",
                table: "Countries",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actors_Slug",
                table: "Actors",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaFiles_Episodes_EpisodeId",
                table: "MediaFiles",
                column: "EpisodeId",
                principalTable: "Episodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaFiles_Movies_MovieId",
                table: "MediaFiles",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaFiles_Episodes_EpisodeId",
                table: "MediaFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaFiles_Movies_MovieId",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_WatchHistories_MovieId",
                table: "WatchHistories");

            migrationBuilder.DropIndex(
                name: "IX_WatchHistories_WatchedAt",
                table: "WatchHistories");

            migrationBuilder.DropIndex(
                name: "IX_SearchSuggestions_CreatedAt",
                table: "SearchSuggestions");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CreatedAt",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Movies_CreatedAt",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_IsActive",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_Slug",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_ViewCount",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movie_Tags_MovieId",
                table: "Movie_Tags");

            migrationBuilder.DropIndex(
                name: "IX_Movie_Genres_MovieId",
                table: "Movie_Genres");

            migrationBuilder.DropIndex(
                name: "IX_Movie_Actors_MovieId",
                table: "Movie_Actors");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_FileType",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_IsActive",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "IX_Countries_IsoCode",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Countries_Slug",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Actors_Slug",
                table: "Actors");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WatchedAt",
                table: "WatchHistories",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Favorites",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaFiles_Episodes_EpisodeId",
                table: "MediaFiles",
                column: "EpisodeId",
                principalTable: "Episodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaFiles_Movies_MovieId",
                table: "MediaFiles",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
