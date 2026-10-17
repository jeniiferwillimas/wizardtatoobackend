using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ArtistStyles_StyleName",
                table: "ArtistStyles",
                column: "StyleName");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_City",
                table: "Artists",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_OrderNo",
                table: "Artists",
                column: "OrderNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArtistStyles_StyleName",
                table: "ArtistStyles");

            migrationBuilder.DropIndex(
                name: "IX_Artists_City",
                table: "Artists");

            migrationBuilder.DropIndex(
                name: "IX_Artists_OrderNo",
                table: "Artists");
        }
    }
}
