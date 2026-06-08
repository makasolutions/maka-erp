using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Verification;

/// <summary>Resultado de un lookup externo (RUES u otro proveedor) de una identificación.</summary>
public sealed record IdentityLookupResult(
    bool    Found,
    string? LegalName,
    string? RegistryStatus,
    string  Source);

/// <summary>
/// Proveedor de verificación de identidad (swappable). La implementación por
/// defecto consulta RUES; cambiar de proveedor solo toca esta interfaz.
/// </summary>
public interface IIdentityVerificationProvider
{
    string Name { get; }
    Task<IdentityLookupResult?> VerifyAsync(string identificationTypeCode, string number, CancellationToken cancellationToken);
}

/// <summary>Valida localmente (DV/formato) y opcionalmente trae razón social del registro.</summary>
public sealed record VerifyIdentificationQuery(
    string IdentificationTypeCode,
    string Number,
    int?   VerificationDigit = null) : IQuery<VerifyIdentificationResult>;

public sealed record VerifyIdentificationResult(
    bool    Valid,
    string? Error,
    int?    VerificationDigit,
    string? LegalName,
    string? RegistryStatus,
    string  Source);
