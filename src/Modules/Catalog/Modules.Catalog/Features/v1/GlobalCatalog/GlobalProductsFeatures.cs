using System.Net;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
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

internal sealed record ImgSnap(string Url, string? AltText, bool IsPrimary, int SortOrder);

// ── Publicar producto del tenant al catálogo global ──────────────────────────
public sealed class PublishProductToGlobalCommandValidator : AbstractValidator<PublishProductToGlobalCommand>
{
    public PublishProductToGlobalCommandValidator() => RuleFor(x => x.ProductId).NotEmpty();
}

public sealed class PublishProductToGlobalCommandHandler(
    CatalogDbContext db, IGlobalCatalogReader global, IMultiTenantContextAccessor<AppTenantInfo> tenant)
    : ICommandHandler<PublishProductToGlobalCommand, Guid>
{
    public async ValueTask<Guid> Handle(PublishProductToGlobalCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var p = await db.Products.Include(x => x.Images).Include(x => x.Variations)
            .FirstOrDefaultAsync(x => x.Id == command.ProductId && !x.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Producto no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        string ownerId = tenant.MultiTenantContext?.TenantInfo?.Id ?? "unknown";
        string? sku = (p.Variations.FirstOrDefault(v => v.IsDefault) ?? p.Variations.FirstOrDefault())?.Sku;
        string globalSlug = $"{ownerId}-{p.Slug}".ToLowerInvariant();
        string globalSku = sku is null ? $"{globalSlug}-default" : $"{ownerId}-{sku}".ToLowerInvariant();
        string name = p.Name;
        string? shortDesc = p.ShortDescription, desc = p.Description, specs = p.TechnicalSpecs;
        string? specsJson = p.Specs?.RootElement.GetRawText();
        var type = p.Type;
        var imgs = p.Images.OrderBy(i => i.SortOrder)
            .Select(i => new ImgSnap(i.Url, i.AltText, i.IsPrimary, i.SortOrder)).ToList();

        // Mark the source product as public/shared.
        if (!p.IsPublic)
        {
            p.UpdateDetails(p.Name, p.ShortDescription, p.Description, p.TechnicalSpecs, p.BrandId, p.TaxRateId,
                p.ShippingClassId, p.IsVirtual, p.IsDownloadable, isPublic: true);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await global.RunAsync(async (gdb, ct) =>
        {
            var existing = await gdb.Products.Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Slug == globalSlug, ct).ConfigureAwait(false);

            Product gp;
            if (existing is null)
            {
                gp = Product.Create(name, globalSlug, type == ProductType.Variable ? ProductType.Simple : type,
                    shortDescription: shortDesc, defaultSku: globalSku);
                gdb.Products.Add(gp);
            }
            else
            {
                gp = existing;
                foreach (var img in gp.Images.ToList()) gdb.Remove(img);
            }

            gp.UpdateDetails(name, shortDesc, desc, specs, null, null, null, false, false, isPublic: true);
            gp.UpdateSpecs(specsJson is null ? null : JsonDocument.Parse(specsJson));
            gp.Publish();
            foreach (var i in imgs) gp.Images.Add(ProductImage.Create(gp.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder));

            await gdb.SaveChangesAsync(ct).ConfigureAwait(false);
            return gp.Id;
        }, cancellationToken).ConfigureAwait(false);
    }
}

// ── Búsqueda difusa de productos globales ────────────────────────────────────
public sealed class SearchGlobalProductsQueryValidator : AbstractValidator<SearchGlobalProductsQuery>
{
    public SearchGlobalProductsQueryValidator() => RuleFor(x => x.Q).NotEmpty().MinimumLength(2);
}

public sealed class SearchGlobalProductsQueryHandler(CatalogDbContext db, IGlobalCatalogReader global)
    : IQueryHandler<SearchGlobalProductsQuery, IReadOnlyList<GlobalProductSuggestionDto>>
{
    private sealed record Hit(Guid Id, string Name, string? DefaultSku, string? ShortDescription, string? ImageUrl, double Score);

    public async ValueTask<IReadOnlyList<GlobalProductSuggestionDto>> Handle(SearchGlobalProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        string q = query.Q.Trim();
        if (q.Length < 2) return [];

        var hits = await global.RunAsync(async (gdb, ct) =>
            await gdb.Database.SqlQuery<Hit>($"""
                SELECT p."Id" AS "Id", p."Name" AS "Name",
                       (SELECT v."Sku" FROM catalog."ProductVariations" v
                        WHERE v."ProductId" = p."Id" AND v."IsDefault" = true LIMIT 1) AS "DefaultSku",
                       p."ShortDescription" AS "ShortDescription",
                       (SELECT i."Url" FROM catalog."ProductImages" i
                        WHERE i."ProductId" = p."Id" ORDER BY i."IsPrimary" DESC, i."SortOrder" LIMIT 1) AS "ImageUrl",
                       GREATEST(similarity(unaccent(lower(p."Name")), unaccent(lower({q}))),
                                word_similarity(unaccent(lower({q})), unaccent(lower(p."Name"))))::double precision AS "Score"
                FROM catalog."Products" p
                WHERE p."TenantId" = 'global' AND NOT p."IsDeleted" AND p."IsPublic" = true
                  AND (unaccent(lower(p."Name")) % unaccent(lower({q}))
                       OR unaccent(lower({q})) <% unaccent(lower(p."Name"))
                       OR EXISTS (SELECT 1 FROM catalog."ProductVariations" v
                                  WHERE v."ProductId" = p."Id" AND unaccent(lower(v."Sku")) % unaccent(lower({q}))))
                ORDER BY "Score" DESC
                LIMIT 20
                """).ToListAsync(ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        // AlreadyAdopted: a tenant product with the same name (case/accent-insensitive) already exists.
        var adopted = (await db.Products.AsNoTracking().Where(p => !p.IsDeleted)
                .Select(p => p.Name).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(n => n.ToLowerInvariant()).ToHashSet();

        return hits.Select(h => new GlobalProductSuggestionDto(
            h.Id, h.Name, h.DefaultSku, h.ShortDescription, h.ImageUrl, h.Score,
            adopted.Contains(h.Name.ToLowerInvariant()))).ToList();
    }
}

// ── Adoptar producto global al catálogo del tenant ───────────────────────────
public sealed class AdoptGlobalProductCommandValidator : AbstractValidator<AdoptGlobalProductCommand>
{
    public AdoptGlobalProductCommandValidator() => RuleFor(x => x.GlobalProductId).NotEmpty();
}

public sealed class AdoptGlobalProductCommandHandler(CatalogDbContext db, IGlobalCatalogReader global)
    : ICommandHandler<AdoptGlobalProductCommand, Guid>
{
    private sealed record Snap(string Name, string? ShortDescription, string? Description, string? TechnicalSpecs,
        string? SpecsJson, ProductType Type, string? Sku, List<ImgSnap> Images);

    public async ValueTask<Guid> Handle(AdoptGlobalProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var snap = await global.RunAsync(async (gdb, ct) =>
        {
            var p = await gdb.Products.Include(x => x.Images).Include(x => x.Variations)
                .FirstOrDefaultAsync(x => x.Id == command.GlobalProductId && !x.IsDeleted, ct).ConfigureAwait(false);
            if (p is null) return null;
            string? sku = (p.Variations.FirstOrDefault(v => v.IsDefault) ?? p.Variations.FirstOrDefault())?.Sku;
            return new Snap(p.Name, p.ShortDescription, p.Description, p.TechnicalSpecs,
                p.Specs?.RootElement.GetRawText(), p.Type, sku,
                p.Images.OrderBy(i => i.SortOrder).Select(i => new ImgSnap(i.Url, i.AltText, i.IsPrimary, i.SortOrder)).ToList());
        }, cancellationToken).ConfigureAwait(false);

        if (snap is null)
            throw new CustomException("Producto global no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        string slug = SlugHelper.Build(null, snap.Name);
        // Keep slug/sku unique within the tenant (Sku is globally unique). A clean
        // {slug}-default SKU is generated for the adopter to edit.
        bool slugTaken = await db.Products.AsNoTracking().AnyAsync(p => p.Slug == slug, cancellationToken).ConfigureAwait(false);
        if (slugTaken) slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var product = Product.Create(snap.Name, slug, snap.Type == ProductType.Variable ? ProductType.Simple : snap.Type,
            shortDescription: snap.ShortDescription, defaultSku: $"{slug}-default");
        product.UpdateDetails(snap.Name, snap.ShortDescription, snap.Description, snap.TechnicalSpecs,
            null, null, null, false, false, isPublic: false);
        product.UpdateSpecs(snap.SpecsJson is null ? null : JsonDocument.Parse(snap.SpecsJson));
        foreach (var i in snap.Images) product.Images.Add(ProductImage.Create(product.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder));

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return product.Id;
    }
}

// ── Endpoints ────────────────────────────────────────────────────────────────
public static class GlobalProductEndpoints
{
    public static void MapGlobalProductEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapPost("/{id:guid}/publish-global",
                async (Guid id, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new PublishProductToGlobalCommand(id), ct)))
            .WithName("PublishProductToGlobal").WithSummary("Publish a tenant product to the global catalog")
            .RequirePermission(CatalogPermissions.Products.Publish)
            .Produces<Guid>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
    }

    public static void MapGlobalProductCatalogEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/search-products",
                async (string q, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new SearchGlobalProductsQuery(q ?? string.Empty), ct)))
            .WithName("SearchGlobalProducts").WithSummary("Fuzzy search published global products")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<IReadOnlyList<GlobalProductSuggestionDto>>(StatusCodes.Status200OK);

        group.MapPost("/adopt-product",
                async (AdoptGlobalProductCommand cmd, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(cmd, ct)))
            .WithName("AdoptGlobalProduct").WithSummary("Adopt a global product into the tenant catalog")
            .RequirePermission(CatalogPermissions.Products.Create)
            .Produces<Guid>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
    }
}
