using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.GlobalCatalog;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Extensions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.GlobalCatalog;

// ── Industrias del tenant global ─────────────────────────────────────────────
public sealed class GetIndustriesQueryHandler(IGlobalCatalogReader global)
    : IQueryHandler<GetIndustriesQuery, IReadOnlyList<IndustryDto>>
{
    public async ValueTask<IReadOnlyList<IndustryDto>> Handle(GetIndustriesQuery query, CancellationToken cancellationToken) =>
        await global.RunAsync(async (db, ct) => (IReadOnlyList<IndustryDto>)await db.Industries
            .AsNoTracking().Where(i => i.IsActive)
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Name)
            .Select(i => new IndustryDto(i.Id, i.Code, i.Name, i.SortOrder))
            .ToListAsync(ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}

// ── Industrias seleccionadas por el tenant actual ────────────────────────────
public sealed class GetTenantIndustriesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetTenantIndustriesQuery, IReadOnlyList<Guid>>
{
    public async ValueTask<IReadOnlyList<Guid>> Handle(GetTenantIndustriesQuery query, CancellationToken cancellationToken) =>
        await db.TenantIndustries.AsNoTracking()
            .Select(t => t.IndustryId).ToListAsync(cancellationToken).ConfigureAwait(false);
}

public sealed class SetTenantIndustriesCommandValidator : AbstractValidator<SetTenantIndustriesCommand>
{
    public SetTenantIndustriesCommandValidator() =>
        RuleFor(x => x.IndustryIds).NotNull();
}

public sealed class SetTenantIndustriesCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetTenantIndustriesCommand>
{
    public async ValueTask<Unit> Handle(SetTenantIndustriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await db.TenantIndustries.ToListAsync(cancellationToken).ConfigureAwait(false);
        db.TenantIndustries.RemoveRange(existing);
        foreach (var id in command.IndustryIds.Distinct())
            db.TenantIndustries.Add(TenantIndustry.Create(id));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ── Categorías globales filtradas por la industria del tenant ────────────────
public sealed class GetGlobalCategoriesQueryHandler(CatalogDbContext db, IGlobalCatalogReader global)
    : IQueryHandler<GetGlobalCategoriesQuery, IReadOnlyList<GlobalCategoryDto>>
{
    private const int MaxResults = 3000;

    public async ValueTask<IReadOnlyList<GlobalCategoryDto>> Handle(GetGlobalCategoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var industryIds = await db.TenantIndustries.AsNoTracking()
            .Select(t => t.IndustryId).ToListAsync(cancellationToken).ConfigureAwait(false);

        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        return await global.RunAsync(async (gdb, ct) =>
        {
            List<int>? roots = null;
            if (industryIds.Count > 0)
            {
                roots = await gdb.IndustryCategories.AsNoTracking()
                    .Where(ic => industryIds.Contains(ic.IndustryId))
                    .Select(ic => ic.RootGoogleCategoryId)
                    .Distinct().ToListAsync(ct).ConfigureAwait(false);
            }

            var q = gdb.Categories.AsNoTracking().Where(c => !c.IsDeleted && c.GoogleCategoryId != null);
            if (roots is { Count: > 0 })
                q = q.Where(c => c.RootGoogleCategoryId != null && roots.Contains(c.RootGoogleCategoryId.Value));
            if (search is not null)
                q = q.Where(c => EF.Functions.ILike(c.Name, $"%{search}%") ||
                                 (c.FullPath != null && EF.Functions.ILike(c.FullPath, $"%{search}%")));

            return (IReadOnlyList<GlobalCategoryDto>)await q
                .OrderBy(c => c.FullPath)
                .Take(MaxResults)
                .Select(c => new GlobalCategoryDto(c.Id, c.GoogleCategoryId, c.ParentId, c.Name, c.FullPath, c.RootGoogleCategoryId))
                .ToListAsync(ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
}

// ── Importar categorías del catálogo global al árbol del tenant ──────────────
public sealed class ImportGlobalCategoriesCommandValidator : AbstractValidator<ImportGlobalCategoriesCommand>
{
    public ImportGlobalCategoriesCommandValidator() =>
        RuleFor(x => x.CategoryIds).NotEmpty().WithMessage("Selecciona al menos una categoría para importar.");
}

public sealed class ImportGlobalCategoriesCommandHandler(CatalogDbContext db, IGlobalCatalogReader global)
    : ICommandHandler<ImportGlobalCategoriesCommand, int>
{
    private sealed record GlobalNode(Guid Id, Guid? ParentId, int GoogleCategoryId, int? RootGoogleCategoryId, string Name, string? FullPath);

    public async ValueTask<int> Handle(ImportGlobalCategoriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var wanted = command.CategoryIds.Distinct().ToHashSet();
        if (wanted.Count == 0) return 0;

        // 1) Read the selected nodes + their ancestor chain from the global tenant.
        var nodes = await global.RunAsync(async (gdb, ct) =>
        {
            var map = new Dictionary<Guid, GlobalNode>();
            var frontier = wanted.ToList();
            while (frontier.Count > 0)
            {
                var ids = frontier;
                var batch = await gdb.Categories.AsNoTracking()
                    .Where(x => ids.Contains(x.Id) && x.GoogleCategoryId != null)
                    .Select(x => new GlobalNode(x.Id, x.ParentId, x.GoogleCategoryId!.Value, x.RootGoogleCategoryId, x.Name, x.FullPath))
                    .ToListAsync(ct).ConfigureAwait(false);
                var next = new List<Guid>();
                foreach (var n in batch)
                {
                    if (!map.TryAdd(n.Id, n)) continue;
                    if (n.ParentId is { } pid && !map.ContainsKey(pid)) next.Add(pid);
                }
                frontier = next;
            }
            return map.Values.ToList();
        }, cancellationToken).ConfigureAwait(false);

        // 2) Parents before children (depth via path segments).
        var ordered = nodes.OrderBy(n => (n.FullPath ?? string.Empty).Count(c => c == '>')).ToList();

        // 3) Dedupe against the tenant's existing categories by GoogleCategoryId.
        var tenantByGoogle = (await db.Categories
                .Where(c => c.GoogleCategoryId != null)
                .Select(c => new { c.Id, Google = c.GoogleCategoryId!.Value })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(e => e.Google, e => e.Id);

        var globalToTenant = new Dictionary<Guid, Guid>();
        int created = 0;
        foreach (var n in ordered)
        {
            if (tenantByGoogle.TryGetValue(n.GoogleCategoryId, out var existingId))
            {
                globalToTenant[n.Id] = existingId;
                continue;
            }
            Guid? parentTenantId = n.ParentId is { } pid && globalToTenant.TryGetValue(pid, out var pt) ? pt : null;
            int root = n.RootGoogleCategoryId ?? n.GoogleCategoryId;
            string slug = $"{SlugHelper.Build(null, n.Name)}-{n.GoogleCategoryId}";
            var entity = Category.FromGoogleTaxonomy(n.GoogleCategoryId, root, n.Name, slug, n.FullPath ?? n.Name, parentTenantId, 0);
            db.Categories.Add(entity);
            globalToTenant[n.Id] = entity.Id;
            tenantByGoogle[n.GoogleCategoryId] = entity.Id;
            created++;
        }

        if (created > 0) await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}

// ── Búsqueda inteligente: categorías globales (unaccent + pg_trgm + alias) ───
public sealed class SearchGlobalCategoriesQueryValidator : AbstractValidator<SearchGlobalCategoriesQuery>
{
    public SearchGlobalCategoriesQueryValidator() => RuleFor(x => x.Q).NotEmpty().MinimumLength(2);
}

public sealed class SearchGlobalCategoriesQueryHandler(CatalogDbContext db, IGlobalCatalogReader global)
    : IQueryHandler<SearchGlobalCategoriesQuery, IReadOnlyList<GlobalCategorySuggestionDto>>
{
    private sealed record CatHit(Guid Id, int? GoogleCategoryId, string Name, string? FullPath, double Score);

    public async ValueTask<IReadOnlyList<GlobalCategorySuggestionDto>> Handle(SearchGlobalCategoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        string q = query.Q.Trim();
        if (q.Length < 2) return [];

        var industryIds = await db.TenantIndustries.AsNoTracking()
            .Select(t => t.IndustryId).ToListAsync(cancellationToken).ConfigureAwait(false);

        var hits = await global.RunAsync(async (gdb, ct) =>
        {
            int[] roots = industryIds.Count == 0
                ? []
                : (await gdb.IndustryCategories.AsNoTracking()
                    .Where(ic => industryIds.Contains(ic.IndustryId))
                    .Select(ic => ic.RootGoogleCategoryId).Distinct()
                    .ToListAsync(ct).ConfigureAwait(false)).ToArray();

            return await gdb.Database.SqlQuery<CatHit>($"""
                SELECT c."Id" AS "Id", c."GoogleCategoryId" AS "GoogleCategoryId", c."Name" AS "Name",
                       c."FullPath" AS "FullPath",
                       GREATEST(
                         similarity(unaccent(lower(c."Name")), unaccent(lower({q}))),
                         COALESCE((SELECT MAX(similarity(unaccent(lower(a."Alias")), unaccent(lower({q}))))
                                   FROM catalog."CatalogAliases" a
                                   WHERE a."TenantId" = 'global' AND a."EntityType" = 'Category' AND a."TargetId" = c."Id"), 0)
                       )::double precision AS "Score"
                FROM catalog."Categories" c
                WHERE c."TenantId" = 'global' AND NOT c."IsDeleted" AND c."GoogleCategoryId" IS NOT NULL
                  AND (cardinality({roots}) = 0 OR c."RootGoogleCategoryId" = ANY({roots}))
                  AND (
                    unaccent(lower(c."Name")) % unaccent(lower({q}))
                    OR EXISTS (SELECT 1 FROM catalog."CatalogAliases" a
                               WHERE a."TenantId" = 'global' AND a."EntityType" = 'Category' AND a."TargetId" = c."Id"
                                 AND unaccent(lower(a."Alias")) % unaccent(lower({q})))
                  )
                ORDER BY "Score" DESC
                LIMIT 20
                """).ToListAsync(ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        var googleIds = hits.Where(h => h.GoogleCategoryId.HasValue).Select(h => h.GoogleCategoryId!.Value).ToList();
        var adopted = (await db.Categories.AsNoTracking()
                .Where(c => c.GoogleCategoryId != null && googleIds.Contains(c.GoogleCategoryId.Value))
                .Select(c => c.GoogleCategoryId!.Value).ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToHashSet();

        return hits.Select(h => new GlobalCategorySuggestionDto(
            h.Id, h.GoogleCategoryId, h.Name, h.FullPath, h.Score,
            h.GoogleCategoryId.HasValue && adopted.Contains(h.GoogleCategoryId.Value))).ToList();
    }
}

// ── Búsqueda inteligente: marcas globales ────────────────────────────────────
public sealed class SearchGlobalBrandsQueryValidator : AbstractValidator<SearchGlobalBrandsQuery>
{
    public SearchGlobalBrandsQueryValidator() => RuleFor(x => x.Q).NotEmpty().MinimumLength(2);
}

public sealed class SearchGlobalBrandsQueryHandler(CatalogDbContext db, IGlobalCatalogReader global)
    : IQueryHandler<SearchGlobalBrandsQuery, IReadOnlyList<GlobalBrandSuggestionDto>>
{
    private sealed record BrandHit(Guid Id, string Name, string? Country, string? LogoUrl, string Slug, double Score);

    public async ValueTask<IReadOnlyList<GlobalBrandSuggestionDto>> Handle(SearchGlobalBrandsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        string q = query.Q.Trim();
        if (q.Length < 2) return [];

        var hits = await global.RunAsync(async (gdb, ct) =>
            await gdb.Database.SqlQuery<BrandHit>($"""
                SELECT b."Id" AS "Id", b."Name" AS "Name", b."CountryOfOrigin" AS "Country",
                       b."LogoUrl" AS "LogoUrl", b."Slug" AS "Slug",
                       GREATEST(
                         similarity(unaccent(lower(b."Name")), unaccent(lower({q}))),
                         COALESCE((SELECT MAX(similarity(unaccent(lower(a."Alias")), unaccent(lower({q}))))
                                   FROM catalog."CatalogAliases" a
                                   WHERE a."TenantId" = 'global' AND a."EntityType" = 'Brand' AND a."TargetId" = b."Id"), 0)
                       )::double precision AS "Score"
                FROM catalog."Brands" b
                WHERE b."TenantId" = 'global' AND NOT b."IsDeleted"
                  AND (
                    unaccent(lower(b."Name")) % unaccent(lower({q}))
                    OR EXISTS (SELECT 1 FROM catalog."CatalogAliases" a
                               WHERE a."TenantId" = 'global' AND a."EntityType" = 'Brand' AND a."TargetId" = b."Id"
                                 AND unaccent(lower(a."Alias")) % unaccent(lower({q})))
                  )
                ORDER BY "Score" DESC
                LIMIT 20
                """).ToListAsync(ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        var slugs = hits.Select(h => h.Slug).ToList();
        var adopted = (await db.Brands.AsNoTracking()
                .Where(b => slugs.Contains(b.Slug))
                .Select(b => b.Slug).ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToHashSet();

        return hits.Select(h => new GlobalBrandSuggestionDto(
            h.Id, h.Name, h.Country, h.LogoUrl, h.Score, adopted.Contains(h.Slug))).ToList();
    }
}

// ── Adopción de categoría con edición ────────────────────────────────────────
public sealed class AdoptGlobalCategoryCommandValidator : AbstractValidator<AdoptGlobalCategoryCommand>
{
    public AdoptGlobalCategoryCommandValidator() => RuleFor(x => x.GlobalCategoryId).NotEmpty();
}

public sealed class AdoptGlobalCategoryCommandHandler(CatalogDbContext db, IGlobalCatalogReader global)
    : ICommandHandler<AdoptGlobalCategoryCommand, Guid>
{
    private sealed record GNode(Guid Id, Guid? ParentId, int GoogleCategoryId, int? RootGoogleCategoryId, string Name, string? FullPath);

    public async ValueTask<Guid> Handle(AdoptGlobalCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Read the target node + ancestor chain from the global tenant.
        var nodes = await global.RunAsync(async (gdb, ct) =>
        {
            var map = new Dictionary<Guid, GNode>();
            var frontier = new List<Guid> { command.GlobalCategoryId };
            while (frontier.Count > 0)
            {
                var ids = frontier;
                var batch = await gdb.Categories.AsNoTracking()
                    .Where(x => ids.Contains(x.Id) && x.GoogleCategoryId != null)
                    .Select(x => new GNode(x.Id, x.ParentId, x.GoogleCategoryId!.Value, x.RootGoogleCategoryId, x.Name, x.FullPath))
                    .ToListAsync(ct).ConfigureAwait(false);
                var next = new List<Guid>();
                foreach (var n in batch)
                {
                    if (!map.TryAdd(n.Id, n)) continue;
                    if (n.ParentId is { } pid && !map.ContainsKey(pid)) next.Add(pid);
                }
                frontier = next;
            }
            return map;
        }, cancellationToken).ConfigureAwait(false);

        if (!nodes.ContainsKey(command.GlobalCategoryId))
            throw new CustomException("Categoría global no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var ordered = nodes.Values.OrderBy(n => (n.FullPath ?? string.Empty).Count(c => c == '>')).ToList();
        var tenantByGoogle = (await db.Categories
                .Where(c => c.GoogleCategoryId != null)
                .Select(c => new { c.Id, Google = c.GoogleCategoryId!.Value })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(e => e.Google, e => e.Id);

        var globalToTenant = new Dictionary<Guid, Guid>();
        Guid leafId = Guid.Empty;
        foreach (var n in ordered)
        {
            bool isLeaf = n.Id == command.GlobalCategoryId;
            if (tenantByGoogle.TryGetValue(n.GoogleCategoryId, out var existingId))
            {
                globalToTenant[n.Id] = existingId;
                if (isLeaf) leafId = existingId;
                continue;
            }
            Guid? parentTenantId = n.ParentId is { } pid && globalToTenant.TryGetValue(pid, out var pt) ? pt : null;
            int root = n.RootGoogleCategoryId ?? n.GoogleCategoryId;
            string name = isLeaf && !string.IsNullOrWhiteSpace(command.Name) ? command.Name!.Trim() : n.Name;
            string slug = isLeaf && !string.IsNullOrWhiteSpace(command.Slug)
                ? SlugHelper.Build(command.Slug, name)
                : $"{SlugHelper.Build(null, n.Name)}-{n.GoogleCategoryId}";
            var entity = Category.FromGoogleTaxonomy(n.GoogleCategoryId, root, name, slug, n.FullPath ?? name, parentTenantId, 0);
            db.Categories.Add(entity);
            globalToTenant[n.Id] = entity.Id;
            tenantByGoogle[n.GoogleCategoryId] = entity.Id;
            if (isLeaf) leafId = entity.Id;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return leafId;
    }
}

// ── Alias / sinónimos (editable, en el tenant global) ────────────────────────
public sealed class GetCatalogAliasesQueryHandler(IGlobalCatalogReader global)
    : IQueryHandler<GetCatalogAliasesQuery, IReadOnlyList<CatalogAliasDto>>
{
    public async ValueTask<IReadOnlyList<CatalogAliasDto>> Handle(GetCatalogAliasesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await global.RunAsync(async (gdb, ct) =>
        {
            var q = gdb.CatalogAliases.AsNoTracking();
            if (query.EntityType is { } et) q = q.Where(a => a.EntityType == et);
            var aliases = await q.OrderBy(a => a.Alias).ToListAsync(ct).ConfigureAwait(false);
            var brandIds = aliases.Where(a => a.EntityType == CatalogAliasEntity.Brand).Select(a => a.TargetId).ToList();
            var catIds = aliases.Where(a => a.EntityType == CatalogAliasEntity.Category).Select(a => a.TargetId).ToList();
            var brandNames = await gdb.Brands.AsNoTracking().Where(b => brandIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name, ct).ConfigureAwait(false);
            var catNames = await gdb.Categories.AsNoTracking().Where(c => catIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct).ConfigureAwait(false);
            return (IReadOnlyList<CatalogAliasDto>)aliases.Select(a => new CatalogAliasDto(
                a.Id, a.EntityType, a.TargetId,
                a.EntityType == CatalogAliasEntity.Brand ? brandNames.GetValueOrDefault(a.TargetId) : catNames.GetValueOrDefault(a.TargetId),
                a.Alias)).ToList();
        }, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class AddCatalogAliasCommandValidator : AbstractValidator<AddCatalogAliasCommand>
{
    public AddCatalogAliasCommandValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Alias).NotEmpty().MinimumLength(2).MaximumLength(128);
    }
}

public sealed class AddCatalogAliasCommandHandler(IGlobalCatalogReader global)
    : ICommandHandler<AddCatalogAliasCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddCatalogAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await global.RunAsync(async (gdb, ct) =>
        {
            string alias = command.Alias.Trim();
            bool dup = await gdb.CatalogAliases.AsNoTracking()
                .AnyAsync(a => a.EntityType == command.EntityType && a.TargetId == command.TargetId && EF.Functions.ILike(a.Alias, alias), ct)
                .ConfigureAwait(false);
            if (dup) throw new CustomException("Ese alias ya existe para este objeto.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);
            var entity = CatalogAlias.Create(command.EntityType, command.TargetId, alias);
            gdb.CatalogAliases.Add(entity);
            await gdb.SaveChangesAsync(ct).ConfigureAwait(false);
            return entity.Id;
        }, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DeleteCatalogAliasCommandValidator : AbstractValidator<DeleteCatalogAliasCommand>
{
    public DeleteCatalogAliasCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteCatalogAliasCommandHandler(IGlobalCatalogReader global)
    : ICommandHandler<DeleteCatalogAliasCommand>
{
    public async ValueTask<Unit> Handle(DeleteCatalogAliasCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await global.RunAsync(async (gdb, ct) =>
        {
            var entity = await gdb.CatalogAliases.FirstOrDefaultAsync(a => a.Id == command.Id, ct).ConfigureAwait(false)
                ?? throw new CustomException("Alias no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
            gdb.CatalogAliases.Remove(entity);
            await gdb.SaveChangesAsync(ct).ConfigureAwait(false);
            return 0;
        }, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ── Endpoints ────────────────────────────────────────────────────────────────
public static class GlobalCatalogEndpoints
{
    public static void MapGlobalCatalogEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/industries",
                async (IMediator m, CancellationToken ct) => Results.Ok(await m.Send(new GetIndustriesQuery(), ct)))
            .WithName("GetIndustries").WithSummary("List global industries")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<IndustryDto>>(StatusCodes.Status200OK);

        group.MapGet("/tenant-industries",
                async (IMediator m, CancellationToken ct) => Results.Ok(await m.Send(new GetTenantIndustriesQuery(), ct)))
            .WithName("GetTenantIndustries").WithSummary("Industries selected by current tenant")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<Guid>>(StatusCodes.Status200OK);

        group.MapPut("/tenant-industries",
                async (SetTenantIndustriesCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(cmd, ct);
                    return Results.NoContent();
                })
            .WithName("SetTenantIndustries").WithSummary("Replace current tenant industries")
            .RequirePermission(CatalogPermissions.Categories.Update)
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/global-categories",
                async (string? search, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new GetGlobalCategoriesQuery(search), ct)))
            .WithName("GetGlobalCategories").WithSummary("Global categories filtered by tenant industry")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<GlobalCategoryDto>>(StatusCodes.Status200OK);

        group.MapPost("/import-categories",
                async (ImportGlobalCategoriesCommand cmd, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(cmd, ct)))
            .WithName("ImportGlobalCategories").WithSummary("Adopt global categories into the tenant tree")
            .RequirePermission(CatalogPermissions.Categories.Create)
            .Produces<int>(StatusCodes.Status200OK);

        group.MapGet("/search-categories",
                async (string q, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new SearchGlobalCategoriesQuery(q ?? string.Empty), ct)))
            .WithName("SearchGlobalCategories").WithSummary("Fuzzy search global categories (typo/accent/alias tolerant)")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<GlobalCategorySuggestionDto>>(StatusCodes.Status200OK);

        group.MapGet("/search-brands",
                async (string q, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new SearchGlobalBrandsQuery(q ?? string.Empty), ct)))
            .WithName("SearchGlobalBrands").WithSummary("Fuzzy search global brands (typo/accent/alias tolerant)")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<GlobalBrandSuggestionDto>>(StatusCodes.Status200OK);

        group.MapPost("/adopt-category",
                async (AdoptGlobalCategoryCommand cmd, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(cmd, ct)))
            .WithName("AdoptGlobalCategory").WithSummary("Adopt a global category (with name/slug edits) into the tenant tree")
            .RequirePermission(CatalogPermissions.Categories.Create)
            .Produces<Guid>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

        group.MapGet("/aliases",
                async (CatalogAliasEntity? entityType, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new GetCatalogAliasesQuery(entityType), ct)))
            .WithName("GetCatalogAliases").WithSummary("List catalog aliases/synonyms")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<CatalogAliasDto>>(StatusCodes.Status200OK);

        group.MapPost("/aliases",
                async (AddCatalogAliasCommand cmd, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(cmd, ct)))
            .WithName("AddCatalogAlias").WithSummary("Add a catalog alias/synonym")
            .RequirePermission(CatalogPermissions.Categories.Update)
            .Produces<Guid>(StatusCodes.Status200OK).Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/aliases/{id:guid}",
                async (Guid id, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(new DeleteCatalogAliasCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteCatalogAlias").WithSummary("Delete a catalog alias/synonym")
            .RequirePermission(CatalogPermissions.Categories.Update)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
    }
}
