using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_V2_SupplierCurrencyNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DefaultCurrencyId",
                schema: "parties",
                table: "SupplierProfiles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DefaultCurrencyId",
                schema: "parties",
                table: "SupplierProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
