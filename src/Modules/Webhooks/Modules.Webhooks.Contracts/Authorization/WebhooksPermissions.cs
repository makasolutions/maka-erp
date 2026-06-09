using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Webhooks.Contracts.Authorization;

/// <summary>
/// Permisos del módulo Webhooks. La gestión de suscripciones de webhook (crear/editar/probar/
/// eliminar) es una operación privilegiada: NO basta con estar autenticado. El rol Admin los
/// hereda automáticamente vía RolePermissionSyncer (todos los permisos no-root).
/// </summary>
public static class WebhooksPermissions
{
    public static class Subscriptions
    {
        public const string Resource = "Webhooks.Subscriptions";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        // View NO se marca como permiso básico — las suscripciones contienen la URL destino
        // y el hash del secreto; solo roles autorizados (Admin) pueden verlas o gestionarlas.
        new("View Webhook Subscriptions",   ActionConstants.View, Subscriptions.Resource),
        new("Manage Webhook Subscriptions", "Manage",             Subscriptions.Resource),
    ];
}
