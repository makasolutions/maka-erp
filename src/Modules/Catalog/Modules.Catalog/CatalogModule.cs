using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Features.v1.Brands.GetBrands;
using FSH.Modules.Catalog.Features.v1.Categories.GetCategories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Catalog.CatalogModule), 600)]

namespace FSH.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(CatalogPermissions.All);

        builder.Services.AddHeroDbContext<CatalogDbContext>();
        builder.Services.AddScoped<IDbInitializer, CatalogDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<CatalogDbContext>(
                name: "db:catalog",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        // No custom middleware needed
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var brands = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/brands")
            .WithTags("Catalog - Brands")
            .WithApiVersionSet(apiVersionSet);

        brands.MapGetBrandsEndpoint();

        var categories = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/categories")
            .WithTags("Catalog - Categories")
            .WithApiVersionSet(apiVersionSet);

        categories.MapGetCategoriesEndpoint();
    }
}
