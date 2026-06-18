using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Phones.DeletePhone;

public sealed record DeletePhoneCommand(Guid Id) : ICommand;
