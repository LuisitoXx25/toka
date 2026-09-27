using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Toka.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TaxBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "subtotal",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_rate",
                table: "orders",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // Orders created before this migration: catalog prices already included 16% IVA.
            migrationBuilder.Sql("""
                UPDATE orders
                SET tax_rate = 0.16,
                    subtotal = round(total / 1.16, 2),
                    tax_amount = total - round(total / 1.16, 2);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_tax_breakdown",
                table: "orders",
                sql: "subtotal + tax_amount = total");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_tax_breakdown",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "subtotal",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "tax_rate",
                table: "orders");
        }
    }
}
