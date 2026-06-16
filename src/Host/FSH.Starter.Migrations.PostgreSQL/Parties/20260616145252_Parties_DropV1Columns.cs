using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_DropV1Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parties_Roles",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "ActividadEconomicaCiiuCode",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CreditBlocked",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CreditCurrency",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CreditDaysCode",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalResponsibilities",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "HasCredit",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "Roles",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "TaxRegimeCode",
                schema: "parties",
                table: "Parties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "CreditCurrency",
                schema: "parties",
                table: "Parties",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditDaysCode",
                schema: "parties",
                table: "Parties",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                schema: "parties",
                table: "Parties",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalResponsibilities",
                schema: "parties",
                table: "Parties",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCredit",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Roles",
                schema: "parties",
                table: "Parties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TaxRegimeCode",
                schema: "parties",
                table: "Parties",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Roles",
                schema: "parties",
                table: "Parties",
                column: "Roles");
        }
    }
}
