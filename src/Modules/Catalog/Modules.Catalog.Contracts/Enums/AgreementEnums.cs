using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

/// <summary>
/// Tipo de convenio comercial con un proveedor (Fase E — Dropshipping/Convenios).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AgreementType>))]
public enum AgreementType
{
    ProveedorUnico,
    ProveedorModelo,
    DistribuidorAprobado,
    ModeloPendiente,
}

/// <summary>
/// Estado del convenio. <c>Borrador</c> editable/eliminable; <c>Vigente</c> activo;
/// <c>Suspendido</c> pausado; <c>Terminado</c> inmutable (§8/§ACC-6).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AgreementStatus>))]
public enum AgreementStatus
{
    Borrador,
    Vigente,
    Suspendido,
    Terminado,
}

/// <summary>Responsable de una tarea operativa del convenio (despacho/guía/liquidación).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AgreementResponsible>))]
public enum AgreementResponsible
{
    Proveedor,
    Distribuidor,
    Plataforma,
}

/// <summary>
/// Tipo de regla configurable que un distribuidor debe cumplir para acceder al convenio.
/// Las numéricas usan <c>NumericValue</c>; las booleanas <c>BoolValue</c>;
/// <c>DocumentoExigido</c> usa <c>TextValue</c> (código de documento).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AgreementRuleType>))]
public enum AgreementRuleType
{
    CompraMinimaMes,
    CompraMinimaAnio,
    AntiguedadMinimaMeses,
    VendeAEmpresa,
    VendeANatural,
    DocumentoExigido,
    CalificacionMinima,
    SlaEntregaDias,
    CumplimientoMinimo,
}

/// <summary>Resultado de evaluar una regla contra un distribuidor concreto.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RuleEvaluationResult>))]
public enum RuleEvaluationResult
{
    Cumple,
    NoCumple,
    Pendiente,
}
