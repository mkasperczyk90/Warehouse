using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Warehouse.MasterData.Partners.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "partners");

            migrationBuilder.CreateTable(
                name: "parties",
                schema: "partners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tax_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_roles",
                schema: "partners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    role_type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Services = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_party_roles_parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "partners",
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_shipping_addresses",
                schema: "partners",
                columns: table => new
                {
                    CustomerRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_shipping_addresses", x => new { x.CustomerRoleId, x.Id });
                    table.ForeignKey(
                        name: "FK_customer_shipping_addresses_party_roles_CustomerRoleId",
                        column: x => x.CustomerRoleId,
                        principalSchema: "partners",
                        principalTable: "party_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_parties_tax_id",
                schema: "partners",
                table: "parties",
                column: "tax_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_party_roles_PartyId",
                schema: "partners",
                table: "party_roles",
                column: "PartyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_shipping_addresses",
                schema: "partners");

            migrationBuilder.DropTable(
                name: "party_roles",
                schema: "partners");

            migrationBuilder.DropTable(
                name: "parties",
                schema: "partners");
        }
    }
}
