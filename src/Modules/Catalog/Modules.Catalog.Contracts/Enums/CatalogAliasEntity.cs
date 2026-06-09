using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

/// <summary>Tipo de objeto del catálogo global al que apunta un alias/sinónimo.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatalogAliasEntity>))]
public enum CatalogAliasEntity
{
    Category,
    Brand,
}
