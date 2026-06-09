using System.Reflection;
using System.Text;
using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Data;

/// <summary>
/// Siembra el catálogo del tenant `global`: taxonomía de categorías de Google,
/// marcas canónicas e industrias/sectores con su mapeo a categorías raíz.
/// Idempotente. Solo se ejecuta dentro del contexto del tenant `global`.
/// </summary>
internal sealed class GlobalCatalogSeeder(CatalogDbContext db, ILogger logger)
{
    private const string TaxonomyResourceSuffix = "google-taxonomy.es-ES.txt";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedBrandsAsync(cancellationToken).ConfigureAwait(false);
        await SeedIndustriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedAliasesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ── Alias/sinónimos para la búsqueda inteligente ─────────────────────────
    private async Task SeedAliasesAsync(CancellationToken cancellationToken)
    {
        if (await db.CatalogAliases.AnyAsync(cancellationToken).ConfigureAwait(false)) return;

        // (alias, nombre canónico de la MARCA)
        var brandAliases = new[]
        {
            ("cannon", "Canon"), ("canón", "Canon"),
            ("nikkon", "Nikon"), ("nicon", "Nikon"),
            ("sonny", "Sony"), ("soni", "Sony"),
            ("gopro", "GoPro"), ("go pro", "GoPro"),
            ("black magic", "Blackmagic Design"), ("blackmagic", "Blackmagic Design"),
            ("dji", "DJI"), ("dyi", "DJI"),
            ("godox", "Godox"), ("nanlite", "Nanlite"),
        };
        // (alias, nombre EXACTO de la categoría Google)
        var categoryAliases = new[]
        {
            ("celular", "Teléfonos móviles"), ("celulares", "Teléfonos móviles"),
            ("movil", "Teléfonos móviles"), ("smartphone", "Teléfonos móviles"),
            ("camara", "Cámaras y ópticas"), ("cámaras", "Cámaras y ópticas"),
            ("audifonos", "Auriculares"), ("audífonos", "Auriculares"), ("auriculares", "Auriculares"),
            ("dron", "Drones y aviones no tripulados"), ("drone", "Drones y aviones no tripulados"),
            ("laptop", "Computadoras portátiles"), ("portatil", "Computadoras portátiles"),
            ("tele", "Televisores"), ("tv", "Televisores"),
        };

        var brandByName = await db.Brands.AsNoTracking()
            .ToDictionaryAsync(b => b.Name.ToLowerInvariant(), b => b.Id, cancellationToken).ConfigureAwait(false);
        // Categorías: resolver por nombre de hoja (puede haber repetidos → tomar el primero).
        var catByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in await db.Categories.AsNoTracking()
                     .Select(c => new { c.Id, c.Name }).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            catByName.TryAdd(c.Name, c.Id);
        }

        var aliases = new List<Domain.CatalogAlias>();
        foreach (var (alias, brand) in brandAliases)
            if (brandByName.TryGetValue(brand.ToLowerInvariant(), out var id))
                aliases.Add(Domain.CatalogAlias.Create(Contracts.Enums.CatalogAliasEntity.Brand, id, alias));
        foreach (var (alias, cat) in categoryAliases)
            if (catByName.TryGetValue(cat, out var id))
                aliases.Add(Domain.CatalogAlias.Create(Contracts.Enums.CatalogAliasEntity.Category, id, alias));

        if (aliases.Count > 0)
        {
            db.CatalogAliases.AddRange(aliases);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Seeded {Count} catalog aliases for the global tenant.", aliases.Count);
        }
    }

    // ── Categorías: taxonomía Google ─────────────────────────────────────────
    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        bool alreadyGoogle = await db.Categories
            .AnyAsync(c => c.GoogleCategoryId != null, cancellationToken).ConfigureAwait(false);
        if (alreadyGoogle) return; // ya sembrada

        // Truncate de las categorías del tenant global (destructivo, solo global).
        await db.Categories.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        var lines = await ReadResourceLinesAsync(cancellationToken).ConfigureAwait(false);

        // FullPath → (Guid, RootGoogleId). El archivo viene ordenado padre-antes-de-hijo.
        var byPath = new Dictionary<string, (Guid Id, int RootGoogleId)>(StringComparer.Ordinal);
        var batch = new List<Category>(lines.Count);
        int order = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            int dash = line.IndexOf(" - ", StringComparison.Ordinal);
            if (dash < 0) continue;
            if (!int.TryParse(line.AsSpan(0, dash), out int googleId)) continue;

            string fullPath = line[(dash + 3)..].Trim();
            var segments = fullPath.Split(" > ", StringSplitOptions.TrimEntries);
            string name = segments[^1];

            Guid? parentId = null;
            int rootGoogleId = googleId;
            if (segments.Length > 1)
            {
                string parentPath = string.Join(" > ", segments[..^1]);
                if (byPath.TryGetValue(parentPath, out var parent))
                {
                    parentId = parent.Id;
                    rootGoogleId = parent.RootGoogleId;
                }
            }

            string slug = $"{Slugify(name)}-{googleId}";
            var category = Category.FromGoogleTaxonomy(googleId, rootGoogleId, name, slug, fullPath, parentId, order++);
            byPath[fullPath] = (category.Id, rootGoogleId);
            batch.Add(category);
        }

        const int chunk = 1000;
        for (int i = 0; i < batch.Count; i += chunk)
        {
            db.Categories.AddRange(batch.GetRange(i, Math.Min(chunk, batch.Count - i)));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("[Catalog:global] seeded {Count} Google taxonomy categories", batch.Count);
    }

    // ── Marcas canónicas (lista curada, ampliable por CSV) ───────────────────
    private async Task SeedBrandsAsync(CancellationToken cancellationToken)
    {
        if (await db.Brands.AnyAsync(cancellationToken).ConfigureAwait(false)) return;

        (string Name, string Country)[] brands =
        [
            // Audiovisual / fotografía (vertical Maka)
            ("Sony","JP"),("Canon","JP"),("Nikon","JP"),("Fujifilm","JP"),("Panasonic","JP"),
            ("DJI","CN"),("Blackmagic Design","AU"),("GoPro","US"),("Godox","CN"),("Nanlite","CN"),
            ("Aputure","CN"),("DZOFilm","CN"),("Sigma","JP"),("Tamron","JP"),("Zhiyun","CN"),
            ("Manfrotto","IT"),("SanDisk","US"),("Sennheiser","DE"),("Rode","AU"),("Shure","US"),
            // Tecnología / electrónica
            ("Apple","US"),("Samsung","KR"),("Xiaomi","CN"),("Huawei","CN"),("Motorola","US"),
            ("LG","KR"),("Lenovo","CN"),("HP","US"),("Dell","US"),("Asus","TW"),("Acer","TW"),
            ("Logitech","CH"),("Intel","US"),("AMD","US"),("Nvidia","US"),("TP-Link","CN"),
            ("JBL","US"),("Anker","CN"),("Epson","JP"),("Bose","US"),
        ];

        db.Brands.AddRange(brands.Select(b =>
            Brand.Create(b.Name, Slugify(b.Name), countryOfOrigin: b.Country, ownerId: null)));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[Catalog:global] seeded {Count} canonical brands", brands.Length);
    }

    // ── Industrias/sectores → categorías raíz Google ─────────────────────────
    private async Task SeedIndustriesAsync(CancellationToken cancellationToken)
    {
        if (await db.Industries.AnyAsync(cancellationToken).ConfigureAwait(false)) return;

        // (Code, Name, [Google root ids]) — inferido de los 21 primeros niveles Google.
        (string Code, string Name, int[] Roots)[] industries =
        [
            ("AUDIOVISUAL","Cámaras y audiovisual", [141, 222]),
            ("ELECTRONICA","Electrónica y celulares", [222]),
            ("MODA","Moda y ropa", [166, 5181]),
            ("HOGAR","Hogar y jardín", [536, 436]),
            ("BELLEZA","Belleza y cuidado personal", [469]),
            ("JUGUETERIA","Juguetería", [1239]),
            ("PAPELERIA","Papelería y oficina", [922]),
            ("AUTOMOTRIZ","Automotriz", [888]),
            ("DEPORTES","Deportes", [988]),
            ("BEBES","Bebés y niños", [537]),
            ("ALIMENTOS","Alimentos y bebidas", [412]),
            ("MASCOTAS","Mascotas y animales", [1]),
            ("ARTE","Arte y multimedia", [8, 783]),
            ("SOFTWARE","Software y tecnología", [2092]),
            ("BRICOLAJE","Bricolaje y ferretería", [632]),
            ("INDUSTRIAL","Economía e industria", [111]),
        ];

        foreach (var (code, name, roots) in industries)
        {
            var industry = Industry.Create(code, name);
            foreach (int root in roots) industry.AddRoot(root);
            db.Industries.Add(industry);
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[Catalog:global] seeded {Count} industries", industries.Length);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static async Task<List<string>> ReadResourceLinesAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(TaxonomyResourceSuffix, StringComparison.OrdinalIgnoreCase))
            ?? TaxonomyResourceSuffix;
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string>(5600);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            lines.Add(line);
        return lines;
    }

    private static string Slugify(string value)
    {
        var sb = new StringBuilder(value.Length);
        bool lastDash = false;
        foreach (char c in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) { sb.Append(Normalize(c)); lastDash = false; }
            else if (!lastDash) { sb.Append('-'); lastDash = true; }
        }
        return sb.ToString().Trim('-');
    }

    private static char Normalize(char c) => c switch
    {
        'á' => 'a', 'é' => 'e', 'í' => 'i', 'ó' => 'o', 'ú' => 'u', 'ü' => 'u', 'ñ' => 'n',
        _ => c,
    };
}
