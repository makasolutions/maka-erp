using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Hr.Contracts.Authorization;
using FSH.Modules.Hr.Contracts.v1.Employees;
using FSH.Modules.Hr.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Hr.Features.v1.Employees;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.BaseSalary).GreaterThanOrEqualTo(0).When(x => x.Data.BaseSalary.HasValue);
        RuleFor(x => x.Data.MainCurrency).Length(3).When(x => !string.IsNullOrWhiteSpace(x.Data.MainCurrency));
    }
}

public sealed class CreateEmployeeCommandHandler(HrDbContext db)
    : ICommandHandler<CreateEmployeeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool exists = await db.Employees.AsNoTracking()
            .AnyAsync(e => !e.IsDeleted && e.PartyId == command.PartyId, cancellationToken).ConfigureAwait(false);
        if (exists)
            throw new CustomException("Ese tercero ya tiene información de empleado.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var employee = EmployeeMapping.ToEntity(command.PartyId, command.Data);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return employee.Id;
    }
}

public static class CreateEmployeeEndpoint
{
    public static RouteHandlerBuilder MapCreateEmployeeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateEmployeeCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/hr/employees/{id}", id);
                })
            .WithName("CreateEmployee")
            .WithSummary("Create employee HR record")
            .RequirePermission(HrPermissions.Employees.Create)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
}
