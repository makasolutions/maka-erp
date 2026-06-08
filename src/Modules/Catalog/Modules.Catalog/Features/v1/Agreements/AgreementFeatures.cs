using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Agreements;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Agreements;

// ───────────────────────── Mapping ─────────────────────────
internal static class AgreementMappings
{
    public static AgreementRuleDto ToDto(this AgreementRule r) =>
        new(r.Id, r.RuleType, r.NumericValue, r.BoolValue, r.TextValue, r.IsMandatory);

    public static IEnumerable<AgreementRule> ToEntities(this IReadOnlyList<AgreementRuleInput>? rules, Guid agreementId) =>
        (rules ?? []).Select(r => AgreementRule.Create(agreementId, r.RuleType, r.NumericValue, r.BoolValue, r.TextValue, r.IsMandatory));

    // PostgreSQL timestamptz columns require UTC; date-only inputs arrive as Kind=Unspecified.
    public static DateTime AsUtc(this DateTime v) =>
        v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);
    public static DateTime? AsUtc(this DateTime? v) => v.HasValue ? v.Value.AsUtc() : null;
}

// ───────────────────────── Validators ─────────────────────────
public sealed class CreateAgreementCommandValidator : AbstractValidator<CreateAgreementCommand>
{
    public CreateAgreementCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del convenio es obligatorio.").MaximumLength(200);
        RuleFor(x => x.SupplierId).NotEmpty().WithMessage("El proveedor es obligatorio.");
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .When(x => x.ValidTo.HasValue).WithMessage("La fecha final no puede ser anterior a la inicial.");
        RuleForEach(x => x.Rules).SetValidator(new AgreementRuleInputValidator());
    }
}

public sealed class UpdateAgreementCommandValidator : AbstractValidator<UpdateAgreementCommand>
{
    public UpdateAgreementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del convenio es obligatorio.").MaximumLength(200);
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .When(x => x.ValidTo.HasValue).WithMessage("La fecha final no puede ser anterior a la inicial.");
        RuleForEach(x => x.Rules).SetValidator(new AgreementRuleInputValidator());
    }
}

public sealed class ChangeAgreementStatusCommandValidator : AbstractValidator<ChangeAgreementStatusCommand>
{
    public ChangeAgreementStatusCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteAgreementCommandValidator : AbstractValidator<DeleteAgreementCommand>
{
    public DeleteAgreementCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class GetAgreementsQueryValidator : AbstractValidator<GetAgreementsQuery>
{
    public GetAgreementsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).When(x => x.PageSize.HasValue);
    }
}

public sealed class AgreementRuleInputValidator : AbstractValidator<AgreementRuleInput>
{
    private static readonly AgreementRuleType[] NumericRules =
    [
        AgreementRuleType.CompraMinimaMes, AgreementRuleType.CompraMinimaAnio,
        AgreementRuleType.AntiguedadMinimaMeses, AgreementRuleType.CalificacionMinima,
        AgreementRuleType.SlaEntregaDias, AgreementRuleType.CumplimientoMinimo,
    ];

    public AgreementRuleInputValidator()
    {
        RuleFor(x => x.NumericValue).NotNull().GreaterThanOrEqualTo(0)
            .When(x => NumericRules.Contains(x.RuleType))
            .WithMessage("Esta regla requiere un valor numérico válido (≥ 0).");
        RuleFor(x => x.TextValue).NotEmpty()
            .When(x => x.RuleType == AgreementRuleType.DocumentoExigido)
            .WithMessage("Indica el documento exigido.");
    }
}

// ───────────────────────── Create ─────────────────────────
public sealed class CreateAgreementCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateAgreementCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateAgreementCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await ValidatePriceListsAsync(db, command.PriceListId, command.SuggestedPriceListId, cancellationToken).ConfigureAwait(false);

        var entity = Agreement.Create(
            command.Name, command.SupplierId, command.AgreementType, command.PriceListId, command.SuggestedPriceListId,
            command.DispatchResponsible, command.WaybillResponsible, command.SettlementResponsible,
            command.FailedDeliveryPolicy, command.ReturnsPolicy, command.WarrantyPolicy,
            command.ValidFrom.AsUtc(), command.ValidTo.AsUtc(), command.Notes);
        entity.ReplaceRules(command.Rules.ToEntities(entity.Id));

        db.Agreements.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }

    internal static async Task ValidatePriceListsAsync(CatalogDbContext db, Guid? priceListId, Guid? suggestedId, CancellationToken ct)
    {
        foreach (var id in new[] { priceListId, suggestedId })
        {
            if (id is { } pl && pl != Guid.Empty)
            {
                bool exists = await db.PriceLists.AsNoTracking().AnyAsync(p => p.Id == pl, ct).ConfigureAwait(false);
                if (!exists)
                    throw new CustomException("Lista de precios no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
            }
        }
    }
}

// ───────────────────────── Update ─────────────────────────
public sealed class UpdateAgreementCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateAgreementCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateAgreementCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = await db.Agreements.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Convenio no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        if (!entity.IsMutable)
            throw new CustomException("Un convenio terminado es inmutable.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        await CreateAgreementCommandHandler.ValidatePriceListsAsync(db, command.PriceListId, command.SuggestedPriceListId, cancellationToken).ConfigureAwait(false);

        entity.Update(
            command.Name, command.AgreementType, command.PriceListId, command.SuggestedPriceListId,
            command.DispatchResponsible, command.WaybillResponsible, command.SettlementResponsible,
            command.FailedDeliveryPolicy, command.ReturnsPolicy, command.WarrantyPolicy,
            command.ValidFrom.AsUtc(), command.ValidTo.AsUtc(), command.Notes);

        // Rules replaced at the db level to dodge EF's new-child mis-tracking on a tracked graph.
        var old = await db.AgreementRules.Where(r => r.AgreementId == entity.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.AgreementRules.RemoveRange(old);
        db.AgreementRules.AddRange(command.Rules.ToEntities(entity.Id));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}

// ───────────────────────── Change status ─────────────────────────
public sealed class ChangeAgreementStatusCommandHandler(CatalogDbContext db)
    : ICommandHandler<ChangeAgreementStatusCommand>
{
    public async ValueTask<Unit> Handle(ChangeAgreementStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = await db.Agreements.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Convenio no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        if (!entity.IsMutable)
            throw new CustomException("Un convenio terminado es inmutable.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        entity.ChangeStatus(command.Status);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ───────────────────────── Delete ─────────────────────────
public sealed class DeleteAgreementCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteAgreementCommand>
{
    public async ValueTask<Unit> Handle(DeleteAgreementCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = await db.Agreements.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Convenio no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        if (entity.Status != AgreementStatus.Borrador)
            throw new CustomException("Solo se pueden eliminar convenios en borrador. Usa 'Terminar' para cerrarlo.",
                Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        db.Agreements.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ───────────────────────── List ─────────────────────────
public sealed class GetAgreementsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetAgreementsQuery, PagedResponse<AgreementDto>>
{
    public async ValueTask<PagedResponse<AgreementDto>> Handle(GetAgreementsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.Agreements.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            q = q.Where(a => EF.Functions.ILike(a.Name, pattern));
        }
        if (query.SupplierId is { } sid && sid != Guid.Empty) q = q.Where(a => a.SupplierId == sid);
        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status.Value);

        q = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"  => q.OrderBy(a => a.Name),
            "-name" => q.OrderByDescending(a => a.Name),
            _       => q.OrderByDescending(a => a.CreatedAtUtc),
        };

        // SupplierName is resolved client-side from the parties cache (avoids cross-module N+1).
        return await (from a in q
                      join pl in db.PriceLists.AsNoTracking() on a.PriceListId equals pl.Id into plj
                      from pl in plj.DefaultIfEmpty()
                      select new AgreementDto(
                          a.Id, a.Name, a.SupplierId, null, a.AgreementType, a.Status,
                          a.PriceListId, pl != null ? pl.Name : null,
                          a.ValidFrom, a.ValidTo, a.Rules.Count))
            .ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

// ───────────────────────── Detail ─────────────────────────
public sealed class GetAgreementByIdQueryHandler(CatalogDbContext db, IMediator mediator)
    : IQueryHandler<GetAgreementByIdQuery, AgreementDetailDto>
{
    public async ValueTask<AgreementDetailDto> Handle(GetAgreementByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var a = await db.Agreements.AsNoTracking().Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Convenio no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        string? priceListName = a.PriceListId is { } p ? await db.PriceLists.AsNoTracking()
            .Where(x => x.Id == p).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) : null;
        string? suggestedName = a.SuggestedPriceListId is { } s ? await db.PriceLists.AsNoTracking()
            .Where(x => x.Id == s).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) : null;

        string? supplierName = await TryGetPartyNameAsync(mediator, a.SupplierId, cancellationToken).ConfigureAwait(false);

        return new AgreementDetailDto(
            a.Id, a.Name, a.SupplierId, supplierName, a.AgreementType, a.Status,
            a.PriceListId, priceListName, a.SuggestedPriceListId, suggestedName,
            a.DispatchResponsible, a.WaybillResponsible, a.SettlementResponsible,
            a.FailedDeliveryPolicy, a.ReturnsPolicy, a.WarrantyPolicy,
            a.ValidFrom, a.ValidTo, a.Notes, a.IsMutable,
            a.Rules.Select(r => r.ToDto()).ToList());
    }

    internal static async Task<string?> TryGetPartyNameAsync(IMediator mediator, Guid partyId, CancellationToken ct)
    {
        try
        {
            var party = await mediator.Send(new GetPartyByIdQuery(partyId), ct).ConfigureAwait(false);
            return party.LegalName;
        }
        catch (CustomException)
        {
            return null;
        }
    }
}

// ───────────────────────── Evaluate ─────────────────────────
public sealed class EvaluateAgreementQueryHandler(CatalogDbContext db, IMediator mediator)
    : IQueryHandler<EvaluateAgreementQuery, AgreementEvaluationDto>
{
    public async ValueTask<AgreementEvaluationDto> Handle(EvaluateAgreementQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var a = await db.Agreements.AsNoTracking().Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.Id == query.AgreementId, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Convenio no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        PartyDetailDto party;
        try
        {
            party = await mediator.Send(new GetPartyByIdQuery(query.DistributorPartyId), cancellationToken).ConfigureAwait(false);
        }
        catch (CustomException)
        {
            throw new CustomException("Distribuidor no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        }

        var results = a.Rules.Select(r => Evaluate(r, party)).ToList();
        bool eligible = results.All(r => !(r.IsMandatory && r.Result == RuleEvaluationResult.NoCumple));

        return new AgreementEvaluationDto(a.Id, party.Id, party.LegalName, eligible, results);
    }

    private static RuleEvaluationDto Evaluate(AgreementRule rule, PartyDetailDto p)
    {
        switch (rule.RuleType)
        {
            case AgreementRuleType.AntiguedadMinimaMeses:
            {
                int months = (int)((DateTime.UtcNow - p.CreatedAtUtc).TotalDays / 30.0);
                int required = (int)(rule.NumericValue ?? 0);
                return Result(rule, months >= required ? RuleEvaluationResult.Cumple : RuleEvaluationResult.NoCumple,
                    $"Antigüedad {months} meses (requiere {required}).");
            }
            case AgreementRuleType.VendeAEmpresa:
                return Result(rule, p.Kind == PartyKind.Juridica ? RuleEvaluationResult.Cumple : RuleEvaluationResult.NoCumple,
                    p.Kind == PartyKind.Juridica ? "Es persona jurídica." : "No es persona jurídica.");
            case AgreementRuleType.VendeANatural:
                return Result(rule, p.Kind == PartyKind.Natural ? RuleEvaluationResult.Cumple : RuleEvaluationResult.NoCumple,
                    p.Kind == PartyKind.Natural ? "Es persona natural." : "No es persona natural.");
            case AgreementRuleType.DocumentoExigido:
            {
                bool ok = !string.IsNullOrWhiteSpace(p.IdentificationNumber)
                    && (string.IsNullOrWhiteSpace(rule.TextValue)
                        || string.Equals(p.IdentificationTypeCode, rule.TextValue, StringComparison.OrdinalIgnoreCase));
                return Result(rule, ok ? RuleEvaluationResult.Cumple : RuleEvaluationResult.NoCumple,
                    ok ? "Documento presente." : "Falta el documento exigido.");
            }
            default:
                return Result(rule, RuleEvaluationResult.Pendiente, "Pendiente: requiere datos de ventas/calificación (Fase F).");
        }
    }

    private static RuleEvaluationDto Result(AgreementRule rule, RuleEvaluationResult result, string detail) =>
        new(rule.RuleType, result, rule.IsMandatory, detail);
}

// ───────────────────────── Endpoints ─────────────────────────
public static class AgreementEndpoints
{
    public static void MapAgreementEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/",
                async ([AsParameters] GetAgreementsQuery query, IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(query, ct)))
            .WithName("GetAgreements").WithSummary("List commercial agreements")
            .RequirePermission(CatalogPermissions.Agreements.View)
            .Produces<PagedResponse<AgreementDto>>();

        group.MapGet("/{id:guid}",
                async (Guid id, IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(new GetAgreementByIdQuery(id), ct)))
            .WithName("GetAgreementById").WithSummary("Agreement detail")
            .RequirePermission(CatalogPermissions.Agreements.View)
            .Produces<AgreementDetailDto>().Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/evaluate",
                async (Guid id, Guid distributorId, IMediator m, CancellationToken ct) =>
                    TypedResults.Ok(await m.Send(new EvaluateAgreementQuery(id, distributorId), ct)))
            .WithName("EvaluateAgreement").WithSummary("Evaluate a distributor against the agreement rules")
            .RequirePermission(CatalogPermissions.Agreements.View)
            .Produces<AgreementEvaluationDto>().Produces(StatusCodes.Status404NotFound);

        group.MapPost("/",
                async (CreateAgreementCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    Guid id = await m.Send(cmd, ct);
                    return TypedResults.Created($"/api/v1/catalog/agreements/{id}", id);
                })
            .WithName("CreateAgreement").WithSummary("Create an agreement")
            .RequirePermission(CatalogPermissions.Agreements.Manage)
            .Produces<Guid>(StatusCodes.Status201Created).Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}",
                async (Guid id, UpdateAgreementCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    if (id != cmd.Id) return Results.BadRequest();
                    await m.Send(cmd, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateAgreement").WithSummary("Update an agreement")
            .RequirePermission(CatalogPermissions.Agreements.Manage)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/status",
                async (Guid id, ChangeAgreementStatusCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(cmd with { Id = id }, ct);
                    return Results.NoContent();
                })
            .WithName("ChangeAgreementStatus").WithSummary("Change agreement status")
            .RequirePermission(CatalogPermissions.Agreements.Manage)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}",
                async (Guid id, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(new DeleteAgreementCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteAgreement").WithSummary("Delete a draft agreement")
            .RequirePermission(CatalogPermissions.Agreements.Manage)
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status409Conflict);
    }
}
