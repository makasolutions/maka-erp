using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Catalog.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Catalog.Features.v1.Agreements;
using FSH.Modules.Catalog.Features.v1.GlobalCatalog;
using FSH.Modules.Catalog.Features.v1.PartyPriceLists;
using FSH.Modules.Catalog.Features.v1.Suppliers;
using FSH.Modules.Catalog.Features.v1.Attributes.AddAttributeValue;
using FSH.Modules.Catalog.Features.v1.Attributes.CreateAttribute;
using FSH.Modules.Catalog.Features.v1.Attributes.DeleteAttribute;
using FSH.Modules.Catalog.Features.v1.Attributes.GetAttributeById;
using FSH.Modules.Catalog.Features.v1.Attributes.GetAttributes;
using FSH.Modules.Catalog.Features.v1.Attributes.RemoveAttributeValue;
using FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttribute;
using FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttributeValue;
using FSH.Modules.Catalog.Features.v1.Bundles.AddBundleItem;
using FSH.Modules.Catalog.Features.v1.Bundles.GetBundleItems;
using FSH.Modules.Catalog.Features.v1.Bundles.RemoveBundleItem;
using FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;
using FSH.Modules.Catalog.Features.v1.Brands.DeleteBrand;
using FSH.Modules.Catalog.Features.v1.Brands.GetBrandById;
using FSH.Modules.Catalog.Features.v1.Brands.GetBrands;
using FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands;
using FSH.Modules.Catalog.Features.v1.Brands.RestoreBrand;
using FSH.Modules.Catalog.Features.v1.Brands.UpdateBrand;
using FSH.Modules.Catalog.Features.v1.Categories.CreateCategory;
using FSH.Modules.Catalog.Features.v1.Categories.DeleteCategory;
using FSH.Modules.Catalog.Features.v1.Products.ArchiveProduct;
using FSH.Modules.Catalog.Features.v1.Products.CreateProduct;
using FSH.Modules.Catalog.Features.v1.Products.DeleteProduct;
using FSH.Modules.Catalog.Features.v1.Products.GetProductById;
using FSH.Modules.Catalog.Features.v1.Products.GetProducts;
using FSH.Modules.Catalog.Features.v1.Products.GetPublicProduct;
using FSH.Modules.Catalog.Features.v1.Products.ListTrashedProducts;
using FSH.Modules.Catalog.Features.v1.Products.PublishProduct;
using FSH.Modules.Catalog.Features.v1.Products.RestoreProduct;
using FSH.Modules.Catalog.Features.v1.Products.SetProductAttributes;
using FSH.Modules.Catalog.Features.v1.Products.ChangeProductType;
using FSH.Modules.Catalog.Features.v1.Products.DuplicateProduct;
using FSH.Modules.Catalog.Features.v1.Marketplaces.GetCategoryRequirements;
using FSH.Modules.Catalog.Features.v1.Marketplaces.SetCategoryRequirements;
using FSH.Modules.Catalog.Features.v1.Marketplaces.GetProductMarketplaceValidation;
using FSH.Modules.Catalog.Features.v1.Marketplaces.GetCategoryCoverageReport;
using FSH.Modules.Catalog.Features.v1.Products.SetProductCategories;
using FSH.Modules.Catalog.Features.v1.Products.UpdateProduct;
using FSH.Modules.Catalog.Features.v1.ProductImages.AddProductImage;
using FSH.Modules.Catalog.Features.v1.ProductImages.GetProductImages;
using FSH.Modules.Catalog.Features.v1.ProductImages.RemoveProductImage;
using FSH.Modules.Catalog.Features.v1.ProductImages.SetPrimaryImage;
using FSH.Modules.Catalog.Features.v1.ProductTags.GetProductTags;
using FSH.Modules.Catalog.Features.v1.ProductTags.SetProductTags;
using FSH.Modules.Catalog.Features.v1.ProductCodes.AddProductCode;
using FSH.Modules.Catalog.Features.v1.ProductCodes.GetProductCodes;
using FSH.Modules.Catalog.Features.v1.ProductCodes.RemoveProductCode;
using FSH.Modules.Catalog.Features.v1.Variations.AddVariation;
using FSH.Modules.Catalog.Features.v1.Variations.GenerateVariations;
using FSH.Modules.Catalog.Features.v1.Variations.DeleteVariation;
using FSH.Modules.Catalog.Features.v1.Variations.GetVariationsByProduct;
using FSH.Modules.Catalog.Features.v1.Variations.RestoreVariation;
using FSH.Modules.Catalog.Features.v1.Variations.UpdateVariation;
using FSH.Modules.Catalog.Features.v1.Categories.GetCategories;
using FSH.Modules.Catalog.Features.v1.Categories.GetCategoryById;
using FSH.Modules.Catalog.Features.v1.Categories.ListTrashedCategories;
using FSH.Modules.Catalog.Features.v1.Categories.RestoreCategory;
using FSH.Modules.Catalog.Features.v1.Categories.UpdateCategory;
using FSH.Modules.Catalog.Features.v1.PriceLists.AddPriceListItem;
using FSH.Modules.Catalog.Features.v1.PriceLists.CreatePriceList;
using FSH.Modules.Catalog.Features.v1.PriceLists.UpdatePriceList;
using FSH.Modules.Catalog.Features.v1.Campaigns.CreateCampaign;
using FSH.Modules.Catalog.Features.v1.Campaigns.UpdateCampaign;
using FSH.Modules.Catalog.Features.v1.Campaigns.SetCampaignItems;
using FSH.Modules.Catalog.Features.v1.Campaigns.CancelCampaign;
using FSH.Modules.Catalog.Features.v1.PriceLists.GetPriceListById;
using FSH.Modules.Catalog.Features.v1.PriceLists.GetPriceLists;
using FSH.Modules.Catalog.Features.v1.PriceLists.UpdatePriceListItem;
using FSH.Modules.Catalog.Features.v1.Prices.GetEffectivePrice;
using FSH.Modules.Catalog.Features.v1.Prices.GetPriceHistory;
using FSH.Modules.Catalog.Features.v1.PriceProposals.ApprovePriceProposals;
using FSH.Modules.Catalog.Features.v1.PriceProposals.BulkUpdatePrices;
using FSH.Modules.Catalog.Features.v1.PriceProposals.GetPriceProposals;
using FSH.Modules.Catalog.Features.v1.PriceProposals.RejectPriceProposals;
using FSH.Modules.Catalog.Features.v1.TaxRates.CreateTaxRate;
using FSH.Modules.Catalog.Features.v1.TaxRates.DeleteTaxRate;
using FSH.Modules.Catalog.Features.v1.TaxRates.GetTaxRates;
using FSH.Modules.Catalog.Features.v1.TaxRates.UpdateTaxRate;
using FSH.Modules.Catalog.Features.v1.ShippingClasses.CreateShippingClass;
using FSH.Modules.Catalog.Features.v1.ShippingClasses.DeleteShippingClass;
using FSH.Modules.Catalog.Features.v1.ShippingClasses.GetShippingClasses;
using FSH.Modules.Catalog.Features.v1.ShippingClasses.UpdateShippingClass;
using FSH.Modules.Catalog.Features.v1.TenantProducts.CloneProduct;
using FSH.Modules.Catalog.Features.v1.TenantProducts.GetResolvedProduct;
using FSH.Modules.Catalog.Features.v1.TenantProducts.UpdateTenantProduct;
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
        builder.Services.AddScoped<Data.IGlobalCatalogReader, Data.GlobalCatalogReader>();
        builder.Services.AddScoped<Services.CampaignJob>();

        // File access policies for Catalog owner types (product/brand/category images).
        // Without these the Files module returns 403 "No file access policy registered".
        builder.Services.AddScoped<IFileAccessPolicy>(_ => new CatalogFileAccessPolicy("Product"));
        builder.Services.AddScoped<IFileAccessPolicy>(_ => new CatalogFileAccessPolicy("Brand"));
        builder.Services.AddScoped<IFileAccessPolicy>(_ => new CatalogFileAccessPolicy("Category"));

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

        var global = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/global")
            .WithTags("Catalog - Global")
            .WithApiVersionSet(apiVersionSet);
        global.MapGlobalCatalogEndpoints();

        var partyPriceLists = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/party-price-lists")
            .WithTags("Catalog - Party price lists")
            .WithApiVersionSet(apiVersionSet);
        partyPriceLists.MapPartyPriceListEndpoints();

        var suppliers = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/suppliers")
            .WithTags("Catalog - Suppliers")
            .WithApiVersionSet(apiVersionSet);
        suppliers.MapSupplierMappingEndpoints();

        var agreements = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/agreements")
            .WithTags("Catalog - Agreements")
            .WithApiVersionSet(apiVersionSet);
        agreements.MapAgreementEndpoints();

        var products = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/products")
            .WithTags("Catalog - Products")
            .WithApiVersionSet(apiVersionSet);

        products.MapListTrashedProductsEndpoint();
        products.MapGetProductsEndpoint();
        products.MapGetProductByIdEndpoint();
        products.MapCreateProductEndpoint();
        products.MapUpdateProductEndpoint();
        products.MapDeleteProductEndpoint();
        products.MapRestoreProductEndpoint();
        products.MapPublishProductEndpoint();
        products.MapArchiveProductEndpoint();
        products.MapSetProductCategoriesEndpoint();
        products.MapSetProductAttributesEndpoint();
        products.MapGetProductMarketplaceValidationEndpoint();
        products.MapChangeProductTypeEndpoint();
        products.MapDuplicateProductEndpoint();

        var variations = products
            .MapGroup("/{productId:guid}/variations")
            .WithTags("Catalog - Variations");

        variations.MapGetVariationsByProductEndpoint();
        variations.MapGenerateVariationsEndpoint();
        variations.MapAddVariationEndpoint();
        variations.MapUpdateVariationEndpoint();
        variations.MapDeleteVariationEndpoint();
        variations.MapRestoreVariationEndpoint();

        var images = products
            .MapGroup("/{productId:guid}/images")
            .WithTags("Catalog - Product Images");

        images.MapGetProductImagesEndpoint();
        images.MapAddProductImageEndpoint();
        images.MapRemoveProductImageEndpoint();
        images.MapSetPrimaryImageEndpoint();

        var tags = products
            .MapGroup("/{productId:guid}/tags")
            .WithTags("Catalog - Product Tags");

        tags.MapGetProductTagsEndpoint();
        tags.MapSetProductTagsEndpoint();

        var bundleItems = products
            .MapGroup("/{productId:guid}/bundle-items")
            .WithTags("Catalog - Bundle Items");

        bundleItems.MapGetBundleItemsEndpoint();
        bundleItems.MapAddBundleItemEndpoint();
        bundleItems.MapRemoveBundleItemEndpoint();

        var codes = variations
            .MapGroup("/{variationId:guid}/codes")
            .WithTags("Catalog - Product Codes");

        codes.MapGetProductCodesEndpoint();
        codes.MapAddProductCodeEndpoint();
        codes.MapRemoveProductCodeEndpoint();
        categories.MapGetCategoryByIdEndpoint();
        categories.MapCreateCategoryEndpoint();
        categories.MapUpdateCategoryEndpoint();
        categories.MapDeleteCategoryEndpoint();
        categories.MapRestoreCategoryEndpoint();
        categories.MapGetCategoryRequirementsEndpoint();
        categories.MapSetCategoryRequirementsEndpoint();
        categories.MapGetCategoryCoverageReportEndpoint();

        var priceLists = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/price-lists")
            .WithTags("Catalog - Price Lists")
            .WithApiVersionSet(apiVersionSet);

        priceLists.MapGetPriceListsEndpoint();
        priceLists.MapGetPriceListByIdEndpoint();
        priceLists.MapCreatePriceListEndpoint();
        priceLists.MapUpdatePriceListEndpoint();
        priceLists.MapAddPriceListItemEndpoint();
        priceLists.MapUpdatePriceListItemEndpoint();

        var campaigns = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/campaigns")
            .WithTags("Catalog - Campaigns")
            .WithApiVersionSet(apiVersionSet);

        campaigns.MapCreateCampaignEndpoint();
        campaigns.MapUpdateCampaignEndpoint();
        campaigns.MapSetCampaignItemsEndpoint();
        campaigns.MapCancelCampaignEndpoint();

        var prices = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/prices")
            .WithTags("Catalog - Prices")
            .WithApiVersionSet(apiVersionSet);

        prices.MapGetEffectivePriceEndpoint();
        prices.MapGetPriceHistoryEndpoint();

        // Bulk import lives under the price-lists group (POST /{id}/bulk-import).
        priceLists.MapImportPricesEndpoint();

        var attributes = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/attributes")
            .WithTags("Catalog - Attributes")
            .WithApiVersionSet(apiVersionSet);

        attributes.MapGetAttributesEndpoint();
        attributes.MapGetAttributeByIdEndpoint();
        attributes.MapCreateAttributeEndpoint();
        attributes.MapUpdateAttributeEndpoint();
        attributes.MapDeleteAttributeEndpoint();

        var attributeValues = attributes
            .MapGroup("/{attributeId:guid}/values")
            .WithTags("Catalog - Attribute Values");

        attributeValues.MapAddAttributeValueEndpoint();
        attributeValues.MapUpdateAttributeValueEndpoint();
        attributeValues.MapRemoveAttributeValueEndpoint();

        var priceProposals = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/price-proposals")
            .WithTags("Catalog - Price Proposals")
            .WithApiVersionSet(apiVersionSet);

        priceProposals.MapGetPriceProposalsEndpoint();
        priceProposals.MapApprovePriceProposalsEndpoint();
        priceProposals.MapRejectPriceProposalsEndpoint();

        var taxRates = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/tax-rates")
            .WithTags("Catalog - Tax Rates")
            .WithApiVersionSet(apiVersionSet);

        taxRates.MapGetTaxRatesEndpoint();
        taxRates.MapCreateTaxRateEndpoint();
        taxRates.MapUpdateTaxRateEndpoint();
        taxRates.MapDeleteTaxRateEndpoint();

        var shippingClasses = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/shipping-classes")
            .WithTags("Catalog - Shipping Classes")
            .WithApiVersionSet(apiVersionSet);

        shippingClasses.MapGetShippingClassesEndpoint();
        shippingClasses.MapCreateShippingClassEndpoint();
        shippingClasses.MapUpdateShippingClassEndpoint();
        shippingClasses.MapDeleteShippingClassEndpoint();

        // Public (anonymous) shareable product sheet — tenant resolved from the 'tenant' header.
        var publicProducts = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/public/products")
            .WithTags("Catalog - Public")
            .WithApiVersionSet(apiVersionSet);

        publicProducts.MapGetPublicProductEndpoint();

        var tenantProducts = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog/tenant-products")
            .WithTags("Catalog - Tenant Products")
            .WithApiVersionSet(apiVersionSet);

        tenantProducts.MapCloneProductToTenantEndpoint();
        tenantProducts.MapUpdateTenantProductEndpoint();
        tenantProducts.MapGetResolvedProductEndpoint();
    }
}
