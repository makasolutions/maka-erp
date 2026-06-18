using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.SharedRecords;

/// <summary>
/// PR-G2 — control genérico PhoneList. Verifica el round-trip real del CRUD, la invariante
/// "un solo principal por owner" con DOS owners independientes, y el aislamiento multitenant.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PhoneEndpointsTests
{
    private const string BasePath = "/api/v1/phones";
    private readonly AuthHelper _auth;

    public PhoneEndpointsTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    private sealed record PhoneRow(Guid Id, bool IsActive, bool IsPrimary, string? TypeCode, string Number);

    private static object NewPhonePayload(string ownerType, Guid ownerId, bool isPrimary, string? type, string number) => new
    {
        ownerType,
        ownerId,
        typeCode = type,
        isActive = true,
        isPrimary,
        number,
        extension = (string?)null,
        countryCode = (string?)null,
    };

    private static async Task<List<PhoneRow>> ListAsync(HttpClient client, string ownerType, Guid ownerId)
    {
        var resp = await client.GetAsync($"{BasePath}?ownerType={ownerType}&ownerId={ownerId}");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await resp.Content.ReadFromJsonAsync<List<PhoneRow>>() ?? [];
    }

    [Fact]
    public async Task Crud_And_SinglePrimary_Hold_For_Two_Independent_Owners()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();

        // Owner A: dos teléfonos, ambos pidiendo principal → el 2.º degrada al 1.º.
        var createA1 = await client.PostAsJsonAsync(BasePath, NewPhonePayload("Party", ownerA, true, "CELULAR", "3201234567"));
        createA1.StatusCode.ShouldBe(HttpStatusCode.Created);
        var a1 = await createA1.Content.ReadFromJsonAsync<Guid>();

        var createA2 = await client.PostAsJsonAsync(BasePath, NewPhonePayload("Party", ownerA, true, "WHATSAPP", "3009998877"));
        createA2.StatusCode.ShouldBe(HttpStatusCode.Created);
        var a2 = await createA2.Content.ReadFromJsonAsync<Guid>();

        var ownerAList = await ListAsync(client, "Party", ownerA);
        ownerAList.Count.ShouldBe(2);
        ownerAList.Count(x => x.IsPrimary).ShouldBe(1, "exactamente un principal por owner");
        ownerAList.Single(x => x.IsPrimary).Id.ShouldBe(a2, "el último en pedir principal gana");

        // set-primary de vuelta al 1.º → degrada al 2.º (idempotente vía la política de dominio).
        var setPrimary = await client.PostAsync($"{BasePath}/{a1}/set-primary", null);
        setPrimary.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ownerAList = await ListAsync(client, "Party", ownerA);
        ownerAList.Single(x => x.IsPrimary).Id.ShouldBe(a1);

        // Owner B: su propio principal — coexiste con el de A (índice único PARCIAL por owner).
        var createB1 = await client.PostAsJsonAsync(BasePath, NewPhonePayload("Party", ownerB, true, "FIJO", "6012345678"));
        createB1.StatusCode.ShouldBe(HttpStatusCode.Created);
        var ownerBList = await ListAsync(client, "Party", ownerB);
        ownerBList.ShouldHaveSingleItem().IsPrimary.ShouldBeTrue();
        (await ListAsync(client, "Party", ownerA)).Count(x => x.IsPrimary).ShouldBe(1);

        // Update: cambia el tipo y el número del principal de A.
        var update = await client.PutAsJsonAsync($"{BasePath}/{a1}", new
        {
            id = a1, typeCode = "FAX", isActive = true, isPrimary = true,
            number = "6017654321", extension = (string?)null, countryCode = (string?)null,
        });
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var afterUpdate = await ListAsync(client, "Party", ownerA);
        afterUpdate.Single(x => x.Id == a1).TypeCode.ShouldBe("FAX");
        afterUpdate.Single(x => x.Id == a1).Number.ShouldBe("6017654321");

        // Delete (soft) del 2.º de A → queda 1.
        var del = await client.DeleteAsync($"{BasePath}/{a2}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(client, "Party", ownerA)).ShouldHaveSingleItem().Id.ShouldBe(a1);
    }

    [Fact]
    public async Task Invalid_Number_Returns_400_Not_500()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var empty = await client.PostAsJsonAsync(BasePath, NewPhonePayload("Party", Guid.NewGuid(), false, "CELULAR", ""));
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var bad = await client.PostAsJsonAsync(BasePath, NewPhonePayload("Party", Guid.NewGuid(), false, "CELULAR", "abc-xyz"));
        bad.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Phones_Are_Tenant_Isolated()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"phone-iso-{uniqueId}";
        var otherAdminEmail = $"phone-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Mismo ownerId en ambos tenants: cada uno ve SOLO lo suyo.
        var ownerId = Guid.NewGuid();
        var rootCreate = await rootClient.PostAsJsonAsync(BasePath, NewPhonePayload("Party", ownerId, true, "CELULAR", "3001112222"));
        rootCreate.StatusCode.ShouldBe(HttpStatusCode.Created);

        (await ListAsync(otherClient, "Party", ownerId)).ShouldBeEmpty();

        var otherCreate = await otherClient.PostAsJsonAsync(BasePath, NewPhonePayload("Party", ownerId, true, "FIJO", "6019998888"));
        otherCreate.StatusCode.ShouldBe(HttpStatusCode.Created);

        (await ListAsync(rootClient, "Party", ownerId)).ShouldHaveSingleItem().Number.ShouldBe("3001112222");
        (await ListAsync(otherClient, "Party", ownerId)).ShouldHaveSingleItem().Number.ShouldBe("6019998888");
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
