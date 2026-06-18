using FluentValidation;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.DeletePhone;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.DeletePhone;

public sealed class DeletePhoneCommandValidator : AbstractValidator<DeletePhoneCommand>
{
    public DeletePhoneCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
