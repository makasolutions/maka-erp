using System.Net;
using System.Text;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands.CreateBrand;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;

public sealed class CreateBrandCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateBrandCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string slug = BuildSlug(command.Slug, command.Name);

        bool slugExists = await db.Brands
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.Slug == slug)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
            throw new CustomException("El slug ya existe.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var brand = Brand.Create(
            command.Name,
            slug,
            command.Description,
            command.LogoUrl,
            command.WebsiteUrl,
            command.CountryOfOrigin,
            isActive: command.IsActive);

        db.Brands.Add(brand);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return brand.Id;
    }

    internal static string BuildSlug(string? inputSlug, string name)
    {
        if (!string.IsNullOrWhiteSpace(inputSlug))
            return inputSlug.ToLowerInvariant().Trim();

        // Normalizar: quitar diacríticos, reemplazar espacios por guiones
        string normalized = name
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            else if (c == ' ' || c == '-' || c == '_') sb.Append('-');
        }

        return sb.ToString().Trim('-');
    }
}
