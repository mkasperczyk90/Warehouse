using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Warehousing.Topology.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "topology");

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "topology",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "docks",
                schema: "topology",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(10)", nullable: false),
                    direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_docks", x => new { x.warehouse_code, x.code });
                    table.ForeignKey(
                        name: "FK_docks_warehouses_warehouse_code",
                        column: x => x.warehouse_code,
                        principalSchema: "topology",
                        principalTable: "warehouses",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                schema: "topology",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(10)", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    temp_min_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    temp_max_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    humidity_controlled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => new { x.warehouse_code, x.code });
                    table.ForeignKey(
                        name: "FK_rooms_warehouses_warehouse_code",
                        column: x => x.warehouse_code,
                        principalSchema: "topology",
                        principalTable: "warehouses",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                schema: "topology",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(10)", nullable: false),
                    room_code = table.Column<string>(type: "character varying(10)", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    capacity_m3 = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    max_load_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => new { x.warehouse_code, x.room_code, x.code });
                    table.ForeignKey(
                        name: "FK_locations_rooms_warehouse_code_room_code",
                        columns: x => new { x.warehouse_code, x.room_code },
                        principalSchema: "topology",
                        principalTable: "rooms",
                        principalColumns: new[] { "warehouse_code", "code" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "docks",
                schema: "topology");

            migrationBuilder.DropTable(
                name: "locations",
                schema: "topology");

            migrationBuilder.DropTable(
                name: "rooms",
                schema: "topology");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "topology");
        }
    }
}
