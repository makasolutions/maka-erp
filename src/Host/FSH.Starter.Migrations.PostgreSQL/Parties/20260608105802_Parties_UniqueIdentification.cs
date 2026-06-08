using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_UniqueIdentification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parties_IdentificationTypeCode_IdentificationNumber",
                schema: "parties",
                table: "Parties");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_TenantId_IdentificationTypeCode_IdentificationNumber",
                schema: "parties",
                table: "Parties",
                columns: new[] { "TenantId", "IdentificationTypeCode", "IdentificationNumber" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parties_TenantId_IdentificationTypeCode_IdentificationNumber",
                schema: "parties",
                table: "Parties");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_IdentificationTypeCode_IdentificationNumber",
                schema: "parties",
                table: "Parties",
                columns: new[] { "IdentificationTypeCode", "IdentificationNumber", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
