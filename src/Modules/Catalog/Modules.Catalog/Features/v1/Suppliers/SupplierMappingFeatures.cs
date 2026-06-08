using FluentValidation;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Suppliers;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Suppliers;

// ── Global brands (para el picker de mapeo) ──────────────────────────────────
public sealed class GetGlobalBrandsQueryHandler(IGlobalCatalogReader global)
    : IQueryHandler<GetGlobalBrandsQuery, IReadOnlyList<GlobalBrandDto>>
{
    public async ValueTask<IReadOnlyList<GlobalBrandDto>> Handle(GetGlobalBrandsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await global.RunAsync(async (db, ct) =>
        {
            var q = db.Brands.AsNoTracking().Where(b => !b.IsDeleted);
            if (search is not null) q = q.Where(b => EF.Functions.ILike(b.Name, $"%{search}%"));
            return (IReadOnlyList<GlobalBrandDto>)await q.OrderBy(b => b.Name)
                .Select(b => new GlobalBrandDto(b.Id, b.Name, b.CountryOfOrigin))
                .ToListAsync(ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
}

// ── Supplier brands ──────────────────────────────────────────────────────────
public sealed class GetSupplierBrandsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetSupplierBrandsQuery, IReadOnlyList<Guid>>
{
    public async ValueTask<IReadOnlyList<Guid>> Handle(GetSupplierBrandsQuery query, CancellationToken cancellationToken) =>
        await db.SupplierBrands.AsNoTracking().Where(x => x.SupplierId == query.SupplierId)
            .Select(x => x.BrandId).ToListAsync(cancellationToken).ConfigureAwait(false);
}

public sealed class SetSupplierBrandsCommandValidator : AbstractValidator<SetSupplierBrandsCommand>
{
    public SetSupplierBrandsCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.BrandIds).NotNull();
    }
}

public sealed class SetSupplierBrandsCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetSupplierBrandsCommand>
{
    public async ValueTask<Unit> Handle(SetSupplierBrandsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await db.SupplierBrands.Where(x => x.SupplierId == command.SupplierId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        db.SupplierBrands.RemoveRange(existing);
        foreach (var brandId in command.BrandIds.Distinct())
            db.SupplierBrands.Add(SupplierBrand.Create(command.SupplierId, brandId));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ── Supplier categories ──────────────────────────────────────────────────────
public sealed class GetSupplierCategoriesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetSupplierCategoriesQuery, IReadOnlyList<Guid>>
{
    public async ValueTask<IReadOnlyList<Guid>> Handle(GetSupplierCategoriesQuery query, CancellationToken cancellationToken) =>
        await db.SupplierCategories.AsNoTracking().Where(x => x.SupplierId == query.SupplierId)
            .Select(x => x.CategoryId).ToListAsync(cancellationToken).ConfigureAwait(false);
}

public sealed class SetSupplierCategoriesCommandValidator : AbstractValidator<SetSupplierCategoriesCommand>
{
    public SetSupplierCategoriesCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CategoryIds).NotNull();
    }
}

public sealed class SetSupplierCategoriesCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetSupplierCategoriesCommand>
{
    public async ValueTask<Unit> Handle(SetSupplierCategoriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await db.SupplierCategories.Where(x => x.SupplierId == command.SupplierId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        db.SupplierCategories.RemoveRange(existing);
        foreach (var categoryId in command.CategoryIds.Distinct())
            db.SupplierCategories.Add(SupplierCategory.Create(command.SupplierId, categoryId));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ── Endpoints ────────────────────────────────────────────────────────────────
public static class SupplierMappingEndpoints
{
    public static void MapSupplierMappingEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/global-brands",
                async (string? search, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new GetGlobalBrandsQuery(search), ct)))
            .WithName("GetGlobalBrands").WithSummary("Global canonical brands")
            .RequirePermission(CatalogPermissions.Brands.View)
            .Produces<IReadOnlyList<GlobalBrandDto>>(StatusCodes.Status200OK);

        group.MapGet("/{supplierId:guid}/brands",
                async (Guid supplierId, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new GetSupplierBrandsQuery(supplierId), ct)))
            .WithName("GetSupplierBrands").WithSummary("Brands a supplier trades")
            .RequirePermission(CatalogPermissions.Brands.View)
            .Produces<IReadOnlyList<Guid>>(StatusCodes.Status200OK);

        group.MapPut("/{supplierId:guid}/brands",
                async (Guid supplierId, SetSupplierBrandsCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    if (supplierId != cmd.SupplierId) return Results.BadRequest("Route id mismatch.");
                    await m.Send(cmd, ct);
                    return Results.NoContent();
                })
            .WithName("SetSupplierBrands").WithSummary("Replace supplier brands")
            .RequirePermission(CatalogPermissions.Brands.Update)
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/{supplierId:guid}/categories",
                async (Guid supplierId, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new GetSupplierCategoriesQuery(supplierId), ct)))
            .WithName("GetSupplierCategories").WithSummary("Categories a supplier trades")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<Guid>>(StatusCodes.Status200OK);

        group.MapPut("/{supplierId:guid}/categories",
                async (Guid supplierId, SetSupplierCategoriesCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    if (supplierId != cmd.SupplierId) return Results.BadRequest("Route id mismatch.");
                    await m.Send(cmd, ct);
                    return Results.NoContent();
                })
            .WithName("SetSupplierCategories").WithSummary("Replace supplier categories")
            .RequirePermission(CatalogPermissions.Categories.Update)
            .Produces(StatusCodes.Status204NoContent);
    }
}
