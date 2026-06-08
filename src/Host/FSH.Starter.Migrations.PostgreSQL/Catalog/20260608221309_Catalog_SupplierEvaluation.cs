using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class Catalog_SupplierEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScorecardKpis",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScorecardKpis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierScorecards",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodLabel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WeightedScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Grade = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierScorecards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScorecardCriteria",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorecardId = table.Column<Guid>(type: "uuid", nullable: false),
                    KpiCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KpiName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScorecardCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScorecardCriteria_SupplierScorecards_ScorecardId",
                        column: x => x.ScorecardId,
                        principalSchema: "catalog",
                        principalTable: "SupplierScorecards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScorecardCriteria_ScorecardId",
                schema: "catalog",
                table: "ScorecardCriteria",
                column: "ScorecardId");

            migrationBuilder.CreateIndex(
                name: "IX_ScorecardKpis_Code",
                schema: "catalog",
                table: "ScorecardKpis",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierScorecards_SupplierId",
                schema: "catalog",
                table: "SupplierScorecards",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierScorecards_SupplierId_PeriodStart",
                schema: "catalog",
                table: "SupplierScorecards",
                columns: new[] { "SupplierId", "PeriodStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScorecardCriteria",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ScorecardKpis",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "SupplierScorecards",
                schema: "catalog");
        }
    }
}
