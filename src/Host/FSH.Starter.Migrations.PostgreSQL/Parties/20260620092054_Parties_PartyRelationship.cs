using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_PartyRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalData_EmailFacturacion",
                schema: "parties",
                table: "Parties",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPEP",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PepType",
                schema: "parties",
                table: "Parties",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartyRelationships",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContactFunctionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    JobTitleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomFields = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyRelationships_Parties_SourcePartyId",
                        column: x => x.SourcePartyId,
                        principalSchema: "parties",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyRelationships_Parties_TargetPartyId",
                        column: x => x.TargetPartyId,
                        principalSchema: "parties",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_partyrel_primary",
                schema: "parties",
                table: "PartyRelationships",
                columns: new[] { "TenantId", "TargetPartyId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_PartyRelationships_SourcePartyId",
                schema: "parties",
                table: "PartyRelationships",
                column: "SourcePartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyRelationships_TargetPartyId",
                schema: "parties",
                table: "PartyRelationships",
                column: "TargetPartyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyRelationships",
                schema: "parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_EmailFacturacion",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "IsPEP",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "PepType",
                schema: "parties",
                table: "Parties");
        }
    }
}
