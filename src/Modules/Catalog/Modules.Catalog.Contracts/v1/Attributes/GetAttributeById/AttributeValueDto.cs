namespace FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributeById;

public sealed record AttributeValueDto(
    Guid    Id,
    Guid    AttributeId,
    string  Value,
    string? ColorCode,
    string? ImageUrl,
    int     SortOrder,
    int?    WooCommerceId);
