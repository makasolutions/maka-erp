using FSH.Framework.Persistence;
using FSH.Modules.Lookups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Lookups.Data;

public sealed class LookupsDbInitializer(
    LookupsDbContext dbContext,
    ILogger<LookupsDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Lookups] applied migrations");
        }
    }

    /// <summary>Seeds the GLOBAL basic tables (TenantId=null) consumed by Parties. Idempotent.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedTableAsync("IdentificationType", "Tipos de identificación", 10,
            [("NIT", "NIT"), ("CC", "Cédula de ciudadanía"), ("CE", "Cédula de extranjería"),
             ("PASAPORTE", "Pasaporte"), ("TI", "Tarjeta de identidad"), ("NUIP", "NUIP"),
             ("NIT_EXT", "NIT de otro país")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("TaxRegime", "Régimen tributario", 20,
            [("COMUN", "Responsable de IVA"), ("NO_RESPONSABLE_IVA", "No responsable de IVA"),
             ("SIMPLE", "Régimen simple de tributación"), ("GRAN_CONTRIBUYENTE", "Gran contribuyente")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("Gender", "Género", 30,
            [("M", "Masculino"), ("F", "Femenino"), ("OTRO", "Otro")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("MaritalStatus", "Estado civil", 40,
            [("SOLTERO", "Soltero(a)"), ("CASADO", "Casado(a)"), ("UNION_LIBRE", "Unión libre"),
             ("DIVORCIADO", "Divorciado(a)"), ("VIUDO", "Viudo(a)")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("PartyChannelType", "Tipos de canal de contacto", 50,
            [("PHONE_FIXED", "Teléfono fijo"), ("MOBILE", "Celular"), ("WHATSAPP", "WhatsApp"),
             ("FACETIME", "FaceTime"), ("SKYPE", "Skype"), ("INSTAGRAM", "Instagram"),
             ("FACEBOOK", "Facebook"), ("WEBSITE", "Sitio web"), ("OTHER", "Otro")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("PartySource", "Origen del tercero", 60,
            [("WEB", "Sitio web"), ("WHATSAPP", "WhatsApp"), ("INSTAGRAM", "Instagram"),
             ("FACEBOOK", "Facebook"), ("REFERIDO", "Referido"), ("MOSTRADOR", "Mostrador"),
             ("CSV", "Importación CSV"), ("OTRO", "Otro")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("AddressLabel", "Etiqueta de dirección", 70,
            [("CASA", "Casa"), ("OFICINA", "Oficina"), ("EMPRESA", "Empresa"), ("SUCURSAL", "Sucursal"),
             ("BODEGA", "Bodega"), ("OTRO", "Otro")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("ContactType", "Tipo de contacto", 80,
            [("PRINCIPAL", "Principal"), ("SECUNDARIO", "Secundario"), ("EMERGENCIA", "Emergencia")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("ContactArea", "Área del contacto", 90,
            [("COMERCIAL", "Comercial"), ("FACTURACION", "Facturación"), ("SOPORTE", "Soporte"),
             ("COMPRAS", "Compras"), ("GERENCIA", "Gerencia"), ("LOGISTICA", "Logística"),
             ("TESORERIA", "Tesorería")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("Position", "Cargo", 100,
            [("GERENTE", "Gerente"), ("DIRECTOR", "Director"), ("JEFE", "Jefe"), ("COORDINADOR", "Coordinador"),
             ("ANALISTA", "Analista"), ("ASESOR", "Asesor comercial"), ("AUXILIAR", "Auxiliar"),
             ("ASISTENTE", "Asistente"), ("OTRO", "Otro")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("Profession", "Profesión", 110,
            [("ADMINISTRADOR", "Administrador de empresas"), ("CONTADOR", "Contador"), ("INGENIERO", "Ingeniero"),
             ("ABOGADO", "Abogado"), ("DISENADOR", "Diseñador"), ("COMUNICADOR", "Comunicador"),
             ("TECNICO", "Técnico"), ("OTRO", "Otro")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("CreditDays", "Días de crédito", 120,
            [("1", "1 día"), ("7", "7 días"), ("15", "15 días"), ("30", "30 días"),
             ("45", "45 días"), ("60", "60 días")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("FiscalResponsibility", "Responsabilidad fiscal (DIAN)", 125,
            [("O-13", "O-13 Gran contribuyente"), ("O-15", "O-15 Autorretenedor"),
             ("O-23", "O-23 Agente de retención IVA"), ("O-47", "O-47 Régimen simple de tributación"),
             ("R-99-PN", "R-99-PN No responsable")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("Ciiu", "Actividad económica (CIIU)", 130,
            [("4651", "4651 - Comercio de computadores y equipos periféricos"),
             ("4652", "4652 - Comercio de equipos de comunicación"),
             ("4742", "4742 - Comercio de equipos de sonido y video"),
             ("4759", "4759 - Comercio de otros artículos de uso doméstico"),
             ("7420", "7420 - Actividades de fotografía"),
             ("9001", "9001 - Creación musical y audiovisual"),
             ("6201", "6201 - Desarrollo de sistemas informáticos"),
             ("4690", "4690 - Comercio al por mayor no especializado")],
            cancellationToken).ConfigureAwait(false);

        // ----- HR / Nómina -----
        await SeedTableAsync("LaborDepartment", "Departamento laboral", 200,
            [("ADMIN", "Administración"), ("VENTAS", "Ventas"), ("BODEGA", "Bodega y logística"),
             ("CONTABILIDAD", "Contabilidad"), ("TECNOLOGIA", "Tecnología"), ("CREATIVO", "Creativo / Studios"),
             ("SOPORTE", "Soporte")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("EmployeeContractType", "Tipo de contrato", 210,
            [("INDEFINIDO", "Término indefinido"), ("FIJO", "Término fijo"), ("OBRA_LABOR", "Obra o labor"),
             ("APRENDIZAJE", "Aprendizaje"), ("PRESTACION", "Prestación de servicios")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("ContractDuration", "Duración del contrato", 220,
            [("1M", "1 mes"), ("3M", "3 meses"), ("6M", "6 meses"), ("1A", "1 año"), ("INDEF", "Indefinido")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("ArlRiskLevel", "Nivel de riesgo ARL", 230,
            [("I", "Riesgo I"), ("II", "Riesgo II"), ("III", "Riesgo III"), ("IV", "Riesgo IV"), ("V", "Riesgo V")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("SalaryType", "Tipo de salario", 240,
            [("ORDINARIO", "Ordinario"), ("INTEGRAL", "Integral")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("PayrollPaymentMethod", "Medio de pago de nómina", 250,
            [("TRANSFERENCIA", "Transferencia bancaria"), ("EFECTIVO", "Efectivo"), ("CHEQUE", "Cheque")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("HealthProvider", "EPS", 260,
            [("SURA", "EPS Sura"), ("SANITAS", "EPS Sanitas"), ("NUEVA_EPS", "Nueva EPS"),
             ("SALUD_TOTAL", "Salud Total"), ("COMPENSAR", "Compensar"), ("FAMISANAR", "Famisanar")],
            cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("PensionFund", "Fondo de pensiones (AFP)", 270,
            [("PORVENIR", "Porvenir"), ("PROTECCION", "Protección"), ("COLFONDOS", "Colfondos"),
             ("COLPENSIONES", "Colpensiones"), ("SKANDIA", "Skandia")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("SeveranceFund", "Fondo de cesantías", 280,
            [("PORVENIR", "Porvenir"), ("PROTECCION", "Protección"), ("COLFONDOS", "Colfondos"),
             ("FNA", "Fondo Nacional del Ahorro")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("Ccf", "Caja de compensación (CCF)", 290,
            [("COMPENSAR", "Compensar"), ("COLSUBSIDIO", "Colsubsidio"), ("CAFAM", "Cafam"),
             ("COMFAMA", "Comfama"), ("COMFENALCO", "Comfenalco")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("ArlProvider", "Administradora de riesgos (ARL)", 300,
            [("SURA", "ARL Sura"), ("POSITIVA", "Positiva"), ("COLMENA", "Colmena Seguros"),
             ("BOLIVAR", "Seguros Bolívar"), ("AXA", "AXA Colpatria")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("CostCenter", "Centro de costos", 310,
            [("ADMIN", "Administrativo"), ("COMERCIAL", "Comercial"), ("LOGISTICA", "Logística"),
             ("STUDIOS", "Maka Studios"), ("IMPORTACIONES", "Importaciones")], cancellationToken).ConfigureAwait(false);

        await SeedTableAsync("Branch", "Sucursal", 320,
            [("BOGOTA", "Bogotá"), ("MEDELLIN", "Medellín")], cancellationToken).ConfigureAwait(false);

        await SeedDivipolaAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Siembra la geografía DIVIPOLA (DANE) global desde el CSV embebido
    /// <c>divipola.es-CO.csv</c> (DeptCode;DeptName;MunCode;MunName). Idempotente.
    /// </summary>
    private async Task SeedDivipolaAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Departments.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var assembly = typeof(LookupsDbInitializer).Assembly;
        string? resource = Array.Find(assembly.GetManifestResourceNames(),
            n => n.EndsWith("divipola.es-CO.csv", StringComparison.OrdinalIgnoreCase));
        if (resource is null)
        {
            logger.LogWarning("[Lookups] DIVIPOLA seed resource not found");
            return;
        }

        await using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);

        var departments = new Dictionary<string, Department>(StringComparer.Ordinal);
        var municipalities = new List<Municipality>();
        bool header = true;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (header) { header = false; continue; }
            if (line.Length == 0) continue;
            string[] cols = line.Split(';');
            if (cols.Length < 4) continue;
            string dc = cols[0].Trim(), dn = cols[1].Trim(), mc = cols[2].Trim(), mn = cols[3].Trim();
            if (dc.Length == 0 || mc.Length == 0) continue;
            if (!departments.ContainsKey(dc)) departments[dc] = Department.Create(dc, dn);
            municipalities.Add(Municipality.Create(mc, mn, dc));
        }

        dbContext.Departments.AddRange(departments.Values);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        const int batch = 500;
        for (int i = 0; i < municipalities.Count; i += batch)
        {
            dbContext.Municipalities.AddRange(municipalities.GetRange(i, Math.Min(batch, municipalities.Count - i)));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "[Lookups] seeded DIVIPOLA: {Departments} departments, {Municipalities} municipalities",
            departments.Count, municipalities.Count);
    }

    private async Task SeedTableAsync(
        string code, string name, int sortOrder,
        (string Code, string Value)[] records, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.BasicTables
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Code == code && t.TenantId == null, cancellationToken)
            .ConfigureAwait(false);
        if (exists) return;

        var table = BasicTable.Create(code, name, isGlobal: true, tenantId: null,
            isManageable: true, sortOrder: sortOrder, visibleInMenu: false);

        int i = 0;
        foreach (var (rc, rv) in records)
            table.Records.Add(BasicRecord.Create(table.Id, rc, rv, tenantId: null, sortOrder: i++ * 10));

        dbContext.BasicTables.Add(table);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[Lookups] seeded global table {Code} ({Count} records)", code, records.Length);
    }
}
