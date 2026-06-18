using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.SharedRecords
{
    /// <inheritdoc />
    public partial class SharedRecords_AddAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.CreateTable(
                name: "Addresses",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Department = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DepartmentCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    MunicipalityCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Line = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Barrio = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_OwnerType_OwnerId",
                schema: "shared",
                table: "Addresses",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "ux_shared_addresses_primary_per_owner",
                schema: "shared",
                table: "Addresses",
                columns: new[] { "TenantId", "OwnerType", "OwnerId" },
                unique: true,
                filter: "\"IsPrimary\" = TRUE AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses",
                schema: "shared");
        }
    }
}
