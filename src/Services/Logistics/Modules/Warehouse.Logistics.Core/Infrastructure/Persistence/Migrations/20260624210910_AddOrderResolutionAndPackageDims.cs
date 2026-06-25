using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Logistics.Core.Infrastructure.Persistence.Migrations
{
    /// <summary>Adds OutboundOrder.resolution (split/hold decision) and closes prior model drift: the
    /// Shipment PackageDimensions columns (length/width/height) the model had but no migration carried.</summary>
    public partial class AddOrderResolutionAndPackageDims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "height_cm",
                schema: "logistics",
                table: "packages",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "length_cm",
                schema: "logistics",
                table: "packages",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "width_cm",
                schema: "logistics",
                table: "packages",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "resolution",
                schema: "logistics",
                table: "outbound_orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "height_cm",
                schema: "logistics",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "length_cm",
                schema: "logistics",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "width_cm",
                schema: "logistics",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "resolution",
                schema: "logistics",
                table: "outbound_orders");
        }
    }
}
