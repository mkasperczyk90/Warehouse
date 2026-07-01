using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Logistics.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "logistics");

            migrationBuilder.CreateTable(
                name: "inbound_deliveries",
                schema: "logistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    planned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slot_dock_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    slot_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    slot_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound_deliveries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbound_orders",
                schema: "logistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ship_to_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ship_to_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ship_to_postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ship_to_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    required_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pick_lists",
                schema: "logistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pick_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                schema: "logistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carrier_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tracking_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbound_delivery_lines",
                schema: "logistics",
                columns: table => new
                {
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expected_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    expected_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    pack_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    pack_factor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    actual_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    batch_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    batch_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    discrepancy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound_delivery_lines", x => new { x.delivery_id, x.line_no });
                    table.ForeignKey(
                        name: "FK_inbound_delivery_lines_inbound_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "logistics",
                        principalTable: "inbound_deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbound_order_lines",
                schema: "logistics",
                columns: table => new
                {
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordered_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ordered_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_order_lines", x => new { x.order_id, x.line_no });
                    table.ForeignKey(
                        name: "FK_outbound_order_lines_outbound_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "logistics",
                        principalTable: "outbound_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pick_tasks",
                schema: "logistics",
                columns: table => new
                {
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    pick_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    batch_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    batch_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    qty_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    qty_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    handled_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pick_tasks", x => new { x.pick_list_id, x.sequence });
                    table.ForeignKey(
                        name: "FK_pick_tasks_pick_lists_pick_list_id",
                        column: x => x.pick_list_id,
                        principalSchema: "logistics",
                        principalTable: "pick_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "packages",
                schema: "logistics",
                columns: table => new
                {
                    number = table.Column<int>(type: "integer", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packages", x => new { x.shipment_id, x.number });
                    table.ForeignKey(
                        name: "FK_packages_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalSchema: "logistics",
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pick_lists_order_id",
                schema: "logistics",
                table: "pick_lists",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_order_id",
                schema: "logistics",
                table: "shipments",
                column: "order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbound_delivery_lines",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "outbound_order_lines",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "packages",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "pick_tasks",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "inbound_deliveries",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "outbound_orders",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "shipments",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "pick_lists",
                schema: "logistics");
        }
    }
}
