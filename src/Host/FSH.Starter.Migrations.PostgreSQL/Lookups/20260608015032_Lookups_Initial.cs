using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Lookups
{
    /// <inheritdoc />
    public partial class Lookups_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lookups");

            migrationBuilder.CreateTable(
                name: "BasicTables",
                schema: "lookups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsManageable = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    VisibleInMenu = table.Column<bool>(type: "boolean", nullable: false),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasicTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BasicRecords",
                schema: "lookups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BasicTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasicRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BasicRecords_BasicTables_BasicTableId",
                        column: x => x.BasicTableId,
                        principalSchema: "lookups",
                        principalTable: "BasicTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BasicRecords_BasicTableId",
                schema: "lookups",
                table: "BasicRecords",
                column: "BasicTableId");

            migrationBuilder.CreateIndex(
                name: "IX_BasicRecords_BasicTableId_Code",
                schema: "lookups",
                table: "BasicRecords",
                columns: new[] { "BasicTableId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BasicTables_IsDeleted",
                schema: "lookups",
                table: "BasicTables",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BasicTables_TenantId_Code",
                schema: "lookups",
                table: "BasicTables",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasicRecords",
                schema: "lookups");

            migrationBuilder.DropTable(
                name: "BasicTables",
                schema: "lookups");
        }
    }
}
