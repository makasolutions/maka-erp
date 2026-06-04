using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;
using FSH.Modules.Catalog.Features.v1.Brands.DeleteBrand;
using FSH.Modules.Catalog.Features.v1.Brands.GetBrandById;
using FSH.Modules.Catalog.Features.v1.Brands.GetBrands;
using FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands;
using FSH.Modules.Catalog.Features.v1.Brands.RestoreBrand;
using FSH.Modules.Catalog.Features.v1.Brands.UpdateBrand;
using FSH.Modules.Catalog.Features.v1.Categories.CreateCategory;
using FSH.Modules.Catalog.Features.v1.Categories.DeleteCategory;
using FSH.Modules.Catalog.Features.v1.Categories.GetCategories;
using FSH.Modules.Catalog.Features.v1.Categories.GetCategoryById;
using FSH.Modules.Catalog.Features.v1.Categories.ListTrashedCategories;
using FSH.Modules.Catalog.Features.v1.Categories.RestoreCategory;
using FSH.Modules.Catalog.Features.v1.Categories.UpdateCategory;
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
        brands.MapGetBrandByIdEndpoint();
        brands.MapCreateBrandEndpoint();
        brands.MapUpdateBrandEndpoint();
        brands.MapDeleteBrandEndpoint();
        brands.MapRestoreBrandEndpoint();
        brands.MapListTrashedBrandsEndpoint();

        var categories = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/categories")
            .WithTags("Catalog - Categories")
            .WithApiVersionSet(apiVersionSet);

        categories.MapListTrashedCategoriesEndpoint();
        categories.MapGetCategoriesEndpoint();
        categories.MapGetCategoryByIdEndpoint();
        categories.MapCreateCategoryEndpoint();
        categories.MapUpdateCategoryEndpoint();
        categories.MapDeleteCategoryEndpoint();
        categories.MapRestoreCategoryEndpoint();
    }
}
