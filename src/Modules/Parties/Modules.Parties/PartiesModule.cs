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
using FSH.Modules.Parties.Features.v1.Parties.UpdateParty;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapGetPartiesEndpoint();
        group.MapGetPartyByIdEndpoint();
        group.MapCreatePartyEndpoint();
        group.MapUpdatePartyEndpoint();
        group.MapSetPartyRolesEndpoint();
        group.MapDeletePartyEndpoint();
        group.MapRestorePartyEndpoint();
    }
}
