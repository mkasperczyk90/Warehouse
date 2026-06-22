using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Warehouse.MasterData.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "product_types",
                schema: "catalog",
                columns: table => new
                {
                    sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ean = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    length_cm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    width_cm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    unit_weight_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    base_unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    temp_min_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    temp_max_c = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    requires_cold_chain = table.Column<bool>(type: "boolean", nullable: false),
                    is_hazardous = table.Column<bool>(type: "boolean", nullable: false),
                    is_batch_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    has_expiry_date = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_types", x => x.sku);
                });

            migrationBuilder.CreateTable(
                name: "product_unit_conversions",
                schema: "catalog",
                columns: table => new
                {
                    ProductTypeId = table.Column<string>(type: "character varying(32)", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    factor_to_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_unit_conversions", x => new { x.ProductTypeId, x.Id });
                    table.ForeignKey(
                        name: "FK_product_unit_conversions_product_types_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalSchema: "catalog",
                        principalTable: "product_types",
                        principalColumn: "sku",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_types_ean",
                schema: "catalog",
                table: "product_types",
                column: "ean",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_unit_conversions",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_types",
                schema: "catalog");
        }
    }
}
