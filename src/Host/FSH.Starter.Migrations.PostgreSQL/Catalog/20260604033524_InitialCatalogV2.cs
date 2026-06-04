using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class InitialCatalogV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "Brands",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    WooCommerceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogAttributes",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsVisibleOnProduct = table.Column<bool>(type: "boolean", nullable: false),
                    IsUsedForVariations = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WooCommerceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    WooCommerceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CustomerSegment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    TechnicalSpecs = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Specs = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxRateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShippingClassId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    IsDownloadable = table.Column<bool>(type: "boolean", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    WeightUnit = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    DimensionLength = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DimensionWidth = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DimensionHeight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DimensionUnit = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    SeoTitle = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    SeoDescription = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SeoKeywords = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    WooCommerceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingClasses",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierBrands",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsExclusiveDistributor = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierProducts",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    CostCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    IsPreferredSupplier = table.Column<bool>(type: "boolean", nullable: false),
                    LastPriceUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantProducts",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    NameOverride = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShortDescriptionOverride = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DescriptionOverride = table.Column<string>(type: "text", nullable: true),
                    TechnicalSpecsOverride = table.Column<string>(type: "text", nullable: true),
                    SeoTitleOverride = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    SeoDescriptionOverride = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    DropshippingPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DropshippingMinQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogAttributeValues",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ColorCode = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    WooCommerceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogAttributeValues_CatalogAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalSchema: "catalog",
                        principalTable: "CatalogAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MinQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SalePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SalePriceFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalePriceTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "catalog",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsUsedForVariations = table.Column<bool>(type: "boolean", nullable: false),
                    IsVisibleOnProduct = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributes_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductBundleItems",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemVariationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    DiscountFixed = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBundleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AltText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductTags",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Color = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTags_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariations",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    WeightUnit = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    DimensionLength = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DimensionWidth = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DimensionHeight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ManageStock = table.Column<bool>(type: "boolean", nullable: false),
                    AllowBackorders = table.Column<bool>(type: "boolean", nullable: false),
                    SoldIndividually = table.Column<bool>(type: "boolean", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: true),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    WooCommerceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariations_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantProductImages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AltText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantProductImages_TenantProducts_TenantProductId",
                        column: x => x.TenantProductId,
                        principalSchema: "catalog",
                        principalTable: "TenantProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItemHistory",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OldSalePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    NewSalePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    OldSalePriceFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewSalePriceFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OldSalePriceTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewSalePriceTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItemHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItemHistory_PriceListItems_PriceListItemId",
                        column: x => x.PriceListItemId,
                        principalSchema: "catalog",
                        principalTable: "PriceListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValues",
                schema: "catalog",
                columns: table => new
                {
                    ProductAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValues", x => new { x.ProductAttributeId, x.AttributeValueId });
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_CatalogAttributeValues_AttributeValu~",
                        column: x => x.AttributeValueId,
                        principalSchema: "catalog",
                        principalTable: "CatalogAttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalSchema: "catalog",
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCodes",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CodeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCodes_ProductVariations_VariationId",
                        column: x => x.VariationId,
                        principalSchema: "catalog",
                        principalTable: "ProductVariations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductCodes_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariationAttributeValues",
                schema: "catalog",
                columns: table => new
                {
                    VariationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariationAttributeValues", x => new { x.VariationId, x.AttributeValueId });
                    table.ForeignKey(
                        name: "FK_VariationAttributeValues_CatalogAttributeValues_AttributeVa~",
                        column: x => x.AttributeValueId,
                        principalSchema: "catalog",
                        principalTable: "CatalogAttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VariationAttributeValues_ProductVariations_VariationId",
                        column: x => x.VariationId,
                        principalSchema: "catalog",
                        principalTable: "ProductVariations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_IsDeleted",
                schema: "catalog",
                table: "Brands",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_OwnerId",
                schema: "catalog",
                table: "Brands",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "catalog",
                table: "Brands",
                columns: new[] { "Slug", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogAttributes_OwnerId",
                schema: "catalog",
                table: "CatalogAttributes",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogAttributeValues_AttributeId",
                schema: "catalog",
                table: "CatalogAttributeValues",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsDeleted",
                schema: "catalog",
                table: "Categories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OwnerId",
                schema: "catalog",
                table: "Categories",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                schema: "catalog",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug_ParentId",
                schema: "catalog",
                table: "Categories",
                columns: new[] { "Slug", "ParentId", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItemHistory_PriceListItemId",
                schema: "catalog",
                table: "PriceListItemHistory",
                column: "PriceListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItemHistory_VariationId",
                schema: "catalog",
                table: "PriceListItemHistory",
                column: "VariationId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_PriceListId_VariationId",
                schema: "catalog",
                table: "PriceListItems",
                columns: new[] { "PriceListId", "VariationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_VariationId",
                schema: "catalog",
                table: "PriceListItems",
                column: "VariationId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_CustomerSegment_OwnerId_IsActive",
                schema: "catalog",
                table: "PriceLists",
                columns: new[] { "CustomerSegment", "OwnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_AttributeId",
                schema: "catalog",
                table: "ProductAttributes",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId",
                schema: "catalog",
                table: "ProductAttributes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_AttributeValueId",
                schema: "catalog",
                table: "ProductAttributeValues",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItems_ItemVariationId",
                schema: "catalog",
                table: "ProductBundleItems",
                column: "ItemVariationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItems_ProductId",
                schema: "catalog",
                table: "ProductBundleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_CategoryId",
                schema: "catalog",
                table: "ProductCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ProductId_CategoryId",
                schema: "catalog",
                table: "ProductCategories",
                columns: new[] { "ProductId", "CategoryId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCodes_ProductId",
                schema: "catalog",
                table: "ProductCodes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCodes_SupplierId_Code",
                schema: "catalog",
                table: "ProductCodes",
                columns: new[] { "SupplierId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCodes_VariationId_CodeType_Code",
                schema: "catalog",
                table: "ProductCodes",
                columns: new[] { "VariationId", "CodeType", "Code", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                schema: "catalog",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                schema: "catalog",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                schema: "catalog",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Products_OwnerId",
                schema: "catalog",
                table: "Products",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                schema: "catalog",
                table: "Products",
                columns: new[] { "Slug", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Status",
                schema: "catalog",
                table: "Products",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TaxRateId",
                schema: "catalog",
                table: "Products",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Type",
                schema: "catalog",
                table: "Products",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTags_ProductId",
                schema: "catalog",
                table: "ProductTags",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariations_IsDeleted",
                schema: "catalog",
                table: "ProductVariations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariations_ProductId",
                schema: "catalog",
                table: "ProductVariations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariations_Sku",
                schema: "catalog",
                table: "ProductVariations",
                columns: new[] { "Sku", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingClasses_OwnerId",
                schema: "catalog",
                table: "ShippingClasses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBrands_BrandId",
                schema: "catalog",
                table: "SupplierBrands",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBrands_SupplierId",
                schema: "catalog",
                table: "SupplierBrands",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProducts_ProductId",
                schema: "catalog",
                table: "SupplierProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProducts_SupplierId",
                schema: "catalog",
                table: "SupplierProducts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProducts_VariationId",
                schema: "catalog",
                table: "SupplierProducts",
                column: "VariationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_OwnerId",
                schema: "catalog",
                table: "TaxRates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantProductImages_TenantProductId",
                schema: "catalog",
                table: "TenantProductImages",
                column: "TenantProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantProducts_CanonicalProductId",
                schema: "catalog",
                table: "TenantProducts",
                column: "CanonicalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_VariationAttributeValues_AttributeValueId",
                schema: "catalog",
                table: "VariationAttributeValues",
                column: "AttributeValueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Brands",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "PriceListItemHistory",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductBundleItems",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductCategories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductCodes",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductImages",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductTags",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ShippingClasses",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "SupplierBrands",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "SupplierProducts",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "TaxRates",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "TenantProductImages",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "VariationAttributeValues",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "PriceListItems",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductAttributes",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "TenantProducts",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "CatalogAttributeValues",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ProductVariations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "PriceLists",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "CatalogAttributes",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "catalog");
        }
    }
}
