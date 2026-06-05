namespace FSH.Modules.Catalog.Contracts.v1.ShippingClasses.GetShippingClasses;

public sealed record ShippingClassDto(
    Guid    Id,
    string  Name,
    string? Description);
