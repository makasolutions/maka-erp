using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ProductType>))]
public enum ProductType   { Simple, Variable, Bundle, Service }

[JsonConverter(typeof(JsonStringEnumConverter<ProductStatus>))]
public enum ProductStatus { Draft, Active, Archived }

[JsonConverter(typeof(JsonStringEnumConverter<WeightUnit>))]
public enum WeightUnit    { KG, G, LB, OZ }

[JsonConverter(typeof(JsonStringEnumConverter<DimensionUnit>))]
public enum DimensionUnit { CM, M, IN }
