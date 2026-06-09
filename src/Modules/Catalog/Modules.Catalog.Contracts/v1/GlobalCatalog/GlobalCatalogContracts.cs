using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.GlobalCatalog;

public sealed record IndustryDto(Guid Id, string Code, string Name, int SortOrder);

public sealed record GlobalCategoryDto(
    Guid    Id,
    int?    GoogleCategoryId,
    Guid?   ParentId,
    string  Name,
    string? FullPath,
    int?    RootGoogleCategoryId);

/// <summary>Lista las industrias/sectores del tenant `global`.</summary>
public sealed record GetIndustriesQuery : IQuery<IReadOnlyList<IndustryDto>>;

/// <summary>Industrias seleccionadas por el tenant actual.</summary>
public sealed record GetTenantIndustriesQuery : IQuery<IReadOnlyList<Guid>>;

/// <summary>Reemplaza las industrias del tenant actual (onboarding/configuración).</summary>
public sealed record SetTenantIndustriesCommand(IReadOnlyList<Guid> IndustryIds) : ICommand;

/// <summary>
/// Categorías globales (taxonomía Google) visibles al tenant actual, filtradas por
/// sus industrias seleccionadas. Sin industrias → toda la taxonomía (fallback).
/// </summary>
public sealed record GetGlobalCategoriesQuery(string? Search = null) : IQuery<IReadOnlyList<GlobalCategoryDto>>;

/// <summary>
/// Adopta categorías de la taxonomía global (tenant `global`) al árbol de categorías
/// propio del tenant actual, recreando la cadena de ancestros y deduplicando por
/// <c>GoogleCategoryId</c>. Devuelve cuántas categorías nuevas se crearon.
/// </summary>
public sealed record ImportGlobalCategoriesCommand(IReadOnlyList<Guid> CategoryIds) : ICommand<int>;

// ── Búsqueda inteligente (unaccent + pg_trgm + alias) ────────────────────────
public sealed record GlobalCategorySuggestionDto(
    Guid Id, int? GoogleCategoryId, string Name, string? FullPath, double Score, bool AlreadyAdopted);

public sealed record GlobalBrandSuggestionDto(
    Guid Id, string Name, string? Country, string? LogoUrl, double Score, bool AlreadyAdopted);

public sealed record SearchGlobalCategoriesQuery(string Q) : IQuery<IReadOnlyList<GlobalCategorySuggestionDto>>;

public sealed record SearchGlobalBrandsQuery(string Q) : IQuery<IReadOnlyList<GlobalBrandSuggestionDto>>;

/// <summary>
/// Adopta una categoría global al árbol del tenant: asegura la cadena de ancestros y crea la hoja
/// con nombre/slug editables. Devuelve el id de la categoría del tenant.
/// </summary>
public sealed record AdoptGlobalCategoryCommand(Guid GlobalCategoryId, string? Name, string? Slug) : ICommand<Guid>;

// ── Alias / sinónimos (editable) ─────────────────────────────────────────────
public sealed record CatalogAliasDto(Guid Id, CatalogAliasEntity EntityType, Guid TargetId, string? TargetName, string Alias);

public sealed record GetCatalogAliasesQuery(CatalogAliasEntity? EntityType = null) : IQuery<IReadOnlyList<CatalogAliasDto>>;

public sealed record AddCatalogAliasCommand(CatalogAliasEntity EntityType, Guid TargetId, string Alias) : ICommand<Guid>;

public sealed record DeleteCatalogAliasCommand(Guid Id) : ICommand;

// ── Productos globales (publicar / buscar / adoptar) ─────────────────────────
public sealed record GlobalProductSuggestionDto(
    Guid Id, string Name, string? DefaultSku, string? ShortDescription, string? ImageUrl,
    double Score, bool AlreadyAdopted);

/// <summary>Publica un producto del tenant al catálogo global (copia canónica). Devuelve el id global.</summary>
public sealed record PublishProductToGlobalCommand(Guid ProductId) : ICommand<Guid>;

public sealed record SearchGlobalProductsQuery(string Q) : IQuery<IReadOnlyList<GlobalProductSuggestionDto>>;

/// <summary>Adopta un producto global al catálogo del tenant (copia editable). Devuelve el id del producto del tenant.</summary>
public sealed record AdoptGlobalProductCommand(Guid GlobalProductId) : ICommand<Guid>;
