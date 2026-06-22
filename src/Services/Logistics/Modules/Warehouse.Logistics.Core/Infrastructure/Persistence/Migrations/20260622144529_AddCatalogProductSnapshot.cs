using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Logistics.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProductSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_product_snapshots",
                schema: "logistics",
                columns: table => new
                {
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    base_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    is_batch_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_product_snapshots", x => x.product_code);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_product_snapshots",
                schema: "logistics");
        }
    }
}
