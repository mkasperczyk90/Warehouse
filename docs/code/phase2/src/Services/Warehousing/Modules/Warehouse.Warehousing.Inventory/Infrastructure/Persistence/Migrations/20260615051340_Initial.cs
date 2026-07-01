using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "batches",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    quality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "handling_units",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lpn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    location = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handling_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "location_snapshots",
                schema: "inventory",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    temp_min_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    temp_max_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    is_hazmat_zone = table.Column<bool>(type: "boolean", nullable: false),
                    capacity_m3 = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    max_load_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_location_snapshots", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "product_snapshots",
                schema: "inventory",
                columns: table => new
                {
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    base_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    unit_weight_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unit_volume_m3 = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    temp_min_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    temp_max_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    requires_cold_chain = table.Column<bool>(type: "boolean", nullable: false),
                    is_hazardous = table.Column<bool>(type: "boolean", nullable: false),
                    is_batch_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    has_expiry_date = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_snapshots", x => x.sku);
                });

            migrationBuilder.CreateTable(
                name: "stock_items",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    batch = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    location = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    on_hand_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    on_hand_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    allocated_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    batch = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    from_location = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    to_location = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    qty_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    qty_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    performed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    warehouse = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    order_ref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    qty_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    qty_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    allocated_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "handling_unit_lines",
                schema: "inventory",
                columns: table => new
                {
                    handling_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    batch = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    qty_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    qty_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handling_unit_lines", x => new { x.handling_unit_id, x.Id });
                    table.ForeignKey(
                        name: "FK_handling_unit_lines_handling_units_handling_unit_id",
                        column: x => x.handling_unit_id,
                        principalSchema: "inventory",
                        principalTable: "handling_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "allocations",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_ref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    qty_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    qty_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allocations", x => new { x.stock_item_id, x.id });
                    table.ForeignKey(
                        name: "FK_allocations_stock_items_stock_item_id",
                        column: x => x.stock_item_id,
                        principalSchema: "inventory",
                        principalTable: "stock_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_batches_sku_number",
                schema: "inventory",
                table: "batches",
                columns: new[] { "sku", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_handling_units_lpn",
                schema: "inventory",
                table: "handling_units",
                column: "lpn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_sku_batch_location",
                schema: "inventory",
                table: "stock_items",
                columns: new[] { "sku", "batch", "location" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_sku_occurred_at",
                schema: "inventory",
                table: "stock_movements",
                columns: new[] { "sku", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_order_ref",
                schema: "inventory",
                table: "stock_reservations",
                column: "order_ref");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allocations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "batches",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "handling_unit_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "location_snapshots",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "product_snapshots",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_reservations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "handling_units",
                schema: "inventory");
        }
    }
}
