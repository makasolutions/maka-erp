using FSH.Framework.Storage.Local;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace FSH.Modules.Files.Features.v1.Internal;

/// <summary>
/// Development-only local-upload proxy.
///
/// When Storage:Provider is "local", LocalStorageService issues presigned upload URLs
/// with the <c>local://upload/{token}</c> scheme — a custom scheme the browser cannot
/// PUT to directly. This endpoint bridges the gap by accepting the raw bytes from the
/// React dashboard and writing them to the wwwroot/uploads tree, exactly as if the
/// browser had talked to S3/MinIO.
///
/// Registration is conditional on the "local" storage provider so this route is never
/// present in staging or production (where S3 handles uploads directly).
///
/// Auth: the one-shot token IS the credential — no JWT check on this route.
/// </summary>
public static class UploadEndpoint
{
    /// <summary>
    /// Registers PUT /local-upload/{token} only when Storage:Provider == "local".
    /// Call from the host's app-building phase AFTER MapEndpoints(). Returns the route
    /// builder when registered, or <c>null</c> when the local provider is not active.
    /// </summary>
    public static RouteHandlerBuilder? MapUploadEndpoint(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Storage:Provider"]?.Trim().ToUpperInvariant();
        if (provider != "LOCAL")
        {
            return null;
        }

        // Unversioned, unauthenticated route — the token itself is the credential.
        return app.MapPut("/local-upload/{token}", HandleAsync)
           .WithTags("Files (local dev)")
           .WithSummary("[Dev only] Receive a presigned local upload")
           .WithDescription(
               "Accepts raw file bytes for a previously minted local-storage presigned token. " +
               "Only active when Storage:Provider = 'local'. Not present in staging or production.")
           .AllowAnonymous()
           .ExcludeFromDescription(); // hide from Scalar in prod builds
    }

    private static async Task<IResult> HandleAsync(
        string token,
        HttpRequest request,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        // Consume the token — one-shot, validates expiry.
        var entry = LocalStorageService.SharedTokenStore.Consume(token);
        if (entry is null)
        {
            return Results.Problem(
                title: "Invalid or expired upload token",
                detail: "The local upload token was not found or has expired.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate Content-Type matches what was promised at presign time.
        if (!string.IsNullOrWhiteSpace(entry.ContentType) &&
            request.ContentType is string ct &&
            !ct.StartsWith(entry.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                title: "Content-Type mismatch",
                detail: $"Expected '{entry.ContentType}', got '{ct}'.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        // Validate size if Content-Length header is present.
        if (request.ContentLength.HasValue && request.ContentLength.Value > entry.MaxBytes)
        {
            return Results.Problem(
                title: "File too large",
                detail: $"Max {entry.MaxBytes} bytes allowed.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        // Build the full path from the storage key.
        var rootPath = string.IsNullOrWhiteSpace(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;

        // StorageKey already contains the relative path (e.g. "uploads/tenant/type/file.jpg").
        var normalizedKey = entry.StorageKey.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(rootPath, normalizedKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous);
        await request.Body.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

        return Results.NoContent(); // 204 — matches S3 PUT behaviour
    }
}
