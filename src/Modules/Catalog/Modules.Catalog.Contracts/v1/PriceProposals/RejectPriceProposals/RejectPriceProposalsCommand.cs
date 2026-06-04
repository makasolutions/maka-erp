using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceProposals.RejectPriceProposals;

/// <summary>Rejects all Pending proposals in a batch. Returns the count rejected.</summary>
public sealed record RejectPriceProposalsCommand(Guid BatchId) : ICommand<int>;
