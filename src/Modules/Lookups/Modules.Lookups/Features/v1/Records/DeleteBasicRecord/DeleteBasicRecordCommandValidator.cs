using FluentValidation;
using FSH.Modules.Lookups.Contracts.v1.Records.DeleteBasicRecord;

namespace FSH.Modules.Lookups.Features.v1.Records.DeleteBasicRecord;

public sealed class DeleteBasicRecordCommandValidator : AbstractValidator<DeleteBasicRecordCommand>
{
    public DeleteBasicRecordCommandValidator()
    {
        RuleFor(x => x.TableId).NotEmpty();
        RuleFor(x => x.RecordId).NotEmpty();
    }
}
