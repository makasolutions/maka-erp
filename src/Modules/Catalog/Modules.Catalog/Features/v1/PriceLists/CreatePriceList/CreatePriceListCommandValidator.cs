using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.CreatePriceList;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.CreatePriceList;

public sealed class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    internal static readonly string[] ValidSegments =
        ["retail", "wholesale", "vip", "b2b", "dropshipping"];

    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Description)
            .MaximumLength(512)
            .When(x => x.Description is not null);

        RuleFor(x => x.CustomerSegment)
            .NotEmpty()
            .Must(s => ValidSegments.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage($"CustomerSegment debe ser uno de: {string.Join(", ", ValidSegments)}");
    }
}
