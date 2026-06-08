using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class Catalog_Agreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agreements",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: true),
                    SuggestedPriceListId = table.Column<Guid>(type: "uuid", nullable: true),
                    DispatchResponsible = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WaybillResponsible = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SettlementResponsible = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FailedDeliveryPolicy = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReturnsPolicy = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WarrantyPolicy = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgreementRules",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NumericValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BoolValue = table.Column<bool>(type: "boolean", nullable: true),
                    TextValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgreementRules_Agreements_AgreementId",
                        column: x => x.AgreementId,
                        principalSchema: "catalog",
                        principalTable: "Agreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgreementRules_AgreementId",
                schema: "catalog",
                table: "AgreementRules",
                column: "AgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_Agreements_Status",
                schema: "catalog",
                table: "Agreements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Agreements_SupplierId",
                schema: "catalog",
                table: "Agreements",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgreementRules",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Agreements",
                schema: "catalog");
        }
    }
}
