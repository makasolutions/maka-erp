using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties.SetGlobalSupplier;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.SetGlobalSupplier;

public sealed class SetGlobalSupplierCommandValidator : AbstractValidator<SetGlobalSupplierCommand>
{
    public SetGlobalSupplierCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class SetGlobalSupplierCommandHandler(PartiesDbContext db)
    : ICommandHandler<SetGlobalSupplierCommand>
{
    public async ValueTask<Unit> Handle(SetGlobalSupplierCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var party = await db.Parties.FirstOrDefaultAsync(p => p.Id == command.Id && !p.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        party.SetGlobalSupplier(command.IsGlobalSupplier);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class SetGlobalSupplierEndpoint
{
    public static RouteHandlerBuilder MapSetGlobalSupplierEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/global-supplier",
                async (Guid id, SetGlobalSupplierCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SetGlobalSupplier")
            .WithSummary("Activate/deactivate a party as global supplier")
            .RequirePermission(PartiesPermissions.Parties.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
