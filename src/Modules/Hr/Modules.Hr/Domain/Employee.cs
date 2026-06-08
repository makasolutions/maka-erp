using FSH.Framework.Core.Domain;

namespace FSH.Modules.Hr.Domain;

/// <summary>
/// Información laboral / de nómina de un empleado. Relación 1:1 con un <c>Party</c>
/// (rol Employee) por <see cref="PartyId"/>. Base del futuro módulo Nómina.
/// Todas las picklists se guardan como <c>Code</c> de Tabla Básica (no enum).
/// </summary>
public sealed class Employee : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid PartyId { get; private set; }

    // Datos básicos
    public string? MainCurrency        { get; private set; }
    public bool    IsVendedor          { get; private set; }
    public bool    IsCobrador          { get; private set; }
    public string? BranchCode          { get; private set; }
    public string? CostCenterCode      { get; private set; }
    public DateOnly? SeniorityDate     { get; private set; }
    public string? Notes               { get; private set; }
    public bool    PayrollEnabled      { get; private set; }
    public string? HealthProviderCode  { get; private set; }  // EPS
    public string? PensionFundCode     { get; private set; }  // AFP
    public string? SeveranceFundCode   { get; private set; }  // Cesantías
    public string? CcfCode             { get; private set; }  // Caja de compensación
    public string? ArlProviderCode     { get; private set; }
    public string? PaymentMethodCode   { get; private set; }

    // Cargo
    public string?   LaborDepartmentCode { get; private set; }
    public string?   PositionCode        { get; private set; }
    public Guid?     ImmediateBossPartyId { get; private set; }
    public DateOnly? PositionStartDate   { get; private set; }

    // Contrato laboral
    public string?   ContractTypeCode     { get; private set; }
    public string?   ContractDurationCode { get; private set; }
    public DateOnly? ContractStartDate    { get; private set; }
    public string?   ArlRiskLevelCode     { get; private set; }
    public bool      HighPensionRisk      { get; private set; }
    public string?   RestDays             { get; private set; }  // ej. "SAB,DOM"
    public bool      AppliesLaw1607       { get; private set; }

    // Salario
    public string?   SalaryTypeCode          { get; private set; }
    public decimal?  BaseSalary              { get; private set; }
    public DateOnly? SalaryStartDate         { get; private set; }
    public bool      LegalTransportAllowance { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    private Employee() { }

    public static Employee Create(
        Guid partyId,
        string? mainCurrency, bool isVendedor, bool isCobrador, string? branchCode, string? costCenterCode,
        DateOnly? seniorityDate, string? notes, bool payrollEnabled, string? healthProviderCode, string? pensionFundCode,
        string? severanceFundCode, string? ccfCode, string? arlProviderCode, string? paymentMethodCode,
        string? laborDepartmentCode, string? positionCode, Guid? immediateBossPartyId, DateOnly? positionStartDate,
        string? contractTypeCode, string? contractDurationCode, DateOnly? contractStartDate, string? arlRiskLevelCode,
        bool highPensionRisk, string? restDays, bool appliesLaw1607,
        string? salaryTypeCode, decimal? baseSalary, DateOnly? salaryStartDate, bool legalTransportAllowance)
    {
        var e = new Employee { Id = Guid.CreateVersion7(), PartyId = partyId, CreatedAtUtc = DateTime.UtcNow };
        e.Apply(mainCurrency, isVendedor, isCobrador, branchCode, costCenterCode, seniorityDate, notes, payrollEnabled,
            healthProviderCode, pensionFundCode, severanceFundCode, ccfCode, arlProviderCode, paymentMethodCode,
            laborDepartmentCode, positionCode, immediateBossPartyId, positionStartDate, contractTypeCode,
            contractDurationCode, contractStartDate, arlRiskLevelCode, highPensionRisk, restDays, appliesLaw1607,
            salaryTypeCode, baseSalary, salaryStartDate, legalTransportAllowance);
        return e;
    }

    public void Update(
        string? mainCurrency, bool isVendedor, bool isCobrador, string? branchCode, string? costCenterCode,
        DateOnly? seniorityDate, string? notes, bool payrollEnabled, string? healthProviderCode, string? pensionFundCode,
        string? severanceFundCode, string? ccfCode, string? arlProviderCode, string? paymentMethodCode,
        string? laborDepartmentCode, string? positionCode, Guid? immediateBossPartyId, DateOnly? positionStartDate,
        string? contractTypeCode, string? contractDurationCode, DateOnly? contractStartDate, string? arlRiskLevelCode,
        bool highPensionRisk, string? restDays, bool appliesLaw1607,
        string? salaryTypeCode, decimal? baseSalary, DateOnly? salaryStartDate, bool legalTransportAllowance)
    {
        Apply(mainCurrency, isVendedor, isCobrador, branchCode, costCenterCode, seniorityDate, notes, payrollEnabled,
            healthProviderCode, pensionFundCode, severanceFundCode, ccfCode, arlProviderCode, paymentMethodCode,
            laborDepartmentCode, positionCode, immediateBossPartyId, positionStartDate, contractTypeCode,
            contractDurationCode, contractStartDate, arlRiskLevelCode, highPensionRisk, restDays, appliesLaw1607,
            salaryTypeCode, baseSalary, salaryStartDate, legalTransportAllowance);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void Apply(
        string? mainCurrency, bool isVendedor, bool isCobrador, string? branchCode, string? costCenterCode,
        DateOnly? seniorityDate, string? notes, bool payrollEnabled, string? healthProviderCode, string? pensionFundCode,
        string? severanceFundCode, string? ccfCode, string? arlProviderCode, string? paymentMethodCode,
        string? laborDepartmentCode, string? positionCode, Guid? immediateBossPartyId, DateOnly? positionStartDate,
        string? contractTypeCode, string? contractDurationCode, DateOnly? contractStartDate, string? arlRiskLevelCode,
        bool highPensionRisk, string? restDays, bool appliesLaw1607,
        string? salaryTypeCode, decimal? baseSalary, DateOnly? salaryStartDate, bool legalTransportAllowance)
    {
        MainCurrency = mainCurrency; IsVendedor = isVendedor; IsCobrador = isCobrador; BranchCode = branchCode;
        CostCenterCode = costCenterCode; SeniorityDate = seniorityDate; Notes = notes; PayrollEnabled = payrollEnabled;
        HealthProviderCode = healthProviderCode; PensionFundCode = pensionFundCode; SeveranceFundCode = severanceFundCode;
        CcfCode = ccfCode; ArlProviderCode = arlProviderCode; PaymentMethodCode = paymentMethodCode;
        LaborDepartmentCode = laborDepartmentCode; PositionCode = positionCode; ImmediateBossPartyId = immediateBossPartyId;
        PositionStartDate = positionStartDate; ContractTypeCode = contractTypeCode; ContractDurationCode = contractDurationCode;
        ContractStartDate = contractStartDate; ArlRiskLevelCode = arlRiskLevelCode; HighPensionRisk = highPensionRisk;
        RestDays = restDays; AppliesLaw1607 = appliesLaw1607; SalaryTypeCode = salaryTypeCode; BaseSalary = baseSalary;
        SalaryStartDate = salaryStartDate; LegalTransportAllowance = legalTransportAllowance;
    }

    public void SoftDelete(string? by) { IsDeleted = true; DeletedOnUtc = DateTimeOffset.UtcNow; DeletedBy = by; }
}
