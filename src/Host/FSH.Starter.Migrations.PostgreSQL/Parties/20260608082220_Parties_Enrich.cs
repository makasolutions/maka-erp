using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_Enrich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Label",
                schema: "parties",
                table: "PartyAddresses",
                newName: "LabelCode");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AreaCode",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                schema: "parties",
                table: "PartyContacts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactTypeCode",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenderCode",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentificationNumber",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentificationTypeCode",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatusCode",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PositionCode",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionCode",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barrio",
                schema: "parties",
                table: "PartyAddresses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActividadEconomicaCiiuCode",
                schema: "parties",
                table: "Parties",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CreditBlocked",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CreditDaysCode",
                schema: "parties",
                table: "Parties",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "parties",
                table: "Parties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCredit",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "parties",
                table: "Parties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaCode",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "ContactTypeCode",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "GenderCode",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "IdentificationNumber",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "IdentificationTypeCode",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "MaritalStatusCode",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "PositionCode",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "ProfessionCode",
                schema: "parties",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "Barrio",
                schema: "parties",
                table: "PartyAddresses");

            migrationBuilder.DropColumn(
                name: "ActividadEconomicaCiiuCode",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CreditBlocked",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CreditDaysCode",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "HasCredit",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "parties",
                table: "Parties");

            migrationBuilder.RenameColumn(
                name: "LabelCode",
                schema: "parties",
                table: "PartyAddresses",
                newName: "Label");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                schema: "parties",
                table: "PartyContacts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(240)",
                oldMaxLength: 240,
                oldNullable: true);
        }
    }
}
