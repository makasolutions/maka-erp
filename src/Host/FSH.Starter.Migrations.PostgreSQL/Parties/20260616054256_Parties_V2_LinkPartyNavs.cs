using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_V2_LinkPartyNavs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierProfiles_PartyId",
                schema: "parties",
                table: "SupplierProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PartnerProfiles_PartyId",
                schema: "parties",
                table: "PartnerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_PartyId",
                schema: "parties",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_PartyId",
                schema: "parties",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ContactProfiles_PartyId",
                schema: "parties",
                table: "ContactProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProfiles_PartyId",
                schema: "parties",
                table: "SupplierProfiles",
                columns: new[] { "PartyId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProfiles_PartyId1",
                schema: "parties",
                table: "SupplierProfiles",
                column: "PartyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProfiles_PartyId",
                schema: "parties",
                table: "PartnerProfiles",
                columns: new[] { "PartyId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProfiles_PartyId1",
                schema: "parties",
                table: "PartnerProfiles",
                column: "PartyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PartyId",
                schema: "parties",
                table: "EmployeeProfiles",
                columns: new[] { "PartyId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PartyId1",
                schema: "parties",
                table: "EmployeeProfiles",
                column: "PartyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_PartyId",
                schema: "parties",
                table: "CustomerProfiles",
                columns: new[] { "PartyId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_PartyId1",
                schema: "parties",
                table: "CustomerProfiles",
                column: "PartyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactProfiles_PartyId",
                schema: "parties",
                table: "ContactProfiles",
                columns: new[] { "PartyId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactProfiles_PartyId1",
                schema: "parties",
                table: "ContactProfiles",
                column: "PartyId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactProfiles_Parties_PartyId",
                schema: "parties",
                table: "ContactProfiles",
                column: "PartyId",
                principalSchema: "parties",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditAccounts_CustomerProfiles_CustomerProfileId",
                schema: "parties",
                table: "CreditAccounts",
                column: "CustomerProfileId",
                principalSchema: "parties",
                principalTable: "CustomerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerProfiles_Parties_PartyId",
                schema: "parties",
                table: "CustomerProfiles",
                column: "PartyId",
                principalSchema: "parties",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeProfiles_Parties_PartyId",
                schema: "parties",
                table: "EmployeeProfiles",
                column: "PartyId",
                principalSchema: "parties",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartnerProfiles_Parties_PartyId",
                schema: "parties",
                table: "PartnerProfiles",
                column: "PartyId",
                principalSchema: "parties",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartyCiiuActivities_Parties_PartyId",
                schema: "parties",
                table: "PartyCiiuActivities",
                column: "PartyId",
                principalSchema: "parties",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierProfiles_Parties_PartyId",
                schema: "parties",
                table: "SupplierProfiles",
                column: "PartyId",
                principalSchema: "parties",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactProfiles_Parties_PartyId",
                schema: "parties",
                table: "ContactProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditAccounts_CustomerProfiles_CustomerProfileId",
                schema: "parties",
                table: "CreditAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerProfiles_Parties_PartyId",
                schema: "parties",
                table: "CustomerProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeProfiles_Parties_PartyId",
                schema: "parties",
                table: "EmployeeProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_PartnerProfiles_Parties_PartyId",
                schema: "parties",
                table: "PartnerProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_PartyCiiuActivities_Parties_PartyId",
                schema: "parties",
                table: "PartyCiiuActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierProfiles_Parties_PartyId",
                schema: "parties",
                table: "SupplierProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SupplierProfiles_PartyId",
                schema: "parties",
                table: "SupplierProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SupplierProfiles_PartyId1",
                schema: "parties",
                table: "SupplierProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PartnerProfiles_PartyId",
                schema: "parties",
                table: "PartnerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PartnerProfiles_PartyId1",
                schema: "parties",
                table: "PartnerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_PartyId",
                schema: "parties",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_PartyId1",
                schema: "parties",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_PartyId",
                schema: "parties",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_PartyId1",
                schema: "parties",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ContactProfiles_PartyId",
                schema: "parties",
                table: "ContactProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ContactProfiles_PartyId1",
                schema: "parties",
                table: "ContactProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProfiles_PartyId",
                schema: "parties",
                table: "SupplierProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProfiles_PartyId",
                schema: "parties",
                table: "PartnerProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PartyId",
                schema: "parties",
                table: "EmployeeProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_PartyId",
                schema: "parties",
                table: "CustomerProfiles",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactProfiles_PartyId",
                schema: "parties",
                table: "ContactProfiles",
                column: "PartyId");
        }
    }
}
