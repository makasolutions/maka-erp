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
