using System.Net;
using FluentValidation;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Hr.Contracts.Authorization;
using FSH.Modules.Hr.Contracts.v1.Employees;
using FSH.Modules.Hr.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Hr.Features.v1.Employees;

public sealed class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).When(x => x.PageSize.HasValue);
    }
}

public sealed class GetEmployeesQueryHandler(HrDbContext db)
    : IQueryHandler<GetEmployeesQuery, PagedResponse<EmployeeDto>>
{
    public async ValueTask<PagedResponse<EmployeeDto>> Handle(GetEmployeesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var employees = db.Employees.AsNoTracking().Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pat = $"%{query.Search.Trim()}%";
            employees = employees.Where(e =>
                (e.PositionCode != null && EF.Functions.ILike(e.PositionCode, pat)) ||
                (e.BranchCode != null && EF.Functions.ILike(e.BranchCode, pat)));
        }
        if (query.PayrollEnabled.HasValue) employees = employees.Where(e => e.PayrollEnabled == query.PayrollEnabled.Value);

        employees = (query.Sort?.ToLowerInvariant()) switch
        {
            "createdat" or "createdatutc" => employees.OrderBy(e => e.CreatedAtUtc),
            "-createdat" or "-createdatutc" => employees.OrderByDescending(e => e.CreatedAtUtc),
            _ => employees.OrderByDescending(e => e.CreatedAtUtc),
        };

        return await employees
            .Select(e => new EmployeeDto(e.Id, e.PartyId, e.PositionCode, e.LaborDepartmentCode, e.BranchCode,
                e.PayrollEnabled, e.BaseSalary, e.CreatedAtUtc))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class GetEmployeeByPartyIdQueryHandler(HrDbContext db)
    : IQueryHandler<GetEmployeeByPartyIdQuery, EmployeeDetailDto>
{
    public async ValueTask<EmployeeDetailDto> Handle(GetEmployeeByPartyIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var e = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PartyId == query.PartyId && !x.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Empleado no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        return new EmployeeDetailDto(e.Id, e.PartyId, e.ToData());
    }
}

public static class GetEmployeesEndpoint
{
    public static RouteHandlerBuilder MapGetEmployeesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetEmployeesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    PagedResponse<EmployeeDto> result = await mediator.Send(query, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetEmployees")
            .WithSummary("List employees")
            .RequirePermission(HrPermissions.Employees.View)
            .Produces<PagedResponse<EmployeeDto>>(StatusCodes.Status200OK);

    public static RouteHandlerBuilder MapGetEmployeeByPartyIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/by-party/{partyId:guid}",
                async (Guid partyId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    EmployeeDetailDto result = await mediator.Send(new GetEmployeeByPartyIdQuery(partyId), cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetEmployeeByPartyId")
            .WithSummary("Get employee by party id")
            .RequirePermission(HrPermissions.Employees.View)
            .Produces<EmployeeDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
}
