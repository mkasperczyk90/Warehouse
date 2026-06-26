using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Logistics.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShipmentCarrierLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "carrier_role_id",
                schema: "logistics",
                table: "shipments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "pickup",
                schema: "logistics",
                table: "shipments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pickup",
                schema: "logistics",
                table: "shipments");

            migrationBuilder.AlterColumn<string>(
                name: "carrier_role_id",
                schema: "logistics",
                table: "shipments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
