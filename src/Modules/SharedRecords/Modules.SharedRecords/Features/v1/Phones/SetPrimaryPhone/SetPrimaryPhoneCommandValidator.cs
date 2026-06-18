using FluentValidation;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.SetPrimaryPhone;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.SetPrimaryPhone;

public sealed class SetPrimaryPhoneCommandValidator : AbstractValidator<SetPrimaryPhoneCommand>
{
    public SetPrimaryPhoneCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
