using FluentValidation;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.CreateBasicTable;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.CreateBasicTable;

public sealed class CreateBasicTableCommandValidator : AbstractValidator<CreateBasicTableCommand>
{
    public CreateBasicTableCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(64)
            .Matches(@"^[A-Za-z][A-Za-z0-9_]*$")
            .WithMessage("El código solo puede contener letras, números y guion bajo, e iniciar con letra.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
