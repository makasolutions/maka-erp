using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Catalog.Authorization;

/// <summary>
/// File access policy for Catalog owner types (Product, Brand, Category).
/// - Attach: any authenticated user.
/// - Read: Public files (visibility 0) visible to anyone in the tenant — product/brand
///   images are part of the public catalog; Private only to the uploader.
/// - Delete: only the uploader.
/// Registered once per owner type in <c>CatalogModule.ConfigureServices</c>.
/// </summary>
public sealed class CatalogFileAccessPolicy : IFileAccessPolicy
{
    public CatalogFileAccessPolicy(string ownerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        OwnerType = ownerType;
    }

    public string OwnerType { get; }

    public Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(currentUserId));

    public Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Visibility == 0) return Task.FromResult(true); // Public catalog asset.
        if (string.IsNullOrEmpty(currentUserId)) return Task.FromResult(false);
        return Task.FromResult(IsUploader(context, currentUserId));
    }

    public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrEmpty(currentUserId)) return Task.FromResult(false);
        return Task.FromResult(IsUploader(context, currentUserId));
    }

    private static bool IsUploader(FileAccessContext context, string currentUserId)
        => string.Equals(currentUserId, context.CreatedByUserId, StringComparison.Ordinal);
}
