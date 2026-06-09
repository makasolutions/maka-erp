using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_AddressNormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                schema: "parties",
                table: "PartyAddresses",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MunicipalityCode",
                schema: "parties",
                table: "PartyAddresses",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NormalizedAtUtc",
                schema: "parties",
                table: "PartyAddresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLine",
                schema: "parties",
                table: "PartyAddresses",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                schema: "parties",
                table: "PartyAddresses");

            migrationBuilder.DropColumn(
                name: "MunicipalityCode",
                schema: "parties",
                table: "PartyAddresses");

            migrationBuilder.DropColumn(
                name: "NormalizedAtUtc",
                schema: "parties",
                table: "PartyAddresses");

            migrationBuilder.DropColumn(
                name: "NormalizedLine",
                schema: "parties",
                table: "PartyAddresses");
        }
    }
}
