using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.Events;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// Subscribes to <see cref="MentionedInChannelIntegrationEvent"/> emitted by the Chat module via
/// the Outbox. Writes a row to the mentioned user's inbox and pushes a
/// <c>NotificationCreated</c> event to their SignalR group so the bell badge updates live.
///
/// The event arrives from the <c>OutboxDispatcher</c> background pump, which carries NO
/// HTTP/tenant context (eventing.md gotcha). A constructor-injected DbContext would capture a
/// null Finbuckle tenant and the write would mis-stamp. So we open a fresh DI scope, install
/// the event's <c>TenantId</c> into the Finbuckle context, and only then resolve the
/// <see cref="NotificationsDbContext"/> — same mechanics as <c>GlobalCatalogReader</c> /
/// <c>WebhookDispatchJob</c>.
/// </summary>
public sealed class MentionedInChannelIntegrationEventHandler(
    IServiceScopeFactory scopeFactory,
    IHubContext<AppHub> hub,
    ILogger<MentionedInChannelIntegrationEventHandler> logger)
    : IIntegrationEventHandler<MentionedInChannelIntegrationEvent>
{
    public async Task HandleAsync(MentionedInChannelIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

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

        // Fresh scope with the tenant installed BEFORE the DbContext is constructed,
        // so Finbuckle captures the right TenantInfo for filtering + stamping.
        using (var scope = scopeFactory.CreateScope())
        {
            var info = new AppTenantInfo(@event.TenantId, @event.TenantId);
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(info);

            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
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
