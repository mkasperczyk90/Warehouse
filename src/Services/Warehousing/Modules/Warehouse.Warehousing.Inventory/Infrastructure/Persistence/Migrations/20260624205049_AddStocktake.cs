using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStocktake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stocktakes",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ordered_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocktakes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stocktake_count_lines",
                schema: "inventory",
                columns: table => new
                {
                    stocktake_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    location = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    batch = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    counted_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    counted_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    expected_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    expected_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    counted_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocktake_count_lines", x => new { x.stocktake_id, x.Id });
                    table.ForeignKey(
                        name: "FK_stocktake_count_lines_stocktakes_stocktake_id",
                        column: x => x.stocktake_id,
                        principalSchema: "inventory",
                        principalTable: "stocktakes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stocktake_count_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stocktakes",
                schema: "inventory");
        }
    }
}
