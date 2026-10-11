using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Artists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Followers",
                table: "Artists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramHandle",
                table: "Artists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PieceCount",
                table: "Artists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriceMax",
                table: "Artists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceMin",
                table: "Artists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Artists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Artists",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "ArtistImages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Slug",
                table: "Artists",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Artists_Slug",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Followers",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "InstagramHandle",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "PieceCount",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "PriceMax",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "PriceMin",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ArtistImages");
        }
    }
}
