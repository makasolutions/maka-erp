using System.Text.Json;
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.Relationships;

/// <summary>
/// Vínculo M2M persona↔empresa (SPEC §1.1, PR-2). La <see cref="SourcePartyId"/> (persona) puede estar
/// vinculada a N empresas (<see cref="TargetPartyId"/>), cada una con su cargo/función/principal/vigencia.
/// Reemplaza el modelo single-parent (`ContactProfile` 1:1) y la lista denormalizada (`PartyContact` v1)
/// para los datos *por-empresa*. Tenant-scoped (hereda el filtro de <c>BaseDbContext</c>; NO es IGlobalEntity).
///
/// Invariantes:
/// - <see cref="StartDate"/> ≤ <see cref="EndDate"/> (si hay fin).
/// - Máximo un <see cref="IsPrimary"/> activo por <see cref="TargetPartyId"/> (índice parcial único;
///   el handler desmarca el anterior antes de marcar el nuevo).
/// - El cargo/función/rol son **códigos de Tabla Básica** (no enums): ampliables sin tocar código.
/// </summary>
public sealed class PartyRelationship : BaseEntity<Guid>
{
    public Guid    SourcePartyId        { get; private set; }   // la persona (normalmente Natural)
    public Guid    TargetPartyId        { get; private set; }   // la empresa dueña del vínculo
    public string  RelationshipTypeCode { get; private set; } = default!; // Tabla Básica RelationshipType
    public string? ContactFunctionCode  { get; private set; }   // Tabla Básica ContactFunction (DIAN)
    public string? JobTitleCode          { get; private set; }   // Tabla Básica Position ("Cargos laborales")
    public bool    IsPrimary            { get; private set; }
    public bool    IsActive             { get; private set; }
    public DateOnly  StartDate          { get; private set; }
    public DateOnly? EndDate            { get; private set; }

    /// <summary>Valores de custom fields con scope <c>PartyRelationship</c> (PR-1/§3.4) — JSONB por ApiSlug.</summary>
    public JsonDocument? CustomFields    { get; private set; }

    private PartyRelationship() { }

    public static PartyRelationship Create(
        Guid sourcePartyId,
        Guid targetPartyId,
        string relationshipTypeCode,
        string? contactFunctionCode = null,
        string? jobTitleCode = null,
        bool isPrimary = false,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        if (sourcePartyId == Guid.Empty) throw new ArgumentException("SourcePartyId requerido.", nameof(sourcePartyId));
        if (targetPartyId == Guid.Empty) throw new ArgumentException("TargetPartyId requerido.", nameof(targetPartyId));
        if (sourcePartyId == targetPartyId) throw new ArgumentException("Una persona no puede vincularse a sí misma.", nameof(targetPartyId));
        ArgumentException.ThrowIfNullOrWhiteSpace(relationshipTypeCode);

        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (endDate is { } e && e < start) throw new ArgumentException("La fecha de fin no puede ser anterior al inicio.", nameof(endDate));

        return new PartyRelationship
        {
            Id                   = Guid.CreateVersion7(),
            SourcePartyId        = sourcePartyId,
            TargetPartyId        = targetPartyId,
            RelationshipTypeCode = relationshipTypeCode.Trim(),
            ContactFunctionCode  = string.IsNullOrWhiteSpace(contactFunctionCode) ? null : contactFunctionCode.Trim(),
            JobTitleCode         = string.IsNullOrWhiteSpace(jobTitleCode) ? null : jobTitleCode.Trim(),
            IsPrimary            = isPrimary,
            IsActive             = true,
            StartDate            = start,
            EndDate              = endDate,
        };
    }

    public void Update(string relationshipTypeCode, string? contactFunctionCode, string? jobTitleCode,
        DateOnly startDate, DateOnly? endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relationshipTypeCode);
        if (endDate is { } e && e < startDate) throw new ArgumentException("La fecha de fin no puede ser anterior al inicio.", nameof(endDate));
        RelationshipTypeCode = relationshipTypeCode.Trim();
        ContactFunctionCode  = string.IsNullOrWhiteSpace(contactFunctionCode) ? null : contactFunctionCode.Trim();
        JobTitleCode         = string.IsNullOrWhiteSpace(jobTitleCode) ? null : jobTitleCode.Trim();
        StartDate            = startDate;
        EndDate              = endDate;
    }

    /// <summary>Marca/desmarca principal. El caller (handler) garantiza el invariante "uno por empresa"
    /// desmarcando el anterior en la misma transacción (el índice parcial único es la red de seguridad).</summary>
    public void SetPrimary(bool value) => IsPrimary = value;

    /// <summary>Baja lógica del vínculo (no borra la persona ni sus otras relaciones).</summary>
    public void Deactivate(DateOnly? endDate = null)
    {
        IsActive = false;
        IsPrimary = false; // un vínculo inactivo no puede ser el principal
        if (endDate is not null) EndDate = endDate;
    }

    public void SetCustomFields(JsonDocument? values) => CustomFields = values;
}
