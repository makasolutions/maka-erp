using FSH.Framework.Persistence;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.Events;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// Subscribes to <see cref="MentionedInChannelIntegrationEvent"/> emitted by the Chat module.
/// Writes a row to the mentioned user's inbox and pushes a <c>NotificationCreated</c> event
/// to their SignalR group so the bell badge updates live.
///
/// Migrado a Wolverine en Fase 3: el <see cref="FSH.Framework.Eventing.Tenant.TenantContextMiddleware"/>
/// global de Wolverine restaura el Finbuckle <c>ITenantInfo</c> desde <c>envelope.TenantId</c>
/// ANTES de invocar este handler, así que el <see cref="NotificationsDbContext"/> resuelve
/// con el tenant correcto sin el bloque manual que el handler previo del bus propio tenía.
/// Discovery vía <c>IncludeAssembly(typeof(NotificationsModule).Assembly)</c> en Program.cs.
/// </summary>
public static class MentionedInChannelIntegrationEventHandler
{
    public static async Task Handle(
        MentionedInChannelIntegrationEvent @event,
        IServiceScopeFactory scopeFactory,
        IHubContext<AppHub> hub,
        ILogger<MentionedInChannelIntegrationEventHandlerLog> logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(hub);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(@event.TenantId))
        {
            logger.LogWarning(
                "Mention event {EventId} arrived without TenantId — cannot write a tenant-scoped notification",
                @event.Id);
            return;
        }

        var notification = Notification.Create(
            userId: @event.MentionedUserId,
            type: "chat.mention",
            title: string.IsNullOrEmpty(@event.ChannelName)
                ? "You were mentioned in a conversation"
                : $"You were mentioned in #{@event.ChannelName}",
            body: @event.BodyPreview,
            link: $"/chat/{@event.ChannelId}?messageId={@event.MessageId}",
            source: @event.Source,
            metadata: new
            {
                channelId = @event.ChannelId,
                channelName = @event.ChannelName,
                messageId = @event.MessageId,
                authorUserId = @event.AuthorUserId,
            });

        // CAPA 3 — escribir con el tenant del evento. NO usar un DbContext inyectado por parámetro:
        // el frame EF-tx de Wolverine lo construye con tenant null (ver TenantScopedDbContext).
        await using (var tenantDb = TenantScopedDbContext.Create<NotificationsDbContext>(scopeFactory, @event.TenantId))
        {
            tenantDb.Context.Notifications.Add(notification);
            await tenantDb.Context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        await hub.Clients.Group($"user:{@event.MentionedUserId}")
            .SendAsync("NotificationCreated", new
            {
                id = notification.Id,
                type = notification.Type,
                title = notification.Title,
                body = notification.Body,
                link = notification.Link,
                source = notification.Source,
                createdAtUtc = notification.CreatedAtUtc,
            }, ct)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Mention notification {NotificationId} for user {UserId} from {AuthorUserId} in channel {ChannelId}",
                notification.Id, @event.MentionedUserId, @event.AuthorUserId, @event.ChannelId);
        }
    }
}

#pragma warning disable S2094 // marker para el ILogger categoría
public sealed class MentionedInChannelIntegrationEventHandlerLog { }
#pragma warning restore S2094
