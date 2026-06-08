using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Scorecards;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Features.v1.Agreements;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.SupplierEvaluation;

internal static class ScorecardDefaults
{
    // (code, name, weight) — suman 100.
    public static readonly (string Code, string Name, decimal Weight)[] Kpis =
    [
        ("CALIDAD",      "Calidad",                35m),
        ("ENTREGA",      "Entrega / cumplimiento", 25m),
        ("PRECIO",       "Precio / costo",         20m),
        ("SERVICIO",     "Servicio / comunicación",10m),
        ("CUMPLIMIENTO", "Cumplimiento documental",10m),
    ];
}

internal static class ScorecardMappings
{
    public static ScorecardKpiDto ToDto(this ScorecardKpi k) => new(k.Id, k.Code, k.Name, k.Weight, k.SortOrder, k.IsActive);
    public static ScorecardCriterionDto ToDto(this ScorecardCriterion c) => new(c.Id, c.KpiCode, c.KpiName, c.Weight, c.Score, c.Comment);
}

// ───────────────────────── KPI catalog ─────────────────────────
public sealed class GetScorecardKpisQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetScorecardKpisQuery, IReadOnlyList<ScorecardKpiDto>>
{
    public async ValueTask<IReadOnlyList<ScorecardKpiDto>> Handle(GetScorecardKpisQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.ScorecardKpis.AsNoTracking();
        if (query.ActiveOnly) q = q.Where(k => k.IsActive);
        return await q.OrderBy(k => k.SortOrder).ThenBy(k => k.Name)
            .Select(k => new ScorecardKpiDto(k.Id, k.Code, k.Name, k.Weight, k.SortOrder, k.IsActive))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class UpsertScorecardKpiCommandValidator : AbstractValidator<UpsertScorecardKpiCommand>
{
    public UpsertScorecardKpiCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("El código del KPI es obligatorio.").MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del KPI es obligatorio.").MaximumLength(200);
        RuleFor(x => x.Weight).InclusiveBetween(0, 100).WithMessage("El peso debe estar entre 0 y 100.");
    }
}

public sealed class UpsertScorecardKpiCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpsertScorecardKpiCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpsertScorecardKpiCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Id is { } id)
        {
            var kpi = await db.ScorecardKpis.FirstOrDefaultAsync(k => k.Id == id, cancellationToken).ConfigureAwait(false)
                ?? throw new CustomException("KPI no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
            kpi.Update(command.Name, command.Weight, command.SortOrder, command.IsActive);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return kpi.Id;
        }

        bool dup = await db.ScorecardKpis.AsNoTracking().AnyAsync(k => k.Code == command.Code, cancellationToken).ConfigureAwait(false);
        if (dup) throw new CustomException("Ya existe un KPI con ese código.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var entity = ScorecardKpi.Create(command.Code, command.Name, command.Weight, command.SortOrder, command.IsActive);
        db.ScorecardKpis.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class DeleteScorecardKpiCommandValidator : AbstractValidator<DeleteScorecardKpiCommand>
{
    public DeleteScorecardKpiCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteScorecardKpiCommandHandler(CatalogDbContext db) : ICommandHandler<DeleteScorecardKpiCommand>
{
    public async ValueTask<Unit> Handle(DeleteScorecardKpiCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var kpi = await db.ScorecardKpis.FirstOrDefaultAsync(k => k.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("KPI no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        db.ScorecardKpis.Remove(kpi);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class SeedDefaultKpisCommandValidator : AbstractValidator<SeedDefaultKpisCommand>
{
    public SeedDefaultKpisCommandValidator() { }
}

public sealed class SeedDefaultKpisCommandHandler(CatalogDbContext db) : ICommandHandler<SeedDefaultKpisCommand, int>
{
    public async ValueTask<int> Handle(SeedDefaultKpisCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await db.ScorecardKpis.AsNoTracking().Select(k => k.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
        int created = 0;
        int order = existing.Count;
        foreach (var (code, name, weight) in ScorecardDefaults.Kpis)
        {
            if (existing.Contains(code, StringComparer.OrdinalIgnoreCase)) continue;
            db.ScorecardKpis.Add(ScorecardKpi.Create(code, name, weight, order++));
            created++;
        }
        if (created > 0) await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}

// ───────────────────────── Scorecards: create ─────────────────────────
public sealed class CreateSupplierScorecardCommandValidator : AbstractValidator<CreateSupplierScorecardCommand>
{
    public CreateSupplierScorecardCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty().WithMessage("El proveedor es obligatorio.");
        RuleFor(x => x.PeriodLabel).NotEmpty().WithMessage("El período es obligatorio.").MaximumLength(32);
        RuleForEach(x => x.Criteria).SetValidator(new ScorecardCriterionInputValidator());
    }
}

public sealed class ScorecardCriterionInputValidator : AbstractValidator<ScorecardCriterionInput>
{
    public ScorecardCriterionInputValidator()
    {
        RuleFor(x => x.KpiCode).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(1, 5).WithMessage("El puntaje de cada KPI debe estar entre 1 y 5.");
        RuleFor(x => x.Weight).InclusiveBetween(0, 100);
    }
}

public sealed class CreateSupplierScorecardCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateSupplierScorecardCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateSupplierScorecardCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = SupplierScorecard.Create(command.SupplierId, command.PeriodLabel, command.PeriodStart, command.Notes);

        IEnumerable<ScorecardCriterion> criteria;
        if (command.Criteria is { Count: > 0 })
        {
            criteria = command.Criteria.Select(c => ScorecardCriterion.Create(entity.Id, c.KpiCode, c.KpiName, c.Weight, c.Score, c.Comment));
        }
        else
        {
            // Sin criterios: generar uno por KPI activo (snapshot), score neutro 3.
            var kpis = await db.ScorecardKpis.AsNoTracking().Where(k => k.IsActive)
                .OrderBy(k => k.SortOrder).ToListAsync(cancellationToken).ConfigureAwait(false);
            criteria = kpis.Select(k => ScorecardCriterion.Create(entity.Id, k.Code, k.Name, k.Weight, 3, null));
        }
        entity.ReplaceCriteria(criteria);

        db.SupplierScorecards.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}

// ───────────────────────── Scorecards: update ─────────────────────────
public sealed class UpdateSupplierScorecardCommandValidator : AbstractValidator<UpdateSupplierScorecardCommand>
{
    public UpdateSupplierScorecardCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PeriodLabel).NotEmpty().WithMessage("El período es obligatorio.").MaximumLength(32);
        RuleForEach(x => x.Criteria).SetValidator(new ScorecardCriterionInputValidator());
    }
}

public sealed class UpdateSupplierScorecardCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateSupplierScorecardCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateSupplierScorecardCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = await db.SupplierScorecards.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Scorecard no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        if (!entity.IsMutable)
            throw new CustomException("Un scorecard cerrado es inmutable.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        entity.Update(command.PeriodLabel, command.PeriodStart, command.Notes);

        // Criterios gestionados a nivel de DbContext (evita el mis-tracking de hijos nuevos).
        var old = await db.ScorecardCriteria.Where(c => c.ScorecardId == entity.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.ScorecardCriteria.RemoveRange(old);
        var fresh = (command.Criteria ?? [])
            .Select(c => ScorecardCriterion.Create(entity.Id, c.KpiCode, c.KpiName, c.Weight, c.Score, c.Comment)).ToList();
        db.ScorecardCriteria.AddRange(fresh);
        entity.RecomputeFrom(fresh);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}

// ───────────────────────── Scorecards: close / delete ─────────────────────────
public sealed class CloseScorecardCommandValidator : AbstractValidator<CloseScorecardCommand>
{
    public CloseScorecardCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class CloseScorecardCommandHandler(CatalogDbContext db) : ICommandHandler<CloseScorecardCommand>
{
    public async ValueTask<Unit> Handle(CloseScorecardCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = await db.SupplierScorecards.Include(s => s.Criteria)
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Scorecard no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        entity.Close();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class DeleteScorecardCommandValidator : AbstractValidator<DeleteScorecardCommand>
{
    public DeleteScorecardCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteScorecardCommandHandler(CatalogDbContext db) : ICommandHandler<DeleteScorecardCommand>
{
    public async ValueTask<Unit> Handle(DeleteScorecardCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = await db.SupplierScorecards.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Scorecard no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        if (entity.Status != ScorecardStatus.Borrador)
            throw new CustomException("Solo se pueden eliminar scorecards en borrador.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        db.SupplierScorecards.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ───────────────────────── Scorecards: list / detail ─────────────────────────
public sealed class GetSupplierScorecardsQueryValidator : AbstractValidator<GetSupplierScorecardsQuery>
{
    public GetSupplierScorecardsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).When(x => x.PageSize.HasValue);
    }
}

public sealed class GetSupplierScorecardsQueryHandler(CatalogDbContext db, IMediator mediator)
    : IQueryHandler<GetSupplierScorecardsQuery, PagedResponse<ScorecardDto>>
{
    public async ValueTask<PagedResponse<ScorecardDto>> Handle(GetSupplierScorecardsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.SupplierScorecards.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            q = q.Where(s => EF.Functions.ILike(s.PeriodLabel, pattern));
        }
        if (query.SupplierId is { } sid && sid != Guid.Empty) q = q.Where(s => s.SupplierId == sid);
        if (query.Status.HasValue) q = q.Where(s => s.Status == query.Status.Value);

        q = (query.Sort?.ToLowerInvariant()) switch
        {
            "score"  => q.OrderBy(s => s.WeightedScore),
            "-score" => q.OrderByDescending(s => s.WeightedScore),
            "period" => q.OrderBy(s => s.PeriodStart),
            _        => q.OrderByDescending(s => s.PeriodStart),
        };

        var paged = await q.Select(s => new ScorecardDto(
                s.Id, s.SupplierId, null, s.PeriodLabel, s.PeriodStart, s.Status, s.WeightedScore, s.Grade))
            .ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);

        var names = new Dictionary<Guid, string?>();
        foreach (var id in paged.Items.Select(i => i.SupplierId).Distinct())
            names[id] = await GetAgreementByIdQueryHandler.TryGetPartyNameAsync(mediator, id, cancellationToken).ConfigureAwait(false);

        return new PagedResponse<ScorecardDto>
        {
            Items = paged.Items.Select(i => i with { SupplierName = names.GetValueOrDefault(i.SupplierId) }).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };
    }
}

public sealed class GetScorecardByIdQueryHandler(CatalogDbContext db, IMediator mediator)
    : IQueryHandler<GetScorecardByIdQuery, ScorecardDetailDto>
{
    public async ValueTask<ScorecardDetailDto> Handle(GetScorecardByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var s = await db.SupplierScorecards.AsNoTracking().Include(x => x.Criteria)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Scorecard no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        string? supplierName = await GetAgreementByIdQueryHandler.TryGetPartyNameAsync(mediator, s.SupplierId, cancellationToken).ConfigureAwait(false);

        return new ScorecardDetailDto(
            s.Id, s.SupplierId, supplierName, s.PeriodLabel, s.PeriodStart, s.Status, s.WeightedScore, s.Grade,
            s.Notes, s.IsMutable,
            s.Criteria.OrderBy(c => c.KpiName).Select(c => c.ToDto()).ToList());
    }
}

// ───────────────────────── Reports: ranking / trend ─────────────────────────
public sealed class GetSupplierRankingQueryHandler(CatalogDbContext db, IMediator mediator)
    : IQueryHandler<GetSupplierRankingQuery, IReadOnlyList<SupplierRankingDto>>
{
    public async ValueTask<IReadOnlyList<SupplierRankingDto>> Handle(GetSupplierRankingQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        // Último scorecard por proveedor (por PeriodStart).
        var latest = await db.SupplierScorecards.AsNoTracking()
            .GroupBy(s => s.SupplierId)
            .Select(g => g.OrderByDescending(s => s.PeriodStart).First())
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = new List<SupplierRankingDto>(latest.Count);
        foreach (var s in latest.OrderByDescending(s => s.WeightedScore))
        {
            string? name = await GetAgreementByIdQueryHandler.TryGetPartyNameAsync(mediator, s.SupplierId, cancellationToken).ConfigureAwait(false);
            result.Add(new SupplierRankingDto(s.SupplierId, name, s.PeriodLabel, s.WeightedScore, s.Grade));
        }
        return result;
    }
}

public sealed class GetSupplierScoreTrendQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetSupplierScoreTrendQuery, IReadOnlyList<ScoreTrendPointDto>>
{
    public async ValueTask<IReadOnlyList<ScoreTrendPointDto>> Handle(GetSupplierScoreTrendQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await db.SupplierScorecards.AsNoTracking()
            .Where(s => s.SupplierId == query.SupplierId)
            .OrderBy(s => s.PeriodStart)
            .Select(s => new ScoreTrendPointDto(s.PeriodLabel, s.PeriodStart, s.WeightedScore))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

// ───────────────────────── Endpoints ─────────────────────────
public static class ScorecardKpiEndpoints
{
    public static void MapScorecardKpiEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/",
                async ([AsParameters] GetScorecardKpisQuery query, IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(query, ct)))
            .WithName("GetScorecardKpis").WithSummary("List scorecard KPIs")
            .RequirePermission(CatalogPermissions.Scorecards.View)
            .Produces<IReadOnlyList<ScorecardKpiDto>>();

        group.MapPost("/",
                async (UpsertScorecardKpiCommand cmd, IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(cmd, ct)))
            .WithName("UpsertScorecardKpi").WithSummary("Create or update a scorecard KPI")
            .RequirePermission(CatalogPermissions.Scorecards.Manage)
            .Produces<Guid>().Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPost("/seed-defaults",
                async (IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(new SeedDefaultKpisCommand(), ct)))
            .WithName("SeedDefaultScorecardKpis").WithSummary("Seed the default KPI catalog if empty")
            .RequirePermission(CatalogPermissions.Scorecards.Manage)
            .Produces<int>();

        group.MapDelete("/{id:guid}",
                async (Guid id, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(new DeleteScorecardKpiCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteScorecardKpi").WithSummary("Delete a scorecard KPI")
            .RequirePermission(CatalogPermissions.Scorecards.Manage)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
    }
}

public static class SupplierScorecardEndpoints
{
    public static void MapSupplierScorecardEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/",
                async ([AsParameters] GetSupplierScorecardsQuery query, IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(query, ct)))
            .WithName("GetSupplierScorecards").WithSummary("List supplier scorecards")
            .RequirePermission(CatalogPermissions.Scorecards.View)
            .Produces<PagedResponse<ScorecardDto>>();

        group.MapGet("/ranking",
                async (IMediator m, CancellationToken ct) => TypedResults.Ok(await m.Send(new GetSupplierRankingQuery(), ct)))
            .WithName("GetSupplierRanking").WithSummary("Supplier ranking by latest score")
            .RequirePermission(CatalogPermissions.Scorecards.View)
            .Produces<IReadOnlyList<SupplierRankingDto>>();

        group.MapGet("/trend/{supplierId:guid}",
                async (Guid supplierId, IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(new GetSupplierScoreTrendQuery(supplierId), ct)))
            .WithName("GetSupplierScoreTrend").WithSummary("Supplier score trend over periods")
            .RequirePermission(CatalogPermissions.Scorecards.View)
            .Produces<IReadOnlyList<ScoreTrendPointDto>>();

        group.MapGet("/{id:guid}",
                async (Guid id, IMediator m, CancellationToken ct) => TypedResults.Ok(await m.Send(new GetScorecardByIdQuery(id), ct)))
            .WithName("GetScorecardById").WithSummary("Scorecard detail")
            .RequirePermission(CatalogPermissions.Scorecards.View)
            .Produces<ScorecardDetailDto>().Produces(StatusCodes.Status404NotFound);

        group.MapPost("/",
                async (CreateSupplierScorecardCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    Guid id = await m.Send(cmd, ct);
                    return TypedResults.Created($"/api/v1/catalog/supplier-scorecards/{id}", id);
                })
            .WithName("CreateSupplierScorecard").WithSummary("Create a supplier scorecard")
            .RequirePermission(CatalogPermissions.Scorecards.Manage)
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}",
                async (Guid id, UpdateSupplierScorecardCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    if (id != cmd.Id) return Results.BadRequest();
                    await m.Send(cmd, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateSupplierScorecard").WithSummary("Update a supplier scorecard")
            .RequirePermission(CatalogPermissions.Scorecards.Manage)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/close",
                async (Guid id, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(new CloseScorecardCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("CloseScorecard").WithSummary("Close (freeze) a scorecard")
            .RequirePermission(CatalogPermissions.Scorecards.Manage)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}",
                async (Guid id, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(new DeleteScorecardCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteScorecard").WithSummary("Delete a draft scorecard")
            .RequirePermission(CatalogPermissions.Scorecards.Manage)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status409Conflict);
    }
}
