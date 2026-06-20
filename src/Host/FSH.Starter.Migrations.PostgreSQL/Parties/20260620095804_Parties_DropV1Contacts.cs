using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_DropV1Contacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyContacts",
                schema: "parties");

            migrationBuilder.DropColumn(
                name: "ContactFunction",
                schema: "parties",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "IsCommercialContact",
                schema: "parties",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                schema: "parties",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                schema: "parties",
                table: "ContactProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "ContactFunction",
                schema: "parties",
                table: "ContactProfiles",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommercialContact",
                schema: "parties",
                table: "ContactProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                schema: "parties",
                table: "ContactProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                schema: "parties",
                table: "ContactProfiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartyContacts",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Cell = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ContactTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FullName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    GenderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IdentificationNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IdentificationTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsCommercial = table.Column<bool>(type: "boolean", nullable: false),
                    LastName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    MaritalStatusCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PositionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProfessionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId",
                schema: "parties",
                table: "PartyContacts",
                column: "PartyId");
        }
    }
}
