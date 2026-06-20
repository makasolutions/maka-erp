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

    /// <summary>
    /// Custom fields del módulo Parties (PR-1, SPEC §3/§8.4). <c>Define</c> = cambiar el ESQUEMA
    /// (crear/editar/baja de definiciones) → privilegio admin (no IsBasic). <c>View</c> = leer las
    /// definiciones para renderizar formularios → básico. Llenar VALORES se cubre con el permiso de
    /// la entidad dueña (p. ej. <c>Parties.Update</c>), no con uno de aquí.
    /// TODO(transversal): cuando los custom fields se extiendan a otros módulos (p. ej. Negociación),
    /// promover este recurso a uno compartido fuera de Parties.
    /// </summary>
    public static class CustomFields
    {
        public const string Resource = "Parties.CustomFields";
        public const string Define = $"Permissions.{Resource}.Define";
        public const string View   = $"Permissions.{Resource}.View";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Parties",    ActionConstants.View,   Parties.Resource, IsBasic: true),
        new("Create Parties",  ActionConstants.Create, Parties.Resource),
        new("Update Parties",  ActionConstants.Update, Parties.Resource),
        new("Delete Parties",  ActionConstants.Delete, Parties.Resource),
        new("Restore Parties", "Restore",              Parties.Resource),

        new("View Custom Fields",   ActionConstants.View, CustomFields.Resource, IsBasic: true),
        new("Define Custom Fields", "Define",             CustomFields.Resource),
    ];
}
