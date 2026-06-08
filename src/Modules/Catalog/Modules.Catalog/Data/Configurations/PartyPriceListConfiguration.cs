using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class PartyPriceListConfiguration : IEntityTypeConfiguration<PartyPriceList>
{
    public void Configure(EntityTypeBuilder<PartyPriceList> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyPriceLists");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PartyId);
        builder.HasIndex(x => new { x.PartyId, x.PriceListId }).IsUnique();
    }
}
