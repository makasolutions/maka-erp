using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<PriceProposalStatus>))]
public enum PriceProposalStatus { Pending, Approved, Rejected }
