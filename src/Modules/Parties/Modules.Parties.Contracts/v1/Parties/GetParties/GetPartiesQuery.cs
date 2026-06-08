using FSH.Framework.Shared.Persistence;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.GetParties;

public sealed record GetPartiesQuery : IPagedQuery, IQuery<PagedResponse<PartyDto>>
{
    public int?            PageNumber { get; set; } = 1;
    public int?            PageSize   { get; set; } = 50;
    public string?         Sort       { get; set; }
    public string?         Search     { get; set; }   // LegalName / IdentificationNumber / TradeName
    public PartyRole?      Role       { get; set; }   // filtra por bit (Customer/Supplier)
    public PartyStatus?    Status     { get; set; }
    public LifecycleStage? Stage      { get; set; }
    public string?         City       { get; set; }
    public Guid?           AssignedUserId { get; set; }
}
