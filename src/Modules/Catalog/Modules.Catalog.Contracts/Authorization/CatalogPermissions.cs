using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Catalog.Contracts.Authorization;

public static class CatalogPermissions
{
    public static class Brands
    {
        public const string Resource = "Catalog.Brands";
        public const string View    = $"Permissions.{Resource}.View";
        public const string Create  = $"Permissions.{Resource}.Create";
        public const string Update  = $"Permissions.{Resource}.Update";
        public const string Delete  = $"Permissions.{Resource}.Delete";
        public const string Restore = $"Permissions.{Resource}.Restore";
    }

    public static class Categories
    {
        public const string Resource = "Catalog.Categories";
        public const string View    = $"Permissions.{Resource}.View";
        public const string Create  = $"Permissions.{Resource}.Create";
        public const string Update  = $"Permissions.{Resource}.Update";
        public const string Delete  = $"Permissions.{Resource}.Delete";
        public const string Restore = $"Permissions.{Resource}.Restore";
    }

    public static class Products
    {
        public const string Resource = "Catalog.Products";
        public const string View        = $"Permissions.{Resource}.View";
        public const string Create      = $"Permissions.{Resource}.Create";
        public const string Update      = $"Permissions.{Resource}.Update";
        public const string Delete      = $"Permissions.{Resource}.Delete";
        public const string Restore     = $"Permissions.{Resource}.Restore";
        public const string Publish     = $"Permissions.{Resource}.Publish";
        public const string Archive     = $"Permissions.{Resource}.Archive";
        public const string AdjustStock = $"Permissions.{Resource}.AdjustStock";
    }

    public static class PriceLists
    {
        public const string Resource = "Catalog.PriceLists";
        public const string View        = $"Permissions.{Resource}.View";
        public const string Manage      = $"Permissions.{Resource}.Manage";
        public const string ApproveBulk = $"Permissions.{Resource}.ApproveBulk";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Brands",    ActionConstants.View,   Brands.Resource, IsBasic: true),
        new("Create Brands",  ActionConstants.Create, Brands.Resource),
        new("Update Brands",  ActionConstants.Update, Brands.Resource),
        new("Delete Brands",  ActionConstants.Delete, Brands.Resource),
        new("Restore Brands", "Restore",              Brands.Resource),

        new("View Categories",    ActionConstants.View,   Categories.Resource, IsBasic: true),
        new("Create Categories",  ActionConstants.Create, Categories.Resource),
        new("Update Categories",  ActionConstants.Update, Categories.Resource),
        new("Delete Categories",  ActionConstants.Delete, Categories.Resource),
        new("Restore Categories", "Restore",              Categories.Resource),

        new("View Products",        ActionConstants.View,   Products.Resource, IsBasic: true),
        new("Create Products",      ActionConstants.Create, Products.Resource),
        new("Update Products",      ActionConstants.Update, Products.Resource),
        new("Delete Products",      ActionConstants.Delete, Products.Resource),
        new("Restore Products",     "Restore",              Products.Resource),
        new("Publish Products",     "Publish",              Products.Resource),
        new("Archive Products",     "Archive",              Products.Resource),
        new("Adjust Product Stock", "AdjustStock",          Products.Resource),

        new("View Price Lists",     ActionConstants.View, PriceLists.Resource, IsBasic: true),
        new("Manage Price Lists",   "Manage",             PriceLists.Resource),
        new("Approve Bulk Prices",  "ApproveBulk",        PriceLists.Resource),
    ];
}
