using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.RemoveProductCode;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.RemoveProductCode;

public sealed class RemoveProductCodeCommandHandler(CatalogDbContext db)
    : ICommandHandler<RemoveProductCodeCommand, Guid>
{
    public async ValueTask<Guid> Handle(RemoveProductCodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = await db.ProductCodes
            .Where(c => c.ProductId == command.ProductId
                     && c.VariationId == command.VariationId
                     && c.Id == command.CodeId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Code {command.CodeId} not found.");

        db.ProductCodes.Remove(code);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return code.Id;
    }
}
