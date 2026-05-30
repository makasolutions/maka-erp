using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class CatalogAddCodeImageActiveVisible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                schema: "catalog",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "catalog",
                table: "Categories",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "catalog",
                table: "Categories",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "catalog",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                schema: "catalog",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "catalog",
                table: "Brands",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "catalog",
                table: "Brands",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                schema: "catalog",
                table: "Brands",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Backfill a non-empty, unique-per-tenant Code for pre-existing rows
            // (derived from the already-unique Slug) so the unique index below
            // does not collide on the empty-string default.
            migrationBuilder.Sql(
                "UPDATE catalog.\"Categories\" SET \"Code\" = UPPER(LEFT(\"Slug\", 32)) WHERE \"Code\" = '';");
            migrationBuilder.Sql(
                "UPDATE catalog.\"Brands\" SET \"Code\" = UPPER(LEFT(\"Slug\", 32)) WHERE \"Code\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Code",
                schema: "catalog",
                table: "Categories",
                columns: new[] { "Code", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Code",
                schema: "catalog",
                table: "Brands",
                columns: new[] { "Code", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Code",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Code",
                schema: "catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                schema: "catalog",
                table: "Brands");
        }
    }
}
