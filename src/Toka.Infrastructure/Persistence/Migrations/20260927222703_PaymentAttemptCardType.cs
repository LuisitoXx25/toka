using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Toka.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentAttemptCardType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "card_type",
                table: "payment_attempts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                // Attempts recorded before BIN lookup existed have no known card type.
                defaultValue: "Unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "card_type",
                table: "payment_attempts");
        }
    }
}
