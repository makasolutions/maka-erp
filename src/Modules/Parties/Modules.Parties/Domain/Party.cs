using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.V2;
using FSH.Modules.Parties.Domain.V2.Exceptions;
using FSH.Modules.Parties.Domain.V2.Profiles;

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

    // Fiscal DIAN (mínimo; se refina en Billing)
    public string? TaxRegimeCode             { get; private set; }
    public string? FiscalResponsibilities    { get; private set; }
    public string? ActividadEconomicaCiiuCode { get; private set; }

    /// <summary>
    /// Identidad fiscal v2 (owned VO, SPEC §4) — ejes RegimenTributario + ResponsabilidadIVA
    /// SEPARADOS. PR-D1: aditivo y nullable; coexiste con el <see cref="TaxRegimeCode"/> v1 (que
    /// Catalog sigue leyendo) hasta PR-F. Lo puebla el backfill desde TaxRegimeCode (TaxRegimeMapper).
    /// </summary>
    public FiscalData? FiscalData { get; private set; }

    public PartyRole   Roles  { get; private set; }
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

    // Financiera (B2B). Debe/CupoDisponible se calculan con CxC/CxP a futuro.
    public bool     HasCredit      { get; private set; }
    public decimal? CreditLimit    { get; private set; }
    public string?  CreditDaysCode { get; private set; }
    public bool     CreditBlocked  { get; private set; }
    public string?  CreditCurrency { get; private set; }

    public string? Notes    { get; private set; }
    public Guid?   BranchId { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    public bool IsCustomer => Roles.HasFlag(PartyRole.Customer);
    public bool IsSupplier => Roles.HasFlag(PartyRole.Supplier);
    public bool IsEmployee => Roles.HasFlag(PartyRole.Employee);

    public ICollection<PartyAddress>    Addresses { get; private set; } = new List<PartyAddress>();
    public ICollection<PartyContact>    Contacts  { get; private set; } = new List<PartyContact>();
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

    public static Party Create(
        string identificationTypeCode, string identificationNumber, int? verificationDigit,
        PartyKind kind, string legalName, PartyRole roles,
        string? tradeName = null, string? email = null, string? website = null,
        string? taxRegimeCode = null, string? fiscalResponsibilities = null,
        PartyStatus status = PartyStatus.Active, LifecycleStage stage = LifecycleStage.Lead,
        int leadScore = 0, string? sourceCode = null, Guid? assignedUserId = null, string? marketingType = null,
        DateOnly? birthDate = null, string? genderCode = null, string? maritalStatusCode = null,
        decimal? creditLimit = null, string? creditCurrency = null, string? notes = null, Guid? branchId = null,
        string? firstName = null, string? lastName = null, string? actividadEconomicaCiiuCode = null,
        bool hasCredit = false, string? creditDaysCode = null, bool creditBlocked = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);

        return new Party
        {
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            ActividadEconomicaCiiuCode = actividadEconomicaCiiuCode?.Trim(),
            HasCredit = hasCredit,
            CreditDaysCode = creditDaysCode?.Trim(),
            CreditBlocked = creditBlocked,
            Id = Guid.CreateVersion7(),
            IdentificationTypeCode = identificationTypeCode.Trim(),
            IdentificationNumber = identificationNumber.Trim(),
            VerificationDigit = verificationDigit,
            Kind = kind,
            LegalName = legalName.Trim(),
            TradeName = tradeName?.Trim(),
            Email = email?.Trim(),
            Website = website?.Trim(),
            TaxRegimeCode = taxRegimeCode?.Trim(),
            FiscalResponsibilities = fiscalResponsibilities?.Trim(),
            Roles = roles,
            Status = status,
            Stage = stage,
            LeadScore = leadScore,
            SourceCode = sourceCode?.Trim(),
            AssignedUserId = assignedUserId,
            MarketingType = marketingType?.Trim(),
            BirthDate = birthDate,
            GenderCode = genderCode?.Trim(),
            MaritalStatusCode = maritalStatusCode?.Trim(),
            CreditLimit = creditLimit,
            CreditCurrency = creditCurrency?.Trim(),
            Notes = notes?.Trim(),
            BranchId = branchId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(
        PartyKind kind, string legalName, PartyRole roles, string? tradeName, string? email, string? website,
        string? taxRegimeCode, string? fiscalResponsibilities, PartyStatus status, LifecycleStage stage,
        int leadScore, string? sourceCode, Guid? assignedUserId, string? marketingType,
        DateOnly? birthDate, string? genderCode, string? maritalStatusCode,
        decimal? creditLimit, string? creditCurrency, string? notes, Guid? branchId, int? verificationDigit,
        string? firstName, string? lastName, string? actividadEconomicaCiiuCode,
        bool hasCredit, string? creditDaysCode, bool creditBlocked)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        Kind = kind;
        LegalName = legalName.Trim();
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        ActividadEconomicaCiiuCode = actividadEconomicaCiiuCode?.Trim();
        HasCredit = hasCredit;
        CreditDaysCode = creditDaysCode?.Trim();
        CreditBlocked = creditBlocked;
        Roles = roles;
        TradeName = tradeName?.Trim();
        Email = email?.Trim();
        Website = website?.Trim();
        TaxRegimeCode = taxRegimeCode?.Trim();
        FiscalResponsibilities = fiscalResponsibilities?.Trim();
        Status = status;
        Stage = stage;
        LeadScore = leadScore;
        SourceCode = sourceCode?.Trim();
        AssignedUserId = assignedUserId;
        MarketingType = marketingType?.Trim();
        BirthDate = birthDate;
        GenderCode = genderCode?.Trim();
        MaritalStatusCode = maritalStatusCode?.Trim();
        CreditLimit = creditLimit;
        CreditCurrency = creditCurrency?.Trim();
        Notes = notes?.Trim();
        BranchId = branchId;
        VerificationDigit = verificationDigit;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRoles(PartyRole roles) { Roles = roles; UpdatedAtUtc = DateTime.UtcNow; }

    /// <summary>
    /// Asigna la identidad fiscal v2 (owned VO). PR-D1: usado por el backfill para persistir el
    /// régimen derivado del <see cref="TaxRegimeCode"/> v1. Reemplaza el VO completo — el caller
    /// decide la idempotencia (no sobreescribir si ya hay datos fiscales).
    /// </summary>
    public void AssignFiscalData(FiscalData fiscalData)
    {
        ArgumentNullException.ThrowIfNull(fiscalData);
        FiscalData = fiscalData;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetGlobalSupplier(bool isGlobalSupplier)
    {
        IsGlobalSupplier = isGlobalSupplier;
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

    public void ReplaceContacts(IEnumerable<PartyContact> items)
    {
        Contacts.Clear();
        foreach (var c in items) Contacts.Add(c);
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
