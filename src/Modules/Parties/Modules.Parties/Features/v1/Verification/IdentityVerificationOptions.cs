namespace FSH.Modules.Parties.Features.v1.Verification;

/// <summary>Config de verificación de identidad. Sección "IdentityVerification".</summary>
public sealed class IdentityVerificationOptions
{
    /// <summary>"Rues" (default) o "None" para desactivar el lookup externo.</summary>
    public string Provider { get; set; } = "Rues";

    public string RuesBaseUrl { get; set; } = "https://ruesapi.rues.org.co";

    public int TimeoutSeconds { get; set; } = 8;
}
