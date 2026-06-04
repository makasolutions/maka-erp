using FSH.Modules.Catalog.Contracts.v1;
using Mediator;

namespace FSH.Modules.Catalog.Features.v1;

/// <summary>
/// Placeholder handler to satisfy the Mediator source-generator during cleanup.
/// Replaced by real handlers in Fase C2.
/// </summary>
public sealed class GetCatalogHealthQueryHandler : IQueryHandler<GetCatalogHealthQuery, string>
{
    public ValueTask<string> Handle(GetCatalogHealthQuery query, CancellationToken cancellationToken)
        => ValueTask.FromResult("ok");
}
