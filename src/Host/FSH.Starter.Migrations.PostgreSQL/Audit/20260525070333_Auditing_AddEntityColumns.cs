using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Audit
{
    /// <inheritdoc />
    public partial class AuditingAddEntityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Denormalized columns extracted from PayloadJson for EntityChange events.
            // Null for all other event types (Security, Activity, Exception).
            migrationBuilder.AddColumn<string>(
                name: "EntityName",
                schema: "audit",
                table: "AuditRecords",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityKey",
                schema: "audit",
                table: "AuditRecords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityOperation",
                schema: "audit",
                table: "AuditRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Composite index for "list all changes to entity type X ordered by time"
            // — covers the EntityAuditSection and the entity-name filter dropdown.
            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_Tenant_EntityName_OccurredAt",
                schema: "audit",
                table: "AuditRecords",
                columns: new[] { "TenantId", "EntityName", "OccurredAtUtc" },
                descending: new[] { false, false, true });

            // GIN trigram index for ILIKE queries on EntityKey (typically a GUID).
            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_EntityKey_trgm",
                schema: "audit",
                table: "AuditRecords",
                column: "EntityKey")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_Tenant_EntityName_OccurredAt",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_EntityKey_trgm",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropColumn(
                name: "EntityName",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropColumn(
                name: "EntityKey",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropColumn(
                name: "EntityOperation",
                schema: "audit",
                table: "AuditRecords");
        }
    }
}
