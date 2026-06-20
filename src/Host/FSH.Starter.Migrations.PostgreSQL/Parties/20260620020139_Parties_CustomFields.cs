using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Parties
{
    /// <inheritdoc />
    public partial class Parties_CustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "CustomFields",
                schema: "parties",
                table: "Parties",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                schema: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<short>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApiSlug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FieldType = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsUnique = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultValueEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsMultiselect = table.Column<bool>(type: "boolean", nullable: false),
                    Options = table.Column<string>(type: "jsonb", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customfielddef_slug",
                schema: "parties",
                table: "CustomFieldDefinitions",
                columns: new[] { "TenantId", "EntityType", "ApiSlug" },
                unique: true,
                filter: "\"Activo\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_EntityType",
                schema: "parties",
                table: "CustomFieldDefinitions",
                column: "EntityType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions",
                schema: "parties");

            migrationBuilder.DropColumn(
                name: "CustomFields",
                schema: "parties",
                table: "Parties");
        }
    }
}
