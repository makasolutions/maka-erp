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

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.BaseSalary).GreaterThanOrEqualTo(0).When(x => x.Data.BaseSalary.HasValue);
        RuleFor(x => x.Data.MainCurrency).Length(3).When(x => !string.IsNullOrWhiteSpace(x.Data.MainCurrency));
    }
}

public sealed class UpdateEmployeeCommandHandler(HrDbContext db)
    : ICommandHandler<UpdateEmployeeCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Empleado no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        employee.Apply(command.Data);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return employee.Id;
    }
}

public static class UpdateEmployeeEndpoint
{
    public static RouteHandlerBuilder MapUpdateEmployeeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateEmployeeCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdateEmployee")
            .WithSummary("Update employee HR record")
            .RequirePermission(HrPermissions.Employees.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
}
