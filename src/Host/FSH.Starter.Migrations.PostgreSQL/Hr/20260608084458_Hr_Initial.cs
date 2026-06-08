using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Hr
{
    /// <inheritdoc />
    public partial class Hr_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MainCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    IsVendedor = table.Column<bool>(type: "boolean", nullable: false),
                    IsCobrador = table.Column<bool>(type: "boolean", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CostCenterCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SeniorityDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PayrollEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    HealthProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PensionFundCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SeveranceFundCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CcfCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ArlProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PaymentMethodCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LaborDepartmentCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PositionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImmediateBossPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PositionStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ContractDurationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ContractStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ArlRiskLevelCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    HighPensionRisk = table.Column<bool>(type: "boolean", nullable: false),
                    RestDays = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AppliesLaw1607 = table.Column<bool>(type: "boolean", nullable: false),
                    SalaryTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BaseSalary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    SalaryStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalTransportAllowance = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PartyId",
                schema: "hr",
                table: "Employees",
                columns: new[] { "PartyId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees",
                schema: "hr");
        }
    }
}
