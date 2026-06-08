using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Tercero (Opción A): un único master que puede ser cliente y/o proveedor, con datos
/// fiscales DIAN. <c>Party.Id</c> es el <c>SupplierId</c> que Catalog ya referencia.
/// Las picklists se guardan como <c>Code</c> de Tabla Básica (no enum).
/// </summary>
public sealed class Party : AggregateRoot<Guid>, ISoftDeletable
{
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

    public PartyRole   Roles  { get; private set; }
    public PartyStatus Status { get; private set; }

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
}
