using FluentValidation;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.DeleteBasicTable;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.DeleteBasicTable;

public sealed class DeleteBasicTableCommandValidator : AbstractValidator<DeleteBasicTableCommand>
{
    public DeleteBasicTableCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
