using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.TenantProducts.CloneProduct;

/// <summary>Clones a canonical product into the current tenant's catalog (spec §5.7).</summary>
public sealed record CloneProductToTenantCommand(Guid CanonicalProductId) : ICommand<Guid>;
