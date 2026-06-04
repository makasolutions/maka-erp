using System.Text;

namespace FSH.Modules.Catalog.Extensions;

internal static class SlugHelper
{
    internal static string Build(string? inputSlug, string name)
    {
        if (!string.IsNullOrWhiteSpace(inputSlug))
            return inputSlug.ToLowerInvariant().Trim();

        string normalized = name.Normalize(NormalizationForm.FormD);
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
