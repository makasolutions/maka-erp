using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_V2_FiscalDataOnParty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FiscalData_AgenteRetencionICA",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalData_AgenteRetencionIVA",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalData_Autorretenedor",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalData_FlagPEP",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalData_FormaJuridica",
                schema: "parties",
                table: "Parties",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalData_GranContribuyente",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalData_ObligadoLlevarContabilidad",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "FiscalData_RegimenTributario",
                schema: "parties",
                table: "Parties",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "FiscalData_ResponsabilidadIVA",
                schema: "parties",
                table: "Parties",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalData_ResponsabilidadesFiscales",
                schema: "parties",
                table: "Parties",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalData_AgenteRetencionICA",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_AgenteRetencionIVA",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_Autorretenedor",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_FlagPEP",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_FormaJuridica",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_GranContribuyente",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_ObligadoLlevarContabilidad",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_RegimenTributario",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_ResponsabilidadIVA",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "FiscalData_ResponsabilidadesFiscales",
                schema: "parties",
                table: "Parties");
        }
    }
}
