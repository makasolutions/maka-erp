using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class Catalog_SupplierCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierCategories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCategories_SupplierId",
                schema: "catalog",
                table: "SupplierCategories",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCategories_SupplierId_CategoryId",
                schema: "catalog",
                table: "SupplierCategories",
                columns: new[] { "SupplierId", "CategoryId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierCategories",
                schema: "catalog");
        }
    }
}
