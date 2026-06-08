namespace FSH.Framework.Shared.Multitenancy;

public static class MultitenancyConstants
{
    public static class Root
    {
        public const string Id = "root";
        public const string Name = "Root";
        public const string EmailAddress = "admin@root.com";
        public const string DefaultProfilePicture = "assets/defaults/profile-picture.webp";
        public const string Issuer = "mukesh.murugan";
    }

    /// <summary>
    /// Platform-wide "global" tenant: holds the shared marketplace catalog
    /// (category taxonomy, canonical brands, industries, published dropshipping
    /// products, global suppliers, public warehouse stock) read across tenants.
    /// Not a customer tenant — provisioned by the migrator alongside root.
    /// </summary>
    public static class Global
    {
        public const string Id = "global";
        public const string Name = "Global";
        public const string EmailAddress = "admin@global.com";
        public const string Issuer = "maka.global";
    }

    public const string Identifier = "tenant";
    public const string Schema = "tenant";
}