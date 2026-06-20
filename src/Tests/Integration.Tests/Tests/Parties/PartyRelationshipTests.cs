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
            .Handle(new CreatePartyRelationshipCommand(source, target, type, null, null, primary, null, null), default).AsTask());

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
