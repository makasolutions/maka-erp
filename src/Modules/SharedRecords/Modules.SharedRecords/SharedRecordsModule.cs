using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Data;
using FSH.Modules.SharedRecords.Features.v1.Addresses.CreateAddress;
using FSH.Modules.SharedRecords.Features.v1.Addresses.DeleteAddress;
using FSH.Modules.SharedRecords.Features.v1.Addresses.GetAddresses;
using FSH.Modules.SharedRecords.Features.v1.Addresses.SetPrimaryAddress;
using FSH.Modules.SharedRecords.Features.v1.Addresses.UpdateAddress;
using FSH.Modules.SharedRecords.Features.v1.Phones.CreatePhone;
using FSH.Modules.SharedRecords.Features.v1.Phones.DeletePhone;
using FSH.Modules.SharedRecords.Features.v1.Phones.GetPhones;
using FSH.Modules.SharedRecords.Features.v1.Phones.SetPrimaryPhone;
using FSH.Modules.SharedRecords.Features.v1.Phones.UpdatePhone;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.SharedRecords.SharedRecordsModule), 525)]

namespace FSH.Modules.SharedRecords;

/// <summary>
/// Controles genéricos polimórficos reutilizables (asociados por OwnerType+OwnerId). PR-G1: AddressList.
/// No depende de Parties ni de ningún otro módulo de dominio.
/// </summary>
public sealed class SharedRecordsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(SharedRecordsPermissions.All);
        builder.Services.AddHeroDbContext<SharedRecordsDbContext>();
        builder.Services.AddScoped<IDbInitializer, SharedRecordsDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<SharedRecordsDbContext>(name: "db:shared-records", failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        // §18.4 #14: NO .RequireAuthorization() en el grupo — anularía el FallbackPolicy de permisos
        // y haría que .RequirePermission() falle ABIERTO. El fallback ya exige autenticación + permiso.
        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/addresses")
            .WithTags("Addresses")
            .WithApiVersionSet(versionSet);

        group.MapGetAddressesEndpoint();
        group.MapCreateAddressEndpoint();
        group.MapUpdateAddressEndpoint();
        group.MapDeleteAddressEndpoint();
        group.MapSetPrimaryAddressEndpoint();

        var phones = endpoints
            .MapGroup("api/v{version:apiVersion}/phones")
            .WithTags("Phones")
            .WithApiVersionSet(versionSet);

        phones.MapGetPhonesEndpoint();
        phones.MapCreatePhoneEndpoint();
        phones.MapUpdatePhoneEndpoint();
        phones.MapDeletePhoneEndpoint();
        phones.MapSetPrimaryPhoneEndpoint();
    }
}
