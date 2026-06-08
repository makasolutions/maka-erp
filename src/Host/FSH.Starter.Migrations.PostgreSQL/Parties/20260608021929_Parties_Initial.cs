using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "parties");

            migrationBuilder.CreateTable(
                name: "Parties",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentificationTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdentificationNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerificationDigit = table.Column<int>(type: "integer", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Website = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TaxRegimeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FiscalResponsibilities = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Roles = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Stage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LeadScore = table.Column<int>(type: "integer", nullable: false),
                    SourceCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarketingType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    GenderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MaritalStatusCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreditCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartyAddresses",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Department = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Line = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyAddresses_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "parties",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyChannels",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyChannels_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "parties",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyContacts",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Cell = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsCommercial = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyContacts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "parties",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyTeamMembers",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyTeamMembers_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "parties",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parties_AssignedUserId",
                schema: "parties",
                table: "Parties",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_IdentificationTypeCode_IdentificationNumber",
                schema: "parties",
                table: "Parties",
                columns: new[] { "IdentificationTypeCode", "IdentificationNumber", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_IsDeleted",
                schema: "parties",
                table: "Parties",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_LegalName",
                schema: "parties",
                table: "Parties",
                column: "LegalName");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Roles",
                schema: "parties",
                table: "Parties",
                column: "Roles");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAddresses_PartyId",
                schema: "parties",
                table: "PartyAddresses",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyChannels_PartyId",
                schema: "parties",
                table: "PartyChannels",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId",
                schema: "parties",
                table: "PartyContacts",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyTeamMembers_PartyId",
                schema: "parties",
                table: "PartyTeamMembers",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyTeamMembers_UserId",
                schema: "parties",
                table: "PartyTeamMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyAddresses",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "PartyChannels",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "PartyContacts",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "PartyTeamMembers",
                schema: "parties");

            migrationBuilder.DropTable(
                name: "Parties",
                schema: "parties");
        }
    }
}
