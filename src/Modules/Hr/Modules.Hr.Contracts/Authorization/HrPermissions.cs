using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Hr.Contracts.Authorization;

public static class HrPermissions
{
    public static class Employees
    {
        public const string Resource = "Hr.Employees";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Employees",   ActionConstants.View,   Employees.Resource, IsBasic: true),
        new("Create Employees", ActionConstants.Create, Employees.Resource),
        new("Update Employees", ActionConstants.Update, Employees.Resource),
        new("Delete Employees", ActionConstants.Delete, Employees.Resource),
    ];
}
