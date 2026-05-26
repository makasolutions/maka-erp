using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain;

/// <summary>
/// Appearance preferences scoped to a specific user within a tenant.
/// One row per (tenant, user) pair. Controls theme, accent colour, font family,
/// and layout density for the React dashboard. Backed by the database so settings
/// survive across sessions and devices, and are isolated per user.
/// </summary>
public class TenantAppearance : IAuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user this appearance row belongs to (string form of the user's Guid).
    /// Stored as string to avoid a FK dependency on the ASP.NET Identity user table
    /// from this domain entity, keeping the same pattern as <see cref="TenantLocalization"/>.
    /// </summary>
    public string UserId { get; private set; } = default!;

    /// <summary>"light" | "dark" | "system"</summary>
    public string Theme { get; private set; } = default!;

    /// <summary>
    /// Preset accent id (e.g. "rose", "indigo") or "custom".
    /// </summary>
    public string Accent { get; private set; } = default!;

    /// <summary>
    /// Font family id (e.g. "inter", "geist", "mono").
    /// </summary>
    public string Font { get; private set; } = default!;

    /// <summary>
    /// Layout density: "compact" | "default" | "comfortable".
    /// </summary>
    public string Density { get; private set; } = default!;

    /// <summary>
    /// JSON blob for custom accent stops — only meaningful when Accent == "custom".
    /// Stored as a plain string so we avoid a JSONB dependency in this domain entity.
    /// </summary>
    public string? CustomAccentJson { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private TenantAppearance() { } // EF Core

    public static TenantAppearance CreateDefault(string userId, string? createdBy = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Theme = "system",
            Accent = "rose",
            Font = "inter",
            Density = "default",
            CustomAccentJson = null,
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy,
        };

    public void Update(
        string theme,
        string accent,
        string font,
        string density,
        string? customAccentJson,
        string? modifiedBy = null)
    {
        Theme = theme;
        Accent = accent;
        Font = font;
        Density = density;
        CustomAccentJson = customAccentJson;
        LastModifiedOnUtc = TimeProvider.System.GetUtcNow();
        LastModifiedBy = modifiedBy;
    }
}
