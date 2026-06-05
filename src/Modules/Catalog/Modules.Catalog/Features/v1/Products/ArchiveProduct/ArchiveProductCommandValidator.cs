using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.ArchiveProduct;

namespace FSH.Modules.Catalog.Features.v1.Products.ArchiveProduct;

public sealed class ArchiveProductCommandValidator : AbstractValidator<ArchiveProductCommand>
{
    public ArchiveProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
