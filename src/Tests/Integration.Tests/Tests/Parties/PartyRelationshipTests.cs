using System.Net.Http.Json;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.Records.DeleteBasicRecord;
using FSH.Modules.Lookups.Data;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Contracts.v1.Relationships;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain.Relationships;
using Integration.Tests.Infrastructure;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-2: PartyRelationship M2M + reconciliación. Round-trip, índice parcial "un principal por empresa",
/// aislamiento de tenant, CustomFields jsonb, IsPEP en persona, código protegido no se borra, 403 sin permiso.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartyRelationshipTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public PartyRelationshipTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    private static AppTenantInfo Tenant(string id) => new(id, id);

    private async Task<T> InTenantScope<T>(string tenantId, Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Tenant(tenantId));
        return await action(scope.ServiceProvider);
    }

    private Task<T> InRoot<T>(Func<IServiceProvider, Task<T>> action) =>
        InTenantScope(TestConstants.RootTenantId, action);

    private Task<Guid> CreatePartyAsync(string suffix, PartyKind kind = PartyKind.Juridica) =>
        InRoot(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", $"{suffix}{Guid.NewGuid():N}"[..12], null, kind, $"Party {suffix}", PartyRole.Customer), default).AsTask());

    private Task<Guid> CreateRelAsync(Guid source, Guid target, string type, bool primary = false) =>
        InRoot(sp => sp.GetRequiredService<ICommandHandler<CreatePartyRelationshipCommand, Guid>>()
            .Handle(new CreatePartyRelationshipCommand(source, null, target, type, null, null, primary, null, null), default).AsTask());

    [Fact]
    public async Task Create_And_List_RoundTrips_WithSourceName()
    {
        var person = await CreatePartyAsync("per", PartyKind.Natural);
        var company = await CreatePartyAsync("comp");
        var id = await CreateRelAsync(person, company, "EMPLEADO", primary: true);

        var list = await InRoot(sp => sp.GetRequiredService<IQueryHandler<GetPartyRelationshipsQuery, IReadOnlyList<PartyRelationshipDto>>>()
            .Handle(new GetPartyRelationshipsQuery(company), default).AsTask());

        var rel = list.Single(r => r.Id == id);
        rel.SourcePartyId.ShouldBe(person);
        rel.IsPrimary.ShouldBeTrue();
        rel.SourceName.ShouldNotBeNullOrEmpty();   // nombre resuelto en el backend
    }

    [Fact]
    public async Task PartialUniqueIndex_RejectsTwoActivePrimaries_PerCompany()
    {
        var company = await CreatePartyAsync("comp");
        var p1 = await CreatePartyAsync("p1", PartyKind.Natural);
        var p2 = await CreatePartyAsync("p2", PartyKind.Natural);

        // Inserción directa (bypass del handler que desmarca) → el índice parcial debe rechazar el 2.º primario.
        await Should.ThrowAsync<Exception>(() => InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            db.PartyRelationships.Add(PartyRelationship.Create(p1, company, "EMPLEADO", isPrimary: true));
            db.PartyRelationships.Add(PartyRelationship.Create(p2, company, "SOCIO", isPrimary: true));
            await db.SaveChangesAsync();
            return true;
        }));
    }

    [Fact]
    public async Task CustomFields_Jsonb_RoundTrips_OnRelationship()
    {
        var person = await CreatePartyAsync("per", PartyKind.Natural);
        var company = await CreatePartyAsync("comp");
        var id = await CreateRelAsync(person, company, "CONTACTO_EXTERNO");

        await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var rel = await db.PartyRelationships.FirstAsync(r => r.Id == id);
            rel.SetCustomFields(JsonDocument.Parse("""{ "antiguedad": 5 }"""));
            await db.SaveChangesAsync();
            return true;
        });

        var value = await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var rel = await db.PartyRelationships.AsNoTracking().FirstAsync(r => r.Id == id);
            return rel.CustomFields!.RootElement.GetProperty("antiguedad").GetInt32();
        });
        value.ShouldBe(5);
    }

    [Fact]
    public async Task TenantIsolation_RelationshipNotVisibleInOtherTenant()
    {
        var person = await CreatePartyAsync("per", PartyKind.Natural);
        var company = await CreatePartyAsync("comp");
        await CreateRelAsync(person, company, "EMPLEADO");

        var other = await InTenantScope("other-tenant", sp => sp.GetRequiredService<IQueryHandler<GetPartyRelationshipsQuery, IReadOnlyList<PartyRelationshipDto>>>()
            .Handle(new GetPartyRelationshipsQuery(company), default).AsTask());
        other.ShouldBeEmpty();
    }

    [Fact]
    public async Task Party_IsPEP_Persists_OnPerson()
    {
        var person = await CreatePartyAsync("pep", PartyKind.Natural);
        await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var p = await db.Parties.FirstAsync(x => x.Id == person);
            p.SetPep(true, "Funcionario");
            await db.SaveChangesAsync();
            return true;
        });

        var (isPep, type) = await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var p = await db.Parties.AsNoTracking().FirstAsync(x => x.Id == person);
            return (p.IsPEP, p.PepType);
        });
        isPep.ShouldBeTrue();
        type.ShouldBe("Funcionario");
    }

    [Fact]
    public async Task ProtectedContactFunctionCode_CannotBeDeleted()
    {
        var (tableId, recordId) = await InRoot(async sp =>
        {
            var lookups = sp.GetRequiredService<LookupsDbContext>();
            var table = await lookups.BasicTables.IgnoreQueryFilters().Include(t => t.Records)
                .FirstAsync(t => t.Code == "ContactFunction" && t.TenantId == null);
            var prot = table.Records.First(r => r.Code == "FACTURACION_ELECTRONICA");
            prot.IsProtected.ShouldBeTrue();
            return (table.Id, prot.Id);
        });

        // El handler bloquea el borrado del código de sistema (409).
        await Should.ThrowAsync<Exception>(() => InRoot(sp =>
            sp.GetRequiredService<ICommandHandler<DeleteBasicRecordCommand>>()
                .Handle(new DeleteBasicRecordCommand(tableId, recordId), default).AsTask()));
    }

    [Fact]
    public async Task CreateRelationship_Without_ManagePermission_Returns403()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (email, password) = await CreateConfirmedBasicUserAsync(rootClient);
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(email, password, TestConstants.RootTenantId);

        var response = await basicClient.PostAsJsonAsync("/api/v1/parties/relationships", new
        {
            sourcePartyId = Guid.NewGuid(),
            targetPartyId = Guid.NewGuid(),
            relationshipTypeCode = "EMPLEADO",
            contactFunctionCode = (string?)null,
            jobTitleCode = (string?)null,
            isPrimary = false,
            startDate = (string?)null,
            endDate = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);   // regresión §18.4 #14
    }

    // ───────────────────────── PR-3: ContactList (Opción B) ─────────────────────────

    private static NewPersonInput NewPerson(string suffix) =>
        new("CC", $"{suffix}{Guid.NewGuid():N}"[..10], null, suffix, "Contacto", null);

    private Task<Guid> CreatePartyWithRelationshipsAsync(
        IReadOnlyList<PartyRelationshipLineInput> rels, string suffix = "comp") =>
        InRoot(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", $"{suffix}{Guid.NewGuid():N}"[..12], null,
                PartyKind.Juridica, $"Company {suffix}", PartyRole.Customer, Relationships: rels), default).AsTask());

    private Task<List<PartyRelationship>> RelsOf(Guid company) =>
        InRoot(sp => sp.GetRequiredService<PartiesDbContext>().PartyRelationships.AsNoTracking()
            .Where(r => r.TargetPartyId == company).ToListAsync());

    [Fact]
    public async Task CreateParty_WithRelationships_PersistsAtomically_AndCreatesNewPerson()
    {
        var line = new PartyRelationshipLineInput(null, NewPerson("inl"), "REPRESENTANTE_LEGAL",
            "COMERCIAL", null, IsPrimary: true, null, null, null);
        var company = await CreatePartyWithRelationshipsAsync([line]);

        var rels = await RelsOf(company);
        rels.Count.ShouldBe(1);
        rels[0].IsPrimary.ShouldBeTrue();
        // la persona nueva inline se creó (existe el Party referenciado)
        var personExists = await InRoot(sp => sp.GetRequiredService<PartiesDbContext>()
            .Parties.AsNoTracking().AnyAsync(p => p.Id == rels[0].SourcePartyId && p.Kind == PartyKind.Natural));
        personExists.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateParty_WithRelationships_ReusesExistingPerson_NoDuplicate()
    {
        var person = await CreatePartyAsync("exist", PartyKind.Natural);
        var line = new PartyRelationshipLineInput(person, null, "SOCIO", null, null, false, null, null, null);
        var company = await CreatePartyWithRelationshipsAsync([line]);

        var rels = await RelsOf(company);
        rels.Single().SourcePartyId.ShouldBe(person);   // reusa, no crea otra persona
    }

    [Fact]
    public async Task CreateParty_WithTwoPrimaryLines_RollsBackEntirely()
    {
        var idNum = $"roll{Guid.NewGuid():N}"[..12];
        var lines = new List<PartyRelationshipLineInput>
        {
            new(null, NewPerson("a"), "REPRESENTANTE_LEGAL", null, null, IsPrimary: true, null, null, null),
            new(null, NewPerson("b"), "SOCIO", null, null, IsPrimary: true, null, null, null),
        };

        await Should.ThrowAsync<Exception>(() => InRoot(sp =>
            sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
                .Handle(new CreatePartyCommand("NIT", idNum, null, PartyKind.Juridica, "Rollback Co",
                    PartyRole.Customer, Relationships: lines), default).AsTask()));

        // Rollback total: ni empresa, ni relaciones (un solo SaveChanges no se ejecutó).
        await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            (await db.Parties.AsNoTracking().AnyAsync(p => p.IdentificationNumber == idNum)).ShouldBeFalse();
            return true;
        });
    }

    /// <summary>Refuerzo del gate: el invariante "1 principal por empresa" debe dar el MISMO estado final
    /// por el camino buffer (CreateParty atómico) y por el camino live (CreatePartyRelationship). No deben
    /// divergir (un wizard válido que viole el invariante sería un falso verde).</summary>
    [Fact]
    public async Task PrimaryInvariant_BufferAndLive_ProduceSameFinalState()
    {
        var a = await CreatePartyAsync("pa", PartyKind.Natural);
        var b = await CreatePartyAsync("pb", PartyKind.Natural);

        // BUFFER: ambas líneas en la creación atómica; solo A principal.
        var bufferCo = await CreatePartyWithRelationshipsAsync(
        [
            new(a, null, "REPRESENTANTE_LEGAL", null, null, IsPrimary: true, null, null, null),
            new(b, null, "SOCIO", null, null, IsPrimary: false, null, null, null),
        ]);

        // LIVE: empresa primero, luego cada relación por endpoint; A principal, B no.
        var liveCo = await CreatePartyAsync("live");
        await CreateRelAsync(a, liveCo, "REPRESENTANTE_LEGAL", primary: true);
        await CreateRelAsync(b, liveCo, "SOCIO", primary: false);

        var bufferState = Normalize(await RelsOf(bufferCo));
        var liveState = Normalize(await RelsOf(liveCo));
        bufferState.ShouldBe(liveState);   // mismo estado final → no divergen

        static (int primaries, string? primaryType) Normalize(List<PartyRelationship> rels) =>
            (rels.Count(r => r.IsPrimary), rels.SingleOrDefault(r => r.IsPrimary)?.RelationshipTypeCode);
    }

    [Fact]
    public async Task BySource_ListsCompaniesForPerson()
    {
        var person = await CreatePartyAsync("src", PartyKind.Natural);
        var c1 = await CreatePartyAsync("c1");
        var c2 = await CreatePartyAsync("c2");
        await CreateRelAsync(person, c1, "EMPLEADO");
        await CreateRelAsync(person, c2, "SOCIO");

        var list = await InRoot(sp => sp.GetRequiredService<IQueryHandler<GetPartyRelationshipsBySourceQuery, IReadOnlyList<PartyRelationshipBySourceDto>>>()
            .Handle(new GetPartyRelationshipsBySourceQuery(person), default).AsTask());

        list.Count.ShouldBe(2);
        list.Select(r => r.TargetPartyId).ShouldBe(new[] { c1, c2 }, ignoreOrder: true);
        list.ShouldAllBe(r => r.TargetName != null);   // nombre de la empresa resuelto en el backend
    }

    private async Task<(string Email, string Password)> CreateConfirmedBasicUserAsync(HttpClient rootClient)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"rel-basic-{uniqueId}@example.com";
        const string password = "Test@1234!";

        var register = await rootClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Rel", lastName = "Basic", email, userName = $"relbasic-{uniqueId}",
            password, confirmPassword = password,
        });
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FSH.Modules.Identity.Domain.FshUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
        (await userManager.ConfirmEmailAsync(user!, token)).Succeeded.ShouldBeTrue();
        return (email, password);
    }
}
