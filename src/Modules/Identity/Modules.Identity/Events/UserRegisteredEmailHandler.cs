using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Modules.Identity.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Sends a welcome email when a new user registers. Migrado a Wolverine en Fase 3 —
/// la firma <c>Handle(event, deps...)</c> es la convención canónica que el codegen
/// descubre vía discovery (IncludeAssembly del módulo Identity en Program.cs).
/// </summary>
public static class UserRegisteredEmailHandler
{
    public static async Task Handle(
        UserRegisteredIntegrationEvent @event,
        IMailService mailService,
        ILogger<UserRegisteredEmailHandlerLog> logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.Email))
        {
            return;
        }

        try
        {
            var mail = new MailRequest(
                to: new System.Collections.ObjectModel.Collection<string> { @event.Email },
                subject: "Welcome!",
                body: $"Hi {@event.FirstName}, thanks for registering.");

            await mailService.SendAsync(mail, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // email failures must not break the consumer pipeline; rely on Wolverine retry policy
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogWarning(ex, "Failed to send welcome email to {Email}", @event.Email);
        }
    }
}

#pragma warning disable S2094 // marker para el ILogger categoría — la clase static no puede ser usada como TCategory
public sealed class UserRegisteredEmailHandlerLog { }
#pragma warning restore S2094