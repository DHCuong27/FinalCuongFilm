using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalCuongFilm.Datalayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_CuongFilmUser_UserId",
                table: "UserSubscriptions",
                column: "UserId",
                principalTable: "CuongFilmUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_CuongFilmUser_UserId",
                table: "UserSubscriptions");
        }
    }
}
