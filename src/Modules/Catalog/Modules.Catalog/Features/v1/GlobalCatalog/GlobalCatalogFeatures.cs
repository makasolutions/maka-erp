using FluentValidation;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.GlobalCatalog;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
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
    }
}
