namespace FSH.Modules.Catalog.Contracts.Dtos;

public sealed record BrandDto(
    Guid Id,
    string Code,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    bool IsActive,
    bool IsVisible,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
