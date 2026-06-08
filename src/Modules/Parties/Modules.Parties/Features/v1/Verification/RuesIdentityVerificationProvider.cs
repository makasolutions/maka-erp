using System.Net.Http.Json;
using System.Text.Json;
using FSH.Modules.Parties.Contracts.v1.Verification;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Parties.Features.v1.Verification;

/// <summary>
/// Consulta razón social por NIT contra RUES (Registro Único Empresarial y Social).
/// Endpoint NO oficial — tolerante a fallos: cualquier error devuelve null y la UX
/// degrada limpio. Cambiar de proveedor solo requiere otra implementación de
/// <see cref="IIdentityVerificationProvider"/>.
/// </summary>
public sealed class RuesIdentityVerificationProvider(
    HttpClient httpClient,
    ILogger<RuesIdentityVerificationProvider> logger) : IIdentityVerificationProvider
{
    public string Name => "Rues";

    public async Task<IdentityLookupResult?> VerifyAsync(
        string identificationTypeCode, string number, CancellationToken cancellationToken)
    {
        string type = (identificationTypeCode ?? string.Empty).Trim().ToUpperInvariant();
        if (type is not ("NIT" or "NIT_EXT")) return null; // RUES es de personas jurídicas

        var digits = new string((number ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        try
        {
            var body = new { Razon = (string?)null, Nit = digits, Dpto = (string?)null, Cod_Camara = (string?)null, Matricula = (string?)null };
            using var response = await httpClient
                .PostAsJsonAsync("/api/ConsultasRUES/BusquedaAvanzadaRM", body, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("[rues] lookup {Nit} returned {Status}", digits, (int)response.StatusCode);
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var (razon, estado) = ExtractFirst(doc.RootElement);
            return razon is null ? null : new IdentityLookupResult(true, razon, estado, Name);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogInformation(ex, "[rues] lookup failed for {Nit}; degrading", digits);
            return null;
        }
    }

    /// <summary>Busca defensivamente "razon_social"/"estado_matricula" en cualquier registro.</summary>
    private static (string? Razon, string? Estado) ExtractFirst(JsonElement root)
    {
        foreach (var el in Enumerate(root))
        {
            if (el.ValueKind != JsonValueKind.Object) continue;
            string? razon = ReadString(el, "razon_social", "Razon_Social", "razonSocial");
            if (!string.IsNullOrWhiteSpace(razon))
            {
                string? estado = ReadString(el, "estado_matricula", "Codigo_Estado_Matricula", "estado");
                return (razon.Trim(), estado?.Trim());
            }
        }
        return (null, null);
    }

    private static IEnumerable<JsonElement> Enumerate(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                    foreach (var nested in Enumerate(item))
                        yield return nested;
                break;
            case JsonValueKind.Object:
                yield return el;
                foreach (var prop in el.EnumerateObject())
                    if (prop.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                        foreach (var nested in Enumerate(prop.Value))
                            yield return nested;
                break;
            default:
                break;
        }
    }

    private static string? ReadString(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
            if (obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        return null;
    }
}

/// <summary>Proveedor nulo: deshabilita el lookup externo (config Provider="None").</summary>
public sealed class NullIdentityVerificationProvider : IIdentityVerificationProvider
{
    public string Name => "None";
    public Task<IdentityLookupResult?> VerifyAsync(string identificationTypeCode, string number, CancellationToken cancellationToken)
        => Task.FromResult<IdentityLookupResult?>(null);
}
