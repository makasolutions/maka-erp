using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class Catalog_CategoryGoogleTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullPath",
                schema: "catalog",
                table: "Categories",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoogleCategoryId",
                schema: "catalog",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RootGoogleCategoryId",
                schema: "catalog",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Industries",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Industries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantIndustries",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndustryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantIndustries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndustryCategories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndustryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootGoogleCategoryId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndustryCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndustryCategories_Industries_IndustryId",
                        column: x => x.IndustryId,
                        principalSchema: "catalog",
                        principalTable: "Industries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_GoogleCategoryId",
                schema: "catalog",
                table: "Categories",
                column: "GoogleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_RootGoogleCategoryId",
                schema: "catalog",
                table: "Categories",
                column: "RootGoogleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Industries_Code",
                schema: "catalog",
                table: "Industries",
                columns: new[] { "Code", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndustryCategories_IndustryId_RootGoogleCategoryId",
                schema: "catalog",
                table: "IndustryCategories",
                columns: new[] { "IndustryId", "RootGoogleCategoryId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantIndustries_IndustryId",
                schema: "catalog",
                table: "TenantIndustries",
                column: "IndustryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndustryCategories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "TenantIndustries",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Industries",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_Categories_GoogleCategoryId",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_RootGoogleCategoryId",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "FullPath",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "GoogleCategoryId",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "RootGoogleCategoryId",
                schema: "catalog",
                table: "Categories");
        }
    }
}
