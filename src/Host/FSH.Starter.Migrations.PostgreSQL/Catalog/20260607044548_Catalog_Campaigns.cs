using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class Catalog_Campaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignStatus",
                schema: "catalog",
                table: "PriceLists",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndJobId",
                schema: "catalog",
                table: "PriceLists",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartJobId",
                schema: "catalog",
                table: "PriceLists",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreCampaignPrice",
                schema: "catalog",
                table: "PriceListItems",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignStatus",
                schema: "catalog",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "EndJobId",
                schema: "catalog",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "StartJobId",
                schema: "catalog",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "PreCampaignPrice",
                schema: "catalog",
                table: "PriceListItems");
        }
    }
}
