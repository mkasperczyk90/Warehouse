using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationSnapshotRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "room",
                schema: "inventory",
                table: "location_snapshots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "warehouse",
                schema: "inventory",
                table: "location_snapshots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_location_snapshots_warehouse_room",
                schema: "inventory",
                table: "location_snapshots",
                columns: new[] { "warehouse", "room" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_location_snapshots_warehouse_room",
                schema: "inventory",
                table: "location_snapshots");

            migrationBuilder.DropColumn(
                name: "room",
                schema: "inventory",
                table: "location_snapshots");

            migrationBuilder.DropColumn(
                name: "warehouse",
                schema: "inventory",
                table: "location_snapshots");
        }
    }
}
