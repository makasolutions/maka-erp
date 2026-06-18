using FluentValidation;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.DeleteAddress;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.DeleteAddress;

public sealed class DeleteAddressCommandValidator : AbstractValidator<DeleteAddressCommand>
{
    public DeleteAddressCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
