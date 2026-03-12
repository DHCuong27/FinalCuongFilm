using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalCuongFilm.Datalayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movie_Actors_Actors_ActorId",
                table: "Movie_Actors");

            migrationBuilder.DropForeignKey(
                name: "FK_Movie_Actors_Movies_MovieId",
                table: "Movie_Actors");

            migrationBuilder.DropForeignKey(
                name: "FK_Movie_Genres_Genres_GenreId",
                table: "Movie_Genres");

            migrationBuilder.DropForeignKey(
                name: "FK_Movie_Genres_Movies_MovieId",
                table: "Movie_Genres");

            migrationBuilder.DropForeignKey(
                name: "FK_Movie_Tags_Movies_MovieId",
                table: "Movie_Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_Movie_Tags_Tags_TagId",
                table: "Movie_Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Movie_Tags",
                table: "Movie_Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Movie_Genres",
                table: "Movie_Genres");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Movie_Actors",
                table: "Movie_Actors");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "Actors");

            migrationBuilder.RenameTable(
                name: "Movie_Tags",
                newName: "MovieTags");

            migrationBuilder.RenameTable(
                name: "Movie_Genres",
                newName: "MovieGenres");

            migrationBuilder.RenameTable(
                name: "Movie_Actors",
                newName: "MovieActors");

            migrationBuilder.RenameIndex(
                name: "IX_Movie_Tags_TagId",
                table: "MovieTags",
                newName: "IX_MovieTags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_Movie_Tags_MovieId",
                table: "MovieTags",
                newName: "IX_MovieTags_MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_Movie_Genres_MovieId",
                table: "MovieGenres",
                newName: "IX_MovieGenres_MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_Movie_Genres_GenreId",
                table: "MovieGenres",
                newName: "IX_MovieGenres_GenreId");

            migrationBuilder.RenameIndex(
                name: "IX_Movie_Actors_MovieId",
                table: "MovieActors",
                newName: "IX_MovieActors_MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_Movie_Actors_ActorId",
                table: "MovieActors",
                newName: "IX_MovieActors_ActorId");

            migrationBuilder.AddColumn<Guid>(
                name: "TmdbId",
                table: "Movies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TmdbId",
                table: "Actors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieTags",
                table: "MovieTags",
                columns: new[] { "MovieId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieGenres",
                table: "MovieGenres",
                columns: new[] { "MovieId", "GenreId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieActors",
                table: "MovieActors",
                columns: new[] { "MovieId", "ActorId" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_MovieActors_Actors_ActorId",
                table: "MovieActors",
                column: "ActorId",
                principalTable: "Actors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieActors_Movies_MovieId",
                table: "MovieActors",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieGenres_Genres_GenreId",
                table: "MovieGenres",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieGenres_Movies_MovieId",
                table: "MovieGenres",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieTags_Movies_MovieId",
                table: "MovieTags",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieTags_Tags_TagId",
                table: "MovieTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovieActors_Actors_ActorId",
                table: "MovieActors");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieActors_Movies_MovieId",
                table: "MovieActors");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieGenres_Genres_GenreId",
                table: "MovieGenres");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieGenres_Movies_MovieId",
                table: "MovieGenres");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieTags_Movies_MovieId",
                table: "MovieTags");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieTags_Tags_TagId",
                table: "MovieTags");

            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Actors_TmdbId",
                table: "Actors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieTags",
                table: "MovieTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieGenres",
                table: "MovieGenres");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieActors",
                table: "MovieActors");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Actors");

            migrationBuilder.RenameTable(
                name: "MovieTags",
                newName: "Movie_Tags");

            migrationBuilder.RenameTable(
                name: "MovieGenres",
                newName: "Movie_Genres");

            migrationBuilder.RenameTable(
                name: "MovieActors",
                newName: "Movie_Actors");

            migrationBuilder.RenameIndex(
                name: "IX_MovieTags_TagId",
                table: "Movie_Tags",
                newName: "IX_Movie_Tags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_MovieTags_MovieId",
                table: "Movie_Tags",
                newName: "IX_Movie_Tags_MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_MovieGenres_MovieId",
                table: "Movie_Genres",
                newName: "IX_Movie_Genres_MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_MovieGenres_GenreId",
                table: "Movie_Genres",
                newName: "IX_Movie_Genres_GenreId");

            migrationBuilder.RenameIndex(
                name: "IX_MovieActors_MovieId",
                table: "Movie_Actors",
                newName: "IX_Movie_Actors_MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_MovieActors_ActorId",
                table: "Movie_Actors",
                newName: "IX_Movie_Actors_ActorId");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Actors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Movie_Tags",
                table: "Movie_Tags",
                columns: new[] { "MovieId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Movie_Genres",
                table: "Movie_Genres",
                columns: new[] { "MovieId", "GenreId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Movie_Actors",
                table: "Movie_Actors",
                columns: new[] { "MovieId", "ActorId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Movie_Actors_Actors_ActorId",
                table: "Movie_Actors",
                column: "ActorId",
                principalTable: "Actors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Movie_Actors_Movies_MovieId",
                table: "Movie_Actors",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Movie_Genres_Genres_GenreId",
                table: "Movie_Genres",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Movie_Genres_Movies_MovieId",
                table: "Movie_Genres",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Movie_Tags_Movies_MovieId",
                table: "Movie_Tags",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Movie_Tags_Tags_TagId",
                table: "Movie_Tags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
