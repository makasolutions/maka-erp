using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.catalog
{
    /// <inheritdoc />
    public partial class Catalog_AddPriceBulkProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceBulkProposals",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PriceListItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    NewPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceBulkProposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceBulkProposals_BatchId",
                schema: "catalog",
                table: "PriceBulkProposals",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceBulkProposals_Status",
                schema: "catalog",
                table: "PriceBulkProposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PriceBulkProposals_SupplierId_Status",
                schema: "catalog",
                table: "PriceBulkProposals",
                columns: new[] { "SupplierId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceBulkProposals",
                schema: "catalog");
        }
    }
}
