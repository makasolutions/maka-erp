using System.Text.Json;
using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.Exceptions;
using FSH.Modules.Parties.Domain.Profiles;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Tercero (Opción A): un único master que puede ser cliente y/o proveedor, con datos
/// fiscales DIAN. <c>Party.Id</c> es el <c>SupplierId</c> que Catalog ya referencia.
/// Las picklists se guardan como <c>Code</c> de Tabla Básica (no enum).
/// </summary>
public sealed class Party : AggregateRoot<Guid>, ISoftDeletable
{
    /// <summary>
    /// Jerarquía comercial (matriz → sucursal → contacto), patrón Odoo <c>parent_id</c> (PR-D3).
    /// Eje ORTOGONAL a <c>TenantId</c> (R4): la jerarquía es del tercero, el tenant es el SaaS.
    /// Permite delegar campos comerciales al padre (ver <see cref="ResolveCommercialEntity"/>).
    /// </summary>
    public Guid? ParentPartyId { get; private set; }

    // Identificación
    public string  IdentificationTypeCode { get; private set; } = default!;
    public string  IdentificationNumber   { get; private set; } = default!;
    public int?    VerificationDigit      { get; private set; }
    public PartyKind Kind                 { get; private set; }

    public string  LegalName  { get; private set; } = default!;  // razón social (jurídica) o "Nombres Apellidos" (natural)
    public string? FirstName  { get; private set; }               // persona natural
    public string? LastName   { get; private set; }               // persona natural
    public string? TradeName  { get; private set; }
    public string? Email      { get; private set; }
    public string? Website    { get; private set; }

    // Fiscal DIAN v1 (TaxRegimeCode/FiscalResponsibilities/ActividadEconomicaCiiuCode) REMOVIDO en
    // PR-F1b → ahora vive en FiscalData (ejes) + PartyCiiuActivity. El DTO reconstruye los campos
    // v1 desde v2 (output computado, contrato del wizard estable).

    /// <summary>
    /// Identidad fiscal v2 (owned VO, SPEC §4) — ejes RegimenTributario + ResponsabilidadIVA
    /// SEPARADOS. PR-F1b: ÚNICA fuente fiscal (el <c>TaxRegimeCode</c> string v1 fue removido). El
    /// synchronizer la escribe desde el input del comando (vía TaxRegimeMapper) y el DTO la reexpone.
    /// </summary>
    public FiscalData? FiscalData { get; private set; }

    /// <summary>
    /// Representante legal (owned VO nullable, SPEC §5) — personas jurídicas. PR-D4: aditivo y
    /// nullable (arranca null; v1 no tiene datos de rep. legal que backfillear). Usa el enum
    /// <c>TipoIdentificacion</c> v2; la reconciliación con el código v1 está en <c>IdentificationTypeMapper</c>.
    /// </summary>
    public LegalRepresentative? LegalRepresentative { get; private set; }

    // Roles (PartyRole flags) REMOVIDO en PR-F1b → la fuente de verdad de rol son las facetas v2
    // (CustomerProfile/SupplierProfile/EmployeeProfile activas). El DTO computa Roles desde ellas.
    public PartyStatus Status { get; private set; }

    /// <summary>Proveedor publicado al marketplace global (visible a todos los tenants).</summary>
    public bool IsGlobalSupplier { get; private set; }

    // CRM
    public LifecycleStage Stage         { get; private set; }
    public int            LeadScore     { get; private set; }
    public string?        SourceCode    { get; private set; }
    public Guid?          AssignedUserId { get; private set; }
    public string?        MarketingType { get; private set; }

    // Persona natural
    public DateOnly? BirthDate        { get; private set; }
    public string?   GenderCode       { get; private set; }
    public string?   MaritalStatusCode { get; private set; }

    /// <summary>
    /// Persona Expuesta Políticamente (compliance) — atributo de PERSONA natural (PR-2). Se marca UNA vez
    /// en la identidad; las relaciones (p. ej. rep. legal de una empresa) lo EXPONEN, no lo duplican.
    /// Reemplaza el <c>FiscalData.FlagPEP</c> (deprecado, era a nivel empresa).
    /// </summary>
    public bool      IsPEP            { get; private set; }
    public string?   PepType          { get; private set; }   // tipo/cargo PEP (opcional)

    // Financiera v1 (HasCredit/CreditLimit/CreditDaysCode/CreditBlocked/CreditCurrency) REMOVIDA en
    // PR-F1b → vive en CreditAccount + PartyHold(Ventas). El DTO la reconstruye desde ahí.

    public string? Notes    { get; private set; }
    public Guid?   BranchId { get; private set; }

    /// <summary>
    /// Valores de custom fields con scope <c>Party</c> (PR-1, SPEC §3.3) — JSONB keyado por
    /// <c>CustomFieldDefinition.ApiSlug</c>. El ESQUEMA vive en las definiciones; aquí solo los
    /// valores. Null = sin valores. La validación (tipo + completitud gobernada) la hace el caller
    /// con <c>CustomFieldValues</c> (el cableado de escritura llega en PR-3).
    /// </summary>
    public JsonDocument? CustomFields { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    public ICollection<PartyAddress>    Addresses { get; private set; } = new List<PartyAddress>();
    public ICollection<PartyChannel>    Channels  { get; private set; } = new List<PartyChannel>();
    public ICollection<PartyTeamMember> Team      { get; private set; } = new List<PartyTeamMember>();

    /// <summary>
    /// Facetas comerciales v2 (PR-D2). Navegaciones uno-a-cero-o-uno: un tercero tiene a lo sumo
    /// una de cada faceta. De lectura por ahora (EF las puebla con Include); el dual-write llega en
    /// D5. La cuenta de crédito no es navegación del tercero — se alcanza a través de la faceta cliente.
    /// </summary>
    public CustomerProfile? CustomerProfile { get; private set; }
    public SupplierProfile? SupplierProfile { get; private set; }
    public ContactProfile?  ContactProfile  { get; private set; }
    public PartnerProfile?  PartnerProfile  { get; private set; }
    public EmployeeProfile? EmployeeProfile { get; private set; }

    /// <summary>Actividades CIIU del tercero (1..N, exactamente una principal — invariante del agregado, PR-D2).</summary>
    public ICollection<PartyCiiuActivity> CiiuActivities { get; private set; } = new List<PartyCiiuActivity>();

    private Party() { }

    // PR-F1b: los campos v1 (roles/fiscal/crédito) salieron de la firma. Quien los necesite los
    // pasa por el comando → PartyV2WriteInput → el synchronizer escribe el modelo v2.
    public static Party Create(
        string identificationTypeCode, string identificationNumber, int? verificationDigit,
        PartyKind kind, string legalName,
        string? tradeName = null, string? email = null, string? website = null,
        PartyStatus status = PartyStatus.Active, LifecycleStage stage = LifecycleStage.Lead,
        int leadScore = 0, string? sourceCode = null, Guid? assignedUserId = null, string? marketingType = null,
        DateOnly? birthDate = null, string? genderCode = null, string? maritalStatusCode = null,
        string? notes = null, Guid? branchId = null,
        string? firstName = null, string? lastName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);

        return new Party
        {
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            Id = Guid.CreateVersion7(),
            IdentificationTypeCode = identificationTypeCode.Trim(),
            IdentificationNumber = identificationNumber.Trim(),
            VerificationDigit = verificationDigit,
            Kind = kind,
            LegalName = legalName.Trim(),
            TradeName = tradeName?.Trim(),
            Email = email?.Trim(),
            Website = website?.Trim(),
            Status = status,
            Stage = stage,
            LeadScore = leadScore,
            SourceCode = sourceCode?.Trim(),
            AssignedUserId = assignedUserId,
            MarketingType = marketingType?.Trim(),
            BirthDate = birthDate,
            GenderCode = genderCode?.Trim(),
            MaritalStatusCode = maritalStatusCode?.Trim(),
            Notes = notes?.Trim(),
            BranchId = branchId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    // PR-F1b: los campos v1 (roles/fiscal/crédito) salieron de la firma — el synchronizer los
    // escribe en v2 desde el PartyV2WriteInput del comando.
    public void Update(
        PartyKind kind, string legalName, string? tradeName, string? email, string? website,
        PartyStatus status, LifecycleStage stage,
        int leadScore, string? sourceCode, Guid? assignedUserId, string? marketingType,
        DateOnly? birthDate, string? genderCode, string? maritalStatusCode,
        string? notes, Guid? branchId, int? verificationDigit,
        string? firstName, string? lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        Kind = kind;
        LegalName = legalName.Trim();
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        TradeName = tradeName?.Trim();
        Email = email?.Trim();
        Website = website?.Trim();
        Status = status;
        Stage = stage;
        LeadScore = leadScore;
        SourceCode = sourceCode?.Trim();
        AssignedUserId = assignedUserId;
        MarketingType = marketingType?.Trim();
        BirthDate = birthDate;
        GenderCode = genderCode?.Trim();
        MaritalStatusCode = maritalStatusCode?.Trim();
        Notes = notes?.Trim();
        BranchId = branchId;
        VerificationDigit = verificationDigit;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Asigna la identidad fiscal v2 (owned VO). Lo usa el synchronizer para persistir el régimen
    /// derivado del input del comando (vía TaxRegimeMapper). Reemplaza el VO completo — el caller
    /// decide la idempotencia (no sobreescribir si ya hay datos fiscales).
    /// </summary>
    public void AssignFiscalData(FiscalData fiscalData)
    {
        ArgumentNullException.ThrowIfNull(fiscalData);
        FiscalData = fiscalData;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Asigna (o reemplaza) el representante legal v2 (owned VO). PR-D4.</summary>
    public void AssignLegalRepresentative(LegalRepresentative legalRepresentative)
    {
        ArgumentNullException.ThrowIfNull(legalRepresentative);
        LegalRepresentative = legalRepresentative;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetGlobalSupplier(bool isGlobalSupplier)
    {
        IsGlobalSupplier = isGlobalSupplier;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Reemplaza los valores de custom fields (scope Party). La validación contra las
    /// definiciones activas (tipo + completitud gobernada) la realiza el caller con
    /// <c>CustomFieldValues</c> antes de invocar esto. PR-1 (cableado de escritura: PR-3).</summary>
    public void SetCustomFields(JsonDocument? values)
    {
        CustomFields = values;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Marca/desmarca la persona como PEP (compliance, PR-2). El <paramref name="pepType"/> es
    /// opcional; se ignora si <paramref name="isPep"/> es false.</summary>
    public void SetPep(bool isPep, string? pepType = null)
    {
        IsPEP = isPep;
        PepType = isPep ? pepType?.Trim() : null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false; DeletedOnUtc = null; DeletedBy = null; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceAddresses(IEnumerable<PartyAddress> items)
    {
        Addresses.Clear();
        var list = items.ToList();
        if (list.Count > 0 && !list.Any(a => a.IsPrimary))
        {
            // garantizar una dirección primaria
            var first = list[0];
            list[0] = PartyAddress.Create(first.Country, first.Department, first.City, first.Line,
                first.Barrio, first.Reference, first.Latitude, first.Longitude, isPrimary: true, first.LabelCode);
        }
        foreach (var a in list) Addresses.Add(a);
    }

    public void ReplaceChannels(IEnumerable<PartyChannel> items)
    {
        Channels.Clear();
        foreach (var c in items) Channels.Add(c);
    }

    public void ReplaceTeam(IEnumerable<PartyTeamMember> items)
    {
        Team.Clear();
        foreach (var m in items) Team.Add(m);
    }

    // ── CIIU: invariante "exactamente una principal" en el agregado (PR-D2, antes en el
    //    helper transitorio CiiuActivities.SetPrincipal de PR-A). ──

    /// <summary>Agrega una actividad CIIU. Si <paramref name="isPrincipal"/>, desmarca las demás.</summary>
    public PartyCiiuActivity AddCiiuActivity(string ciiuCode, bool isPrincipal = false)
    {
        if (isPrincipal)
        {
            foreach (var a in CiiuActivities) a.SetPrincipal(false);
        }
        var activity = PartyCiiuActivity.Create(Id, ciiuCode, isPrincipal);
        CiiuActivities.Add(activity);
        UpdatedAtUtc = DateTime.UtcNow;
        return activity;
    }

    /// <summary>
    /// Marca una actividad como principal y desmarca las demás (mantiene exactamente una
    /// principal). Lanza si el id no pertenece a la colección del tercero.
    /// </summary>
    public void SetPrincipalCiiu(Guid ciiuActivityId)
    {
        var target = CiiuActivities.FirstOrDefault(a => a.Id == ciiuActivityId)
            ?? throw new ArgumentException("La actividad CIIU no pertenece a este tercero.", nameof(ciiuActivityId));
        foreach (var a in CiiuActivities) a.SetPrincipal(false);
        target.SetPrincipal(true);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // ── Jerarquía (PR-D3): patrón de validación de ciclos REUTILIZABLE para otras jerarquías
    //    del sistema (categorías de catálogo, centros de costo, etc.): el dominio valida en
    //    memoria contra el conjunto de ids de ancestros que el caller carga con un recursive CTE
    //    (una query, solo ids). El dominio no hace I/O; la carga es eficiente y acotada. ──

    /// <summary>
    /// Asigna el padre del tercero validando que NO se forme un ciclo. <paramref name="parentAncestorIds"/>
    /// son los ids de los ancestros del padre propuesto (cargados por el caller vía recursive CTE);
    /// si este tercero está entre ellos, o el padre es él mismo, la asignación crearía un ciclo y lanza.
    /// </summary>
    public void AssignParent(Guid parentId, IReadOnlySet<Guid> parentAncestorIds)
    {
        ArgumentNullException.ThrowIfNull(parentAncestorIds);
        if (parentId == Guid.Empty) throw new ArgumentException("ParentId requerido.", nameof(parentId));
        if (parentId == Id || parentAncestorIds.Contains(Id))
        {
            throw new PartyHierarchyCycleException(Id, parentId);
        }
        ParentPartyId = parentId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Quita el padre (el tercero pasa a ser raíz de su jerarquía).</summary>
    public void ClearParent()
    {
        ParentPartyId = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Resuelve la "entidad comercial" de la cual se leen los campos comerciales (FiscalData y, vía
    /// su <c>CustomerProfile</c>, el CreditAccount) — patrón Odoo <c>commercial_partner_id</c>. La
    /// entidad comercial es UNA unidad: los campos vienen todos de la matriz o todos del propio
    /// tercero, no combinados. Devuelve <c>this</c> si tiene datos comerciales propios; si no, el
    /// ancestro más cercano que los tenga; si ninguno, la matriz (último ancestro).
    /// <paramref name="ancestorsNearestFirst"/> es la cadena de ancestros (padre, abuelo, …) ya cargada.
    /// </summary>
    public Party ResolveCommercialEntity(IReadOnlyList<Party> ancestorsNearestFirst)
    {
        ArgumentNullException.ThrowIfNull(ancestorsNearestFirst);
        if (HasOwnCommercialData) return this;
        foreach (var ancestor in ancestorsNearestFirst)
        {
            if (ancestor.HasOwnCommercialData) return ancestor;
        }
        return ancestorsNearestFirst.Count > 0 ? ancestorsNearestFirst[^1] : this;
    }

    /// <summary>
    /// Criterio de "tiene datos comerciales propios" para la delegación. TODO(D5): refinar si
    /// aparece un caso con crédito propio pero sin FiscalData (hoy se asume que la FiscalData es el
    /// indicador de entidad comercial). Para M1 con jerarquías simples (matriz→sucursal→contacto) basta.
    /// </summary>
    private bool HasOwnCommercialData => FiscalData?.RegimenTributario is not null;
}
