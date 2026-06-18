using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.SharedRecords;

/// <summary>
/// PR-G1 — control genérico AddressList. Verifica el round-trip real del CRUD, la invariante
/// "una sola principal por owner" con DOS owners independientes, y el aislamiento multitenant.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AddressEndpointsTests
{
    private const string BasePath = "/api/v1/addresses";
    private readonly AuthHelper _auth;

    public AddressEndpointsTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    private sealed record AddrRow(Guid Id, bool IsActive, bool IsPrimary, string? LabelCode, string? Line);

    private static object NewAddressPayload(string ownerType, Guid ownerId, bool isPrimary, string? label, string line) => new
    {
        ownerType,
        ownerId,
        labelCode = label,
        isActive = true,
        isPrimary,
        country = "Colombia",
        department = "Bogotá, D.C.",
        city = "Bogotá, D.C.",
        departmentCode = "11",
        municipalityCode = "11001",
        line,
        barrio = (string?)null,
        reference = (string?)null,
        latitude = (decimal?)null,
        longitude = (decimal?)null,
    };

    private static async Task<List<AddrRow>> ListAsync(HttpClient client, string ownerType, Guid ownerId)
    {
        var resp = await client.GetAsync($"{BasePath}?ownerType={ownerType}&ownerId={ownerId}");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<List<AddrRow>>() ?? [];
    }

    [Fact]
    public async Task Crud_And_SinglePrimary_Hold_For_Two_Independent_Owners()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();

        // Owner A: dos direcciones, ambas pidiendo principal → la 2.ª degrada a la 1.ª.
        var createA1 = await client.PostAsJsonAsync(BasePath, NewAddressPayload("Party", ownerA, true, "BODEGA", "CL 1 # 1-1"));
        createA1.StatusCode.ShouldBe(HttpStatusCode.Created);
        var a1 = await createA1.Content.ReadFromJsonAsync<Guid>();

        var createA2 = await client.PostAsJsonAsync(BasePath, NewAddressPayload("Party", ownerA, true, "SEDE", "CL 2 # 2-2"));
        createA2.StatusCode.ShouldBe(HttpStatusCode.Created);
        var a2 = await createA2.Content.ReadFromJsonAsync<Guid>();

        var ownerAList = await ListAsync(client, "Party", ownerA);
        ownerAList.Count.ShouldBe(2);
        ownerAList.Count(x => x.IsPrimary).ShouldBe(1, "exactamente una principal por owner");
        ownerAList.Single(x => x.IsPrimary).Id.ShouldBe(a2, "la última en pedir principal gana");

        // set-primary de vuelta a la 1.ª → degrada a la 2.ª (idempotente vía la política de dominio).
        var setPrimary = await client.PostAsync($"{BasePath}/{a1}/set-primary", null);
        setPrimary.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ownerAList = await ListAsync(client, "Party", ownerA);
        ownerAList.Single(x => x.IsPrimary).Id.ShouldBe(a1);

        // Owner B: su propia principal — coexiste con la de A (índice único PARCIAL por owner).
        var createB1 = await client.PostAsJsonAsync(BasePath, NewAddressPayload("Party", ownerB, true, "OFICINA", "CL 9 # 9-9"));
        createB1.StatusCode.ShouldBe(HttpStatusCode.Created);
        var ownerBList = await ListAsync(client, "Party", ownerB);
        ownerBList.ShouldHaveSingleItem().IsPrimary.ShouldBeTrue();
        // A sigue teniendo SU principal intacta.
        (await ListAsync(client, "Party", ownerA)).Count(x => x.IsPrimary).ShouldBe(1);

        // Update: cambia la etiqueta de la principal de A.
        var update = await client.PutAsJsonAsync($"{BasePath}/{a1}", new
        {
            id = a1, labelCode = "EMPRESA", isActive = true, isPrimary = true, country = "Colombia",
            department = "Bogotá, D.C.", city = "Bogotá, D.C.", departmentCode = "11", municipalityCode = "11001",
            line = "CL 1 # 1-1 ACT", barrio = (string?)null, reference = (string?)null,
            latitude = (decimal?)null, longitude = (decimal?)null,
        });
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var afterUpdate = await ListAsync(client, "Party", ownerA);
        afterUpdate.Single(x => x.Id == a1).LabelCode.ShouldBe("EMPRESA");
        afterUpdate.Single(x => x.Id == a1).Line.ShouldBe("CL 1 # 1-1 ACT");

        // Delete (soft) de la 2.ª de A → queda 1.
        var del = await client.DeleteAsync($"{BasePath}/{a2}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(client, "Party", ownerA)).ShouldHaveSingleItem().Id.ShouldBe(a1);
    }

    [Fact]
    public async Task Addresses_Are_Tenant_Isolated()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"addr-iso-{uniqueId}";
        var otherAdminEmail = $"addr-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Mismo ownerId en ambos tenants: cada uno ve SOLO lo suyo.
        var ownerId = Guid.NewGuid();
        var rootCreate = await rootClient.PostAsJsonAsync(BasePath, NewAddressPayload("Party", ownerId, true, "BODEGA", "CL ROOT # 1-1"));
        rootCreate.StatusCode.ShouldBe(HttpStatusCode.Created);

        // El otro tenant NO ve la dirección de root para el mismo ownerId.
        (await ListAsync(otherClient, "Party", ownerId)).ShouldBeEmpty();

        // El otro tenant crea la suya; root sigue viendo solo la suya (1).
        var otherCreate = await otherClient.PostAsJsonAsync(BasePath, NewAddressPayload("Party", ownerId, true, "SEDE", "CL OTHER # 2-2"));
        otherCreate.StatusCode.ShouldBe(HttpStatusCode.Created);

        var rootList = await ListAsync(rootClient, "Party", ownerId);
        rootList.ShouldHaveSingleItem().Line.ShouldBe("CL ROOT # 1-1");
        var otherList = await ListAsync(otherClient, "Party", ownerId);
        otherList.ShouldHaveSingleItem().Line.ShouldBe("CL OTHER # 2-2");
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase)) return;
                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Tenant {tenantId} provisioning did not complete within {maxRetries}s.");
    }
}
