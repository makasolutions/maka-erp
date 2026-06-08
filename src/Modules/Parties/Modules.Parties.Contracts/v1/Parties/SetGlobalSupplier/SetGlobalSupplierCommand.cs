using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.SetGlobalSupplier;

/// <summary>Activa/desactiva un tercero como proveedor global (marketplace).</summary>
public sealed record SetGlobalSupplierCommand(Guid Id, bool IsGlobalSupplier) : ICommand;
