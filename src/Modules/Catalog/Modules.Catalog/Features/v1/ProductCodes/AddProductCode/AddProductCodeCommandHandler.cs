using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.AddProductCode;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.AddProductCode;

public sealed class AddProductCodeCommandHandler(CatalogDbContext db)
    : ICommandHandler<AddProductCodeCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddProductCodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Verify variation belongs to product
        bool variationExists = await db.Variations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(v => v.ProductId == command.ProductId && v.Id == command.VariationId)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!variationExists)
            throw new NotFoundException($"Variation {command.VariationId} not found for product {command.ProductId}.");

        // Check uniqueness: (VariationId + CodeType + Code) must be unique — spec §2.9
        bool codeExists = await db.ProductCodes
            .AsNoTracking()
            .Where(c => c.VariationId == command.VariationId
                     && c.CodeType == command.CodeType.Trim()
                     && c.Code == command.Code.Trim())
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (codeExists)
            throw new CustomException(
                $"Ya existe un código '{command.Code}' de tipo '{command.CodeType}' para esta variación.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        var code = ProductCode.Create(
            command.ProductId,
            command.VariationId,
            command.CodeType,
            command.Code,
            command.SupplierId,
            command.IsPrimary);

        db.ProductCodes.Add(code);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return code.Id;
    }
}
