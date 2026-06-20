using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Features.v1.Parties.CreateParty;
using FSH.Modules.Parties.Features.v1.Parties.DeleteParty;
using FSH.Modules.Parties.Features.v1.Parties.GetParties;
using FSH.Modules.Parties.Features.v1.Parties.GetPartyById;
using FSH.Modules.Parties.Features.v1.Parties.RestoreParty;
using FSH.Modules.Parties.Features.v1.Parties.SetPartyRoles;
using FSH.Modules.Parties.Features.v1.Parties.SetGlobalSupplier;
using FSH.Modules.Parties.Features.v1.Parties.UpdateParty;
using FSH.Modules.Parties.Contracts.v1.Verification;
using FSH.Modules.Parties.Features.v1.Verification;
using FSH.Modules.Parties.Features.v1.CustomFields.CreateCustomFieldDefinition;
using FSH.Modules.Parties.Features.v1.CustomFields.UpdateCustomFieldDefinition;
using FSH.Modules.Parties.Features.v1.CustomFields.DeactivateCustomFieldDefinition;
using FSH.Modules.Parties.Features.v1.CustomFields.GetCustomFieldDefinitions;
using FSH.Modules.Parties.Features.v1.Relationships.CreatePartyRelationship;
using FSH.Modules.Parties.Features.v1.Relationships.UpdatePartyRelationship;
using FSH.Modules.Parties.Features.v1.Relationships.SetPrimaryPartyRelationship;
using FSH.Modules.Parties.Features.v1.Relationships.DeletePartyRelationship;
using FSH.Modules.Parties.Features.v1.Relationships.GetPartyRelationships;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Parties.PartiesModule), 550)]

namespace FSH.Modules.Parties;

public sealed class PartiesModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(PartiesPermissions.All);
        builder.Services.AddHeroDbContext<PartiesDbContext>();
        builder.Services.AddScoped<IDbInitializer, PartiesDbInitializer>();

        // Jerarquía de terceros (PR-D3): carga ancestros (recursive CTE) para las validaciones
        // de ciclos y la delegación comercial. Lo consumen las features de D5 y los integration tests.
        builder.Services.AddScoped<Domain.PartyHierarchyService>();

        // Dual-write v1→v2 (PR-D5): sincroniza el estado v2 de un tercero desde su v1 en los
        // handlers Create/Update (mismo DbContext, misma transacción).
        builder.Services.AddScoped<Sync.PartyV2Synchronizer>();

        // Identity verification (NIT/cédula): local validation + swappable lookup provider.
        builder.Services.Configure<IdentityVerificationOptions>(
            builder.Configuration.GetSection("IdentityVerification"));
        var verifyOptions = builder.Configuration.GetSection("IdentityVerification").Get<IdentityVerificationOptions>()
            ?? new IdentityVerificationOptions();
        if (string.Equals(verifyOptions.Provider, "Rues", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddHttpClient<IIdentityVerificationProvider, RuesIdentityVerificationProvider>(client =>
            {
                client.BaseAddress = new Uri(verifyOptions.RuesBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(verifyOptions.TimeoutSeconds, 2, 30));
            });
        }
        else
        {
            builder.Services.AddSingleton<IIdentityVerificationProvider, NullIdentityVerificationProvider>();
        }

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<PartiesDbContext>(name: "db:parties", failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/parties")
            .WithTags("Parties")
            .WithApiVersionSet(versionSet);
        // §18.4 #14: NO .RequireAuthorization() here — it overrides the global permission
        // FallbackPolicy and makes per-endpoint .RequirePermission() fail OPEN. The fallback
        // already enforces authentication + the RequiredPermission metadata.

        group.MapGetPartiesEndpoint();
        group.MapGetPartyByIdEndpoint();
        group.MapCreatePartyEndpoint();
        group.MapUpdatePartyEndpoint();
        group.MapSetPartyRolesEndpoint();
        group.MapDeletePartyEndpoint();
        group.MapRestorePartyEndpoint();
        group.MapVerifyIdentificationEndpoint();
        group.MapSetGlobalSupplierEndpoint();

        // PR-1: custom fields (definir = admin / ver = básico). Sin .RequireAuthorization() en el group.
        group.MapGetCustomFieldDefinitionsEndpoint();
        group.MapCreateCustomFieldDefinitionEndpoint();
        group.MapUpdateCustomFieldDefinitionEndpoint();
        group.MapDeleteCustomFieldDefinitionEndpoint();

        // PR-2: vínculos M2M persona↔empresa (View=básico / Manage). Sin .RequireAuthorization() en el group.
        group.MapGetPartyRelationshipsEndpoint();
        group.MapCreatePartyRelationshipEndpoint();
        group.MapUpdatePartyRelationshipEndpoint();
        group.MapSetPrimaryPartyRelationshipEndpoint();
        group.MapDeletePartyRelationshipEndpoint();
    }
}
