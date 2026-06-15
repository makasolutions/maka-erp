using FSH.Framework.Shared.Constants;

namespace FSH.Modules.NamingSeries.Contracts.Authorization;

/// <summary>Permisos del módulo NamingSeries (ADR-0007). Tenant-aislado.</summary>
public static class NamingSeriesPermissions
{
    public const string Resource = "NamingSeries";

    public const string View   = $"Permissions.{Resource}.View";
    public const string Create = $"Permissions.{Resource}.Create";
    public const string Close  = $"Permissions.{Resource}.Close";

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Naming Series",   ActionConstants.View,   Resource, IsBasic: true),
        new("Create Naming Series", ActionConstants.Create, Resource),
        new("Close Naming Series",  "Close",                Resource),
    ];
}
