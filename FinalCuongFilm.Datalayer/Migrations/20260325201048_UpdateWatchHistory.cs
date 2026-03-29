using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalCuongFilm.Datalayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWatchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EpisodeId",
                table: "WatchHistories");

            migrationBuilder.RenameColumn(
                name: "WatchedAt",
                table: "WatchHistories",
                newName: "LastWatchedAt");

            migrationBuilder.RenameIndex(
                name: "IX_WatchHistories_WatchedAt",
                table: "WatchHistories",
                newName: "IX_WatchHistories_LastWatchedAt");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WatchHistories",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CuongFilmUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuongFilmUser", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_UserId",
                table: "WatchHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_CuongFilmUser_UserId",
                table: "WatchHistories",
                column: "UserId",
                principalTable: "CuongFilmUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_Movies_MovieId",
                table: "WatchHistories",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_CuongFilmUser_UserId",
                table: "WatchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_Movies_MovieId",
                table: "WatchHistories");

            migrationBuilder.DropTable(
                name: "CuongFilmUser");

            migrationBuilder.DropIndex(
                name: "IX_WatchHistories_UserId",
                table: "WatchHistories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WatchHistories");

            migrationBuilder.RenameColumn(
                name: "LastWatchedAt",
                table: "WatchHistories",
                newName: "WatchedAt");

            migrationBuilder.RenameIndex(
                name: "IX_WatchHistories_LastWatchedAt",
                table: "WatchHistories",
                newName: "IX_WatchHistories_WatchedAt");

            migrationBuilder.AddColumn<Guid>(
                name: "EpisodeId",
                table: "WatchHistories",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
