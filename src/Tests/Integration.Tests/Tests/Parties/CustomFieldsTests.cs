using System.Net.Http.Json;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.CustomFields;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain.CustomFields;
using Integration.Tests.Infrastructure;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-1: subsistema de custom fields. Verifica lo que el compilador no garantiza: round-trip JSONB,
/// índice único parcial por (tenant, scope, slug), aislamiento de tenant, 403 sin permiso (regresión
/// §18.4 #14), y la completitud gobernada end-to-end (IsRequired no bloquea en Minimal; sí en Complete).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class CustomFieldsTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public CustomFieldsTests(FshWebApplicationFactory factory)
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

    private Task<Guid> CreateDefAsync(string slug, CustomFieldType type, bool isRequired = false,
        IReadOnlyList<CustomFieldOptionDto>? options = null, CustomFieldEntityType scope = CustomFieldEntityType.Party) =>
        InRoot(sp => sp.GetRequiredService<ICommandHandler<CreateCustomFieldDefinitionCommand, Guid>>()
            .Handle(new CreateCustomFieldDefinitionCommand(scope, $"Campo {slug}", slug, type, null,
                isRequired, false, false, null, false, options), default).AsTask());

    private Task<IReadOnlyList<CustomFieldDefinitionDto>> ListDefsAsync(string tenantId, CustomFieldEntityType? scope = null) =>
        InTenantScope(tenantId, sp => sp.GetRequiredService<IQueryHandler<GetCustomFieldDefinitionsQuery, IReadOnlyList<CustomFieldDefinitionDto>>>()
            .Handle(new GetCustomFieldDefinitionsQuery(scope), default).AsTask());

    [Fact]
    public async Task Create_And_Get_RoundTrips_WithOptions()
    {
        var slug = $"seg_{Guid.NewGuid():N}"[..12];
        var id = await CreateDefAsync(slug, CustomFieldType.Select,
            options: [new("vip", "VIP", "#f00"), new("std", "Estándar", null)]);

        var list = await ListDefsAsync(TestConstants.RootTenantId, CustomFieldEntityType.Party);
        var def = list.Single(d => d.Id == id);
        def.FieldType.ShouldBe(CustomFieldType.Select);
        def.Options.Count.ShouldBe(2);
        def.Options[0].Value.ShouldBe("vip");
        def.Activo.ShouldBeTrue();
    }

    [Fact]
    public async Task DuplicateActiveSlug_SameScope_Conflicts()
    {
        var slug = $"dup_{Guid.NewGuid():N}"[..12];
        await CreateDefAsync(slug, CustomFieldType.Text);

        // El índice parcial único ix_customfielddef_slug + el pre-chequeo del handler rechazan el duplicado activo.
        await Should.ThrowAsync<Exception>(() => CreateDefAsync(slug, CustomFieldType.Text));
    }

    [Fact]
    public async Task PartyCustomFields_Jsonb_RoundTrips()
    {
        var num = $"cf-{Guid.NewGuid():N}"[..13];
        var partyId = await InRoot(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", num, null, PartyKind.Juridica, $"Tercero {num}", PartyRole.Customer), default).AsTask());

        // Escribe valores en el JSONB (el cableado por UI llega en PR-3; aquí se prueba la persistencia).
        await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var party = await db.Parties.FirstAsync(p => p.Id == partyId);
            party.SetCustomFields(JsonDocument.Parse("""{ "nivel_riesgo": "alto", "score": 42 }"""));
            await db.SaveChangesAsync();
            return true;
        });

        // Re-lectura en un scope nuevo: el jsonb sobrevive intacto.
        var (riesgo, score) = await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var party = await db.Parties.AsNoTracking().FirstAsync(p => p.Id == partyId);
            party.CustomFields.ShouldNotBeNull();
            var root = party.CustomFields!.RootElement;
            return (root.GetProperty("nivel_riesgo").GetString(), root.GetProperty("score").GetInt32());
        });

        riesgo.ShouldBe("alto");
        score.ShouldBe(42);
    }

    [Fact]
    public async Task GovernedCompleteness_Required_NotBlockedInMinimal_ButRequiredInComplete()
    {
        var slug = $"req_{Guid.NewGuid():N}"[..12];
        await CreateDefAsync(slug, CustomFieldType.Text, isRequired: true);

        // Carga las definiciones reales persistidas (end-to-end) y valida con valores vacíos.
        var defs = await InRoot(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            return await db.CustomFieldDefinitions.AsNoTracking()
                .Where(d => d.EntityType == CustomFieldEntityType.Party && d.Activo && d.ApiSlug == slug)
                .ToListAsync();
        });

        var empty = new Dictionary<string, JsonElement>();
        CustomFieldValues.ValidateAll(defs, empty, CustomFieldCompletenessMode.Minimal).ShouldBeEmpty();
        CustomFieldValues.ValidateAll(defs, empty, CustomFieldCompletenessMode.Complete).Count.ShouldBe(1);
    }

    [Fact]
    public async Task TenantIsolation_DefinitionNotVisibleInOtherTenant()
    {
        var slug = $"iso_{Guid.NewGuid():N}"[..12];
        var id = await CreateDefAsync(slug, CustomFieldType.Text);

        // Mismo tenant la ve…
        (await ListDefsAsync(TestConstants.RootTenantId)).ShouldContain(d => d.Id == id);
        // …otro tenant NO (global query filter por TenantId).
        (await ListDefsAsync("other-tenant")).ShouldNotContain(d => d.Id == id);
    }

    [Fact]
    public async Task Create_Without_DefinePermission_Returns403()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (email, password) = await CreateConfirmedBasicUserAsync(rootClient);
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(email, password, TestConstants.RootTenantId);

        var response = await basicClient.PostAsJsonAsync("/api/v1/parties/custom-fields", new
        {
            entityType = "Party",
            title = "Campo prohibido",
            apiSlug = (string?)null,
            fieldType = "Text",
            description = (string?)null,
            isRequired = false,
            isUnique = false,
            isDefaultValueEnabled = false,
            defaultValue = (string?)null,
            isMultiselect = false,
            options = (object?)null,
        });

        // Regresión §18.4 #14: una escritura sin el permiso debe ser 403, no 200/201/404.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<(string Email, string Password)> CreateConfirmedBasicUserAsync(HttpClient rootClient)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"cf-basic-{uniqueId}@example.com";
        const string password = "Test@1234!";

        var register = await rootClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Custom",
            lastName = "Fields",
            email,
            userName = $"cfbasic-{uniqueId}",
            password,
            confirmPassword = password,
        });
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<FSH.Modules.Identity.Domain.FshUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
        (await userManager.ConfirmEmailAsync(user!, token)).Succeeded.ShouldBeTrue();

        return (email, password);
    }
}
