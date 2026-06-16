using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_V2_LegalRepresentative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RepLegal_Apellidos",
                schema: "parties",
                table: "Parties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepLegal_Celular",
                schema: "parties",
                table: "Parties",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepLegal_Email",
                schema: "parties",
                table: "Parties",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RepLegal_EsPEP",
                schema: "parties",
                table: "Parties",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepLegal_Nombres",
                schema: "parties",
                table: "Parties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepLegal_NumeroIdentificacion",
                schema: "parties",
                table: "Parties",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepLegal_Telefono",
                schema: "parties",
                table: "Parties",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepLegal_TelefonoExtension",
                schema: "parties",
                table: "Parties",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "RepLegal_TipoIdentificacion",
                schema: "parties",
                table: "Parties",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepLegal_Apellidos",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_Celular",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_Email",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_EsPEP",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_Nombres",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_NumeroIdentificacion",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_Telefono",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_TelefonoExtension",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "RepLegal_TipoIdentificacion",
                schema: "parties",
                table: "Parties");
        }
    }
}
