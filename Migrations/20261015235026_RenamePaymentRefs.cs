using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TwoCheckoutOrderRef",
                table: "Bookings",
                newName: "PaymentRef");

            migrationBuilder.RenameColumn(
                name: "TwoCheckoutSubscriptionRef",
                table: "ArtistSubscriptions",
                newName: "PaymentRef");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentRef",
                table: "Bookings",
                newName: "TwoCheckoutOrderRef");

            migrationBuilder.RenameColumn(
                name: "PaymentRef",
                table: "ArtistSubscriptions",
                newName: "TwoCheckoutSubscriptionRef");
        }
    }
}
