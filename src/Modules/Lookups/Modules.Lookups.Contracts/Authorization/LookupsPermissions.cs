using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Lookups.Contracts.Authorization;

public static class LookupsPermissions
{
    /// <summary>Tablas y registros básicos del propio tenant.</summary>
    public static class Tables
    {
        public const string Resource = "Lookups.Tables";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    /// <summary>Administración de tablas/registros GLOBALES — solo root/operador.</summary>
    public static class GlobalTables
    {
        public const string Resource = "Lookups.Global";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Basic Tables",   ActionConstants.View,   Tables.Resource, IsBasic: true),
        new("Create Basic Tables", ActionConstants.Create, Tables.Resource),
        new("Update Basic Tables", ActionConstants.Update, Tables.Resource),
        new("Delete Basic Tables", ActionConstants.Delete, Tables.Resource),

        new("Manage Global Lookups", "Manage", GlobalTables.Resource, IsRoot: true),
    ];
}
