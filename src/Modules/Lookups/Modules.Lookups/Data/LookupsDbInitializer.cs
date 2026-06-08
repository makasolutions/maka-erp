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
