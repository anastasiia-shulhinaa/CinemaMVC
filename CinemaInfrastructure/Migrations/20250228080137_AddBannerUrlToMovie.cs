using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerUrlToMovie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Movies");
        }
    }
}
