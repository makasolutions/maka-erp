using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceProposals.ApprovePriceProposals;

/// <summary>Approves all Pending proposals in a batch. Returns the count applied.</summary>
public sealed record ApprovePriceProposalsCommand(Guid BatchId) : ICommand<int>;
