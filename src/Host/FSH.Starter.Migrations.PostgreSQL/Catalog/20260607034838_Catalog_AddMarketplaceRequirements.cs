using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class Catalog_AddMarketplaceRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketplaceAttributeRequirements",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Marketplace = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceAttributeRequirements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAttributeRequirements_CategoryId",
                schema: "catalog",
                table: "MarketplaceAttributeRequirements",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAttributeRequirements_Marketplace_CategoryId_Att~",
                schema: "catalog",
                table: "MarketplaceAttributeRequirements",
                columns: new[] { "Marketplace", "CategoryId", "AttributeId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceAttributeRequirements",
                schema: "catalog");
        }
    }
}
