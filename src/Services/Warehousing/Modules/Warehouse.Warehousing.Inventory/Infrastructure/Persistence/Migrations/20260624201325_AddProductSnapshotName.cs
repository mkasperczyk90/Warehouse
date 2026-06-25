using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSnapshotName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "inventory",
                table: "product_snapshots",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                schema: "inventory",
                table: "product_snapshots");
        }
    }
}
