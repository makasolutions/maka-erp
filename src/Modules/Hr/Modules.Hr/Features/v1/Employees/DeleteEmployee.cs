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

public sealed class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
{
    public DeleteEmployeeCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeleteEmployeeCommandHandler(HrDbContext db)
    : ICommandHandler<DeleteEmployeeCommand>
{
    public async ValueTask<Unit> Handle(DeleteEmployeeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Empleado no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        db.Employees.Remove(employee); // soft-delete via auditing interceptor
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class DeleteEmployeeEndpoint
{
    public static RouteHandlerBuilder MapDeleteEmployeeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteEmployeeCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteEmployee")
            .WithSummary("Delete employee HR record")
            .RequirePermission(HrPermissions.Employees.Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
