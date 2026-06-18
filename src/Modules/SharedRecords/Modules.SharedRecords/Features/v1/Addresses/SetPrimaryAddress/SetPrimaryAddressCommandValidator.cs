using FluentValidation;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.SetPrimaryAddress;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.SetPrimaryAddress;

public sealed class SetPrimaryAddressCommandValidator : AbstractValidator<SetPrimaryAddressCommand>
{
    public SetPrimaryAddressCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
