using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Data;
using FSH.Modules.Lookups.Features.v1.BasicTables.CreateBasicTable;
using FSH.Modules.Lookups.Features.v1.BasicTables.DeleteBasicTable;
using FSH.Modules.Lookups.Features.v1.BasicTables.GetBasicTableById;
using FSH.Modules.Lookups.Features.v1.BasicTables.GetBasicTables;
using FSH.Modules.Lookups.Features.v1.BasicTables.UpdateBasicTable;
using FSH.Modules.Lookups.Features.v1.Geography;
using FSH.Modules.Lookups.Features.v1.Records.DeleteBasicRecord;
using FSH.Modules.Lookups.Features.v1.Records.GetBasicRecordsByCode;
using FSH.Modules.Lookups.Features.v1.Records.UpsertBasicRecords;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Lookups.LookupsModule), 520)]

namespace FSH.Modules.Lookups;

public sealed class LookupsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(LookupsPermissions.All);
        builder.Services.AddHeroDbContext<LookupsDbContext>();
        builder.Services.AddScoped<IDbInitializer, LookupsDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<LookupsDbContext>(name: "db:lookups", failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var tables = endpoints
            .MapGroup("api/v{version:apiVersion}/lookups/tables")
            .WithTags("Lookups - Basic Tables")
            .WithApiVersionSet(versionSet);
        // §18.4 #14: NO .RequireAuthorization() here — it overrides the global permission
        // FallbackPolicy and makes per-endpoint .RequirePermission() fail OPEN. The fallback
        // already enforces authentication + the RequiredPermission metadata.

        tables.MapGetBasicTablesEndpoint();
        tables.MapGetBasicTableByIdEndpoint();
        tables.MapCreateBasicTableEndpoint();
        tables.MapUpdateBasicTableEndpoint();
        tables.MapDeleteBasicTableEndpoint();
        tables.MapUpsertBasicRecordsEndpoint();
        tables.MapDeleteBasicRecordEndpoint();

        var records = endpoints
            .MapGroup("api/v{version:apiVersion}/lookups/records")
            .WithTags("Lookups - Records")
            .WithApiVersionSet(versionSet);
        // §18.4 #14: NO .RequireAuthorization() here — it overrides the global permission
        // FallbackPolicy and makes per-endpoint .RequirePermission() fail OPEN. The fallback
        // already enforces authentication + the RequiredPermission metadata.

        records.MapGetBasicRecordsByCodeEndpoint();

        var geography = endpoints
            .MapGroup("api/v{version:apiVersion}/geography")
            .WithTags("Lookups - Geography (DIVIPOLA)")
            .WithApiVersionSet(versionSet);
        // §18.4 #14: NO .RequireAuthorization() here — it overrides the global permission
        // FallbackPolicy and makes per-endpoint .RequirePermission() fail OPEN. The fallback
        // already enforces authentication + the RequiredPermission metadata.

        geography.MapGeographyEndpoints();
    }
}
