using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Identity
{
    /// <inheritdoc />
    public partial class AddUserIdToTenantAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            // Add UserId column with a default so existing rows don't violate NOT NULL.
            // Existing rows get a sentinel GUID that won't match any real user, which
            // is acceptable — they will be re-created on first login per-user.
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                schema: "identity",
                table: "TenantAppearances",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "00000000-0000-0000-0000-000000000000");

            // Unique index — one appearance row per (tenant, user).
            // Finbuckle's global query filter handles the tenant scope.
            migrationBuilder.CreateIndex(
                name: "IX_TenantAppearances_UserId",
                schema: "identity",
                table: "TenantAppearances",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_TenantAppearances_UserId",
                schema: "identity",
                table: "TenantAppearances");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "identity",
                table: "TenantAppearances");
        }
    }
}
