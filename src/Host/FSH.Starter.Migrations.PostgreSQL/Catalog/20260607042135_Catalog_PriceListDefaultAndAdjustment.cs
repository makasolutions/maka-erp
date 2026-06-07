using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class Catalog_PriceListDefaultAndAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdjustmentPercent",
                schema: "catalog",
                table: "PriceLists",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                schema: "catalog",
                table: "PriceLists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ListKind",
                schema: "catalog",
                table: "PriceLists",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Segment");

            migrationBuilder.AddColumn<bool>(
                name: "IsManualOverride",
                schema: "catalog",
                table: "PriceListItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustmentPercent",
                schema: "catalog",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                schema: "catalog",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "ListKind",
                schema: "catalog",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "IsManualOverride",
                schema: "catalog",
                table: "PriceListItems");
        }
    }
}
