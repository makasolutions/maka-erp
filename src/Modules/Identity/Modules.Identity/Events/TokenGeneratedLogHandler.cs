using FSH.Modules.Identity.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Sample handler que loguea cuando un token se genera. Migrado a Wolverine en Fase 3
/// (mismo patrón estructural que <see cref="UserRegisteredEmailHandler"/>).
/// Discovery vía IncludeAssembly de Identity en Program.cs.
/// </summary>
public static class TokenGeneratedLogHandler
{
    public static void Handle(TokenGeneratedIntegrationEvent @event, ILogger<TokenGeneratedLogHandlerLog> logger)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Token generated for user {UserId} ({Email}) with client {ClientId}, IP {IpAddress}, UserAgent {UserAgent}, expires at {ExpiresAtUtc} (fingerprint: {Fingerprint})",
                @event.UserId,
                @event.Email,
                @event.ClientId,
                @event.IpAddress,
                @event.UserAgent,
                @event.AccessTokenExpiresAtUtc,
                @event.TokenFingerprint);
        }
    }
}

#pragma warning disable S2094 // marker para el ILogger categoría
public sealed class TokenGeneratedLogHandlerLog { }
#pragma warning restore S2094