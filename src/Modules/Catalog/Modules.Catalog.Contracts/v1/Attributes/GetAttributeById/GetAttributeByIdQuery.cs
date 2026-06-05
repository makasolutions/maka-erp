using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributeById;

public sealed record GetAttributeByIdQuery(Guid Id) : IQuery<AttributeDetailDto>;
