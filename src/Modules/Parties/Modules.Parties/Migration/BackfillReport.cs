using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace FSH.Modules.Parties.Migration;

/// <summary>
/// Reporte auditable del backfill v1→v2 (PR-C). Acumula creados/saltados por tabla, la deuda de
/// reconciliación fiscal (TaxRegimeCode no derivable → PR-D), y las anomalías (crédito sin faceta
/// cliente, proveedores sin moneda). Mutable durante la corrida; se imprime al final.
/// </summary>
public sealed class BackfillReport
{
    public bool DryRun { get; init; }
    public int PartiesScanned { get; set; }

    public int CustomerProfilesCreated { get; set; }
    public int CustomerProfilesSkipped { get; set; }
    public int SupplierProfilesCreated { get; set; }
    public int SupplierProfilesSkipped { get; set; }
    public int EmployeeProfilesCreated { get; set; }
    public int EmployeeProfilesSkipped { get; set; }

    public int CreditAccountsCreated { get; set; }
    public int CreditAccountsSkipped { get; set; }

    public int CiiuActivitiesCreated { get; set; }
    public int CiiuActivitiesSkipped { get; set; }

    public int HoldsCreated { get; set; }
    public int HoldsSkipped { get; set; }

    public int SuppliersWithoutCurrency { get; set; }

    public int TaxRegimeMappedClean { get; set; }
    public int TaxRegimeEmpty { get; set; }

    /// <summary>FiscalData (régimen+IVA) persistidos en Party desde TaxRegimeCode (PR-D1).</summary>
    public int FiscalDataPopulated { get; set; }

    /// <summary>PartyIds cuyo TaxRegimeCode NO mapeó limpio — deuda de reconciliación que hereda PR-D.</summary>
    public Collection<Guid> TaxRegimeUnmapped { get; } = [];

    /// <summary>PartyIds con CreditLimit&gt;0 pero SIN rol Customer — anomalía: crédito sin faceta cliente.</summary>
    public Collection<Guid> CreditWithoutCustomer { get; } = [];

    /// <summary>Acumula otro reporte (de otro tenant) sobre este.</summary>
    public void Merge(BackfillReport other)
    {
        ArgumentNullException.ThrowIfNull(other);
        PartiesScanned += other.PartiesScanned;
        CustomerProfilesCreated += other.CustomerProfilesCreated;
        CustomerProfilesSkipped += other.CustomerProfilesSkipped;
        SupplierProfilesCreated += other.SupplierProfilesCreated;
        SupplierProfilesSkipped += other.SupplierProfilesSkipped;
        EmployeeProfilesCreated += other.EmployeeProfilesCreated;
        EmployeeProfilesSkipped += other.EmployeeProfilesSkipped;
        CreditAccountsCreated += other.CreditAccountsCreated;
        CreditAccountsSkipped += other.CreditAccountsSkipped;
        CiiuActivitiesCreated += other.CiiuActivitiesCreated;
        CiiuActivitiesSkipped += other.CiiuActivitiesSkipped;
        HoldsCreated += other.HoldsCreated;
        HoldsSkipped += other.HoldsSkipped;
        SuppliersWithoutCurrency += other.SuppliersWithoutCurrency;
        TaxRegimeMappedClean += other.TaxRegimeMappedClean;
        TaxRegimeEmpty += other.TaxRegimeEmpty;
        FiscalDataPopulated += other.FiscalDataPopulated;
        foreach (var id in other.TaxRegimeUnmapped) TaxRegimeUnmapped.Add(id);
        foreach (var id in other.CreditWithoutCustomer) CreditWithoutCustomer.Add(id);
    }

    public string ToSummary(string scope)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"[backfill-parties-v2] {scope}{(DryRun ? " (DRY-RUN — sin escrituras)" : "")}\n");
        sb.Append(CultureInfo.InvariantCulture, $"  Parties escaneados:        {PartiesScanned}\n");
        sb.Append(CultureInfo.InvariantCulture, $"  CustomerProfiles:          +{CustomerProfilesCreated} creados / {CustomerProfilesSkipped} ya existían\n");
        sb.Append(CultureInfo.InvariantCulture, $"  SupplierProfiles:          +{SupplierProfilesCreated} creados / {SupplierProfilesSkipped} ya existían\n");
        sb.Append(CultureInfo.InvariantCulture, $"  EmployeeProfiles:          +{EmployeeProfilesCreated} creados / {EmployeeProfilesSkipped} ya existían\n");
        sb.Append(CultureInfo.InvariantCulture, $"  CreditAccounts:            +{CreditAccountsCreated} creados / {CreditAccountsSkipped} ya existían\n");
        sb.Append(CultureInfo.InvariantCulture, $"  PartyCiiuActivities:       +{CiiuActivitiesCreated} creados / {CiiuActivitiesSkipped} ya existían\n");
        sb.Append(CultureInfo.InvariantCulture, $"  PartyHolds (CreditBlocked):+{HoldsCreated} creados / {HoldsSkipped} ya existían\n");
        sb.Append(CultureInfo.InvariantCulture, $"  Suppliers sin moneda:      {SuppliersWithoutCurrency} (DefaultCurrencyId null — PR-D)\n");
        sb.Append(CultureInfo.InvariantCulture, $"  TaxRegime mapeado limpio:  {TaxRegimeMappedClean}\n");
        sb.Append(CultureInfo.InvariantCulture, $"  FiscalData poblados:       {FiscalDataPopulated} (régimen+IVA persistido en Party — PR-D1)\n");
        sb.Append(CultureInfo.InvariantCulture, $"  TaxRegime vacío (sin dato):{TaxRegimeEmpty}\n");
        sb.Append(CultureInfo.InvariantCulture, $"  TaxRegime NO mapeado:      {TaxRegimeUnmapped.Count} (deuda PR-D)\n");
        sb.Append(CultureInfo.InvariantCulture, $"  Crédito sin rol Customer:  {CreditWithoutCustomer.Count} (anomalía, no se creó CreditAccount)\n");
        if (TaxRegimeUnmapped.Count > 0)
        {
            sb.Append(CultureInfo.InvariantCulture, $"    TaxRegime unmapped PartyIds: {string.Join(", ", TaxRegimeUnmapped.Take(50))}{(TaxRegimeUnmapped.Count > 50 ? " …" : "")}\n");
        }
        if (CreditWithoutCustomer.Count > 0)
        {
            sb.Append(CultureInfo.InvariantCulture, $"    Crédito-sin-Customer PartyIds: {string.Join(", ", CreditWithoutCustomer.Take(50))}{(CreditWithoutCustomer.Count > 50 ? " …" : "")}\n");
        }
        return sb.ToString();
    }
}
