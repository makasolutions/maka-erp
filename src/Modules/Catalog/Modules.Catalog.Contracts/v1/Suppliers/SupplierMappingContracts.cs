using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Suppliers;

public sealed record GlobalBrandDto(Guid Id, string Name, string? CountryOfOrigin);

/// <summary>Marcas canónicas del tenant `global` (para el mapeo de proveedores).</summary>
public sealed record GetGlobalBrandsQuery(string? Search = null) : IQuery<IReadOnlyList<GlobalBrandDto>>;

/// <summary>Marcas que un proveedor comercializa (ids de marca global).</summary>
public sealed record GetSupplierBrandsQuery(Guid SupplierId) : IQuery<IReadOnlyList<Guid>>;
public sealed record SetSupplierBrandsCommand(Guid SupplierId, IReadOnlyList<Guid> BrandIds) : ICommand;

/// <summary>Categorías (taxonomía global) que un proveedor comercializa.</summary>
public sealed record GetSupplierCategoriesQuery(Guid SupplierId) : IQuery<IReadOnlyList<Guid>>;
public sealed record SetSupplierCategoriesCommand(Guid SupplierId, IReadOnlyList<Guid> CategoryIds) : ICommand;
