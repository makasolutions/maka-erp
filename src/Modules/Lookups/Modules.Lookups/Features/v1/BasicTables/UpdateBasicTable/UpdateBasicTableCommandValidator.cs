using FluentValidation;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.UpdateBasicTable;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.UpdateBasicTable;

public sealed class UpdateBasicTableCommandValidator : AbstractValidator<UpdateBasicTableCommand>
{
    public UpdateBasicTableCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
