using FSH.Framework.Shared.Constants;

namespace FSH.Modules.SharedRecords.Contracts.Authorization;

/// <summary>
/// Permisos del módulo SharedRecords (controles genéricos polimórficos). Tenant-aislado.
/// Cada control de lista (Address ahora; Phone/Contact/… después) expone View/Manage.
/// </summary>
public static class SharedRecordsPermissions
{
    public static class Addresses
    {
        public const string Resource = "SharedRecords.Addresses";

        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static class Phones
    {
        public const string Resource = "SharedRecords.Phones";

        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Addresses",   ActionConstants.View, Addresses.Resource, IsBasic: true),
        new("Manage Addresses", "Manage",             Addresses.Resource),
        new("View Phones",      ActionConstants.View, Phones.Resource, IsBasic: true),
        new("Manage Phones",    "Manage",             Phones.Resource),
    ];
}
