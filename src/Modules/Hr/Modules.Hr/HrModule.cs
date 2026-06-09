using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Hr.Contracts.Authorization;
using FSH.Modules.Hr.Data;
using FSH.Modules.Hr.Features.v1.Employees;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Hr.HrModule), 560)]

namespace FSH.Modules.Hr;

public sealed class HrModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(HrPermissions.All);
        builder.Services.AddHeroDbContext<HrDbContext>();
        builder.Services.AddScoped<IDbInitializer, HrDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<HrDbContext>(name: "db:hr", failureStatus: HealthStatus.Unhealthy);
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
            .MapGroup("api/v{version:apiVersion}/hr/employees")
            .WithTags("Hr")
            .WithApiVersionSet(versionSet);
        // §18.4 #14: NO .RequireAuthorization() here — it overrides the global permission
        // FallbackPolicy and makes per-endpoint .RequirePermission() fail OPEN. The fallback
        // already enforces authentication + the RequiredPermission metadata.

        group.MapGetEmployeesEndpoint();
        group.MapGetEmployeeByPartyIdEndpoint();
        group.MapCreateEmployeeEndpoint();
        group.MapUpdateEmployeeEndpoint();
        group.MapDeleteEmployeeEndpoint();
    }
}
