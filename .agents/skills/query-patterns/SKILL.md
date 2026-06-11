---
name: query-patterns
description: Query patterns including pagination, search, filtering for FSH/Maka. Use when implementing GET endpoints that return lists or need filtering. DbContext direct — there is no repository.
---

# Query Patterns

Patrones reales del repo para queries de lectura. **No existe `IRepository<T>`/`IReadRepository<T>`**
(prohibido, CLAUDE.md §9): se inyecta el `{X}DbContext` del módulo. Paginación con
`IPagedQuery` → `PagedResponse<T>` (`FSH.Framework.Shared.Persistence`).

Patrón vivo de referencia: `src/Modules/Catalog/Modules.Catalog/Features/v1/Products/SearchProducts/`.

## Paginated search

```csharp
// Query (Contracts project)
public sealed record Search{Entities}Query(
    string? Search,
    int PageNumber = 1,
    int PageSize = 10,
    string? Sort = null) : IQuery<PagedResponse<{Entity}Dto>>, IPagedQuery;

// Handler (runtime Features/) — DbContext directo, AsNoTracking, proyección a DTO en BD
public sealed class Search{Entities}QueryHandler({X}DbContext db)
    : IQueryHandler<Search{Entities}Query, PagedResponse<{Entity}Dto>>
{
    public async ValueTask<PagedResponse<{Entity}Dto>> Handle(
        Search{Entities}Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filtered = db.{Entities}.AsNoTracking()
            .Where(x => string.IsNullOrEmpty(query.Search) || x.Name.Contains(query.Search));

        int totalCount = await filtered.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await filtered
            .OrderBy(x => x.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new {Entity}Dto(x.Id, x.Name /* … proyectar en BD, no en memoria */))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResponse<{Entity}Dto>(items, totalCount, query.PageNumber, query.PageSize);
    }
}
```

⚠️ **Toda paginated query necesita su `{Name}Validator`** — `Architecture.Tests`
(`HandlerValidatorPairingTests`) rompe el build si falta. Valida `PageNumber >= 1`,
`PageSize` acotado, longitud máxima de `Search`.

## Get single

```csharp
public sealed record Get{Entity}ByIdQuery(Guid Id) : IQuery<{Entity}Dto>;

public sealed class Get{Entity}ByIdQueryHandler({X}DbContext db)
    : IQueryHandler<Get{Entity}ByIdQuery, {Entity}Dto>
{
    public async ValueTask<{Entity}Dto> Handle(Get{Entity}ByIdQuery query, CancellationToken cancellationToken)
    {
        var dto = await db.{Entities}.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new {Entity}Dto(x.Id, x.Name /* … */))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return dto ?? throw new NotFoundException($"{Entity} {query.Id} not found");
    }
}
```

## Filtros múltiples

Componer `Where` condicionales sobre el `IQueryable` (cada filtro opcional con
`!x.HasValue || …`), o usar `Specification<T>` del framework
(`src/BuildingBlocks/Persistence/Specifications/`) cuando la composición se reutiliza
entre features. Las Specifications ya aplican `AsNoTracking` por defecto.

```csharp
var q = db.{Entities}.AsNoTracking()
    .Where(x => string.IsNullOrEmpty(query.Search) || x.Name.Contains(query.Search))
    .Where(x => !query.CategoryId.HasValue || x.CategoryId == query.CategoryId)
    .Where(x => !query.IsActive.HasValue || x.IsActive == query.IsActive);

q = query.Sort?.ToLowerInvariant() switch
{
    "name"  => q.OrderBy(x => x.Name),
    "-name" => q.OrderByDescending(x => x.Name),
    _       => q.OrderByDescending(x => x.CreatedOnUtc),
};
```

## Endpoint

```csharp
public static class Search{Entities}Endpoint
{
    internal static RouteHandlerBuilder MapSearch{Entities}Endpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{entities}/search",
                ([AsParameters] Search{Entities}Query query, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(query, ct))
            .WithName("Search{Entities}")
            .WithSummary("Search {entities} (paged)")
            .RequirePermission({X}Permissions.{Entities}.View);
}
```

## Reglas

- Lecturas: `AsNoTracking()` siempre; **nunca** en flujos read-then-mutate-then-save (`database.md`).
- Proyectar a DTO **en la BD** (`Select` antes de `ToListAsync`), no materializar entidades completas.
- Server-side paging siempre — nunca traer todos los registros (CLAUDE.md §8).
- GET handlers son read-only — nunca escribir en BD desde un query handler (AGENTS.md golden rule #15).
- Propagar `CancellationToken` + `.ConfigureAwait(false)` en cada await.
