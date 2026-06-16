using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_V2_ParentPartyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentPartyId",
                schema: "parties",
                table: "Parties",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_parties_parent",
                schema: "parties",
                table: "Parties",
                columns: new[] { "TenantId", "ParentPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parties_ParentPartyId",
                schema: "parties",
                table: "Parties",
                column: "ParentPartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parties_Parties_ParentPartyId",
                schema: "parties",
                table: "Parties",
                column: "ParentPartyId",
                principalSchema: "parties",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parties_Parties_ParentPartyId",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropIndex(
                name: "ix_parties_parent",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropIndex(
                name: "IX_Parties_ParentPartyId",
                schema: "parties",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "ParentPartyId",
                schema: "parties",
                table: "Parties");
        }
    }
}
