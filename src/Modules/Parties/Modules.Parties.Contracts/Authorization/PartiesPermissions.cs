using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Parties.Contracts.Authorization;

public static class PartiesPermissions
{
    public static class Parties
    {
        public const string Resource = "Parties.Parties";
        public const string View    = $"Permissions.{Resource}.View";
        public const string Create  = $"Permissions.{Resource}.Create";
        public const string Update  = $"Permissions.{Resource}.Update";
        public const string Delete  = $"Permissions.{Resource}.Delete";
        public const string Restore = $"Permissions.{Resource}.Restore";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Parties",    ActionConstants.View,   Parties.Resource, IsBasic: true),
        new("Create Parties",  ActionConstants.Create, Parties.Resource),
        new("Update Parties",  ActionConstants.Update, Parties.Resource),
        new("Delete Parties",  ActionConstants.Delete, Parties.Resource),
        new("Restore Parties", "Restore",              Parties.Resource),
    ];
}
