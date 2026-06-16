using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_V2_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactProfiles",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ContactFunction = table.Column<short>(type: "smallint", nullable: true),
                    IsCommercialContact = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    ResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditAccounts",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CupoAsignado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoDisponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MonedaId = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DiasCredito = table.Column<int>(type: "integer", nullable: false),
                    FormaPagoId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstaActivo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAprobacion = table.Column<DateOnly>(type: "date", nullable: true),
                    AprobadoPor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerProfiles",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultSalespersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    WithholdingRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxDiscountPct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AllowDiscount = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingSourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeProfiles",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManagerPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsSalesperson = table.Column<bool>(type: "boolean", nullable: false),
                    IsCollector = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerProfiles",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartyCiiuActivities",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CiiuCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    IsPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyCiiuActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartyHolds",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldType = table.Column<short>(type: "smallint", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaLiberacion = table.Column<DateOnly>(type: "date", nullable: true),
                    EstaActivo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoPor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyHolds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierProfiles",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultCurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTerms_DiasCredito = table.Column<int>(type: "integer", nullable: false),
                    PaymentTerms_FormaPagoId = table.Column<Guid>(type: "uuid", nullable: true),
                    WithholdingRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultPriceListId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDropshipping = table.Column<bool>(type: "boolean", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: true),
                    DefaultBankAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditMovements",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<short>(type: "smallint", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoResultante = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DocumentoReferenciaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FechaUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistradoPor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditMovements_CreditAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalSchema: "parties",
                        principalTable: "CreditAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactProfiles_PartyId",
                schema: "parties",
                table: "ContactProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccounts_CustomerProfileId",
                schema: "parties",
                table: "CreditAccounts",
                column: "CustomerProfileId");

            migrationBuilder.CreateIndex(
                name: "ix_creditmov_account",
                schema: "parties",
                table: "CreditMovements",
                columns: new[] { "CreditAccountId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_PartyId",
                schema: "parties",
                table: "CustomerProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PartyId",
                schema: "parties",
                table: "EmployeeProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProfiles_PartyId",
                schema: "parties",
                table: "PartnerProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "ix_ciiu_principal",
                schema: "parties",
                table: "PartyCiiuActivities",
                columns: new[] { "PartyId", "TenantId" },
                unique: true,
                filter: "\"IsPrincipal\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_holds_active",
                schema: "parties",
                table: "PartyHolds",
                columns: new[] { "PartyId", "HoldType" },
                filter: "\"EstaActivo\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProfiles_PartyId",
                schema: "parties",
                table: "SupplierProfiles",
                column: "PartyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactProfiles",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "CreditMovements",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "CustomerProfiles",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "EmployeeProfiles",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "PartnerProfiles",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "PartyCiiuActivities",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "PartyHolds",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "SupplierProfiles",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "CreditAccounts",
                schema: "parties");
        }
    }
}
