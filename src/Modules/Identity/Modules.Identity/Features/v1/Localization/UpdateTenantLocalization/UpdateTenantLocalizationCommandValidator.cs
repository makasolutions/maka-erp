using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Localization.UpdateTenantLocalization;

namespace FSH.Modules.Identity.Features.v1.Localization.UpdateTenantLocalization;

public sealed class UpdateTenantLocalizationCommandValidator : AbstractValidator<UpdateTenantLocalizationCommand>
{
    // Curated list of IANA timezone identifiers supported by the UI.
    private static readonly HashSet<string> AllowedTimezones =
    [
        // UTC
        "UTC",
        // América Latina
        "America/Bogota", "America/Lima", "America/Guayaquil",
        "America/Panama", "America/Costa_Rica", "America/Managua",
        "America/Tegucigalpa", "America/El_Salvador", "America/Guatemala",
        "America/Mexico_City", "America/Monterrey", "America/Merida",
        "America/Cancun", "America/Havana", "America/Port-au-Prince",
        "America/Santo_Domingo", "America/Puerto_Rico", "America/Jamaica",
        "America/Caracas", "America/La_Paz", "America/Manaus",
        "America/Belem", "America/Fortaleza", "America/Recife",
        "America/Maceio", "America/Bahia", "America/Sao_Paulo",
        "America/Cuiaba", "America/Porto_Velho", "America/Boa_Vista",
        "America/Rio_Branco", "America/Noronha", "America/Santiago",
        "America/Asuncion", "America/Montevideo", "America/Buenos_Aires",
        "America/Cordoba", "America/Mendoza", "America/Argentina/San_Juan",
        // Estados Unidos
        "America/New_York", "America/Chicago", "America/Denver",
        "America/Los_Angeles", "America/Anchorage", "America/Adak",
        "Pacific/Honolulu", "America/Phoenix",
        // Europa
        "Europe/London", "Europe/Lisbon", "Europe/Madrid",
        "Europe/Paris", "Europe/Berlin", "Europe/Rome",
        "Europe/Amsterdam", "Europe/Brussels", "Europe/Zurich",
        "Europe/Vienna", "Europe/Warsaw", "Europe/Prague",
        "Europe/Budapest", "Europe/Bucharest", "Europe/Sofia",
        "Europe/Athens", "Europe/Helsinki", "Europe/Stockholm",
        "Europe/Oslo", "Europe/Copenhagen", "Europe/Dublin",
        "Europe/Istanbul", "Europe/Moscow",
        // Asia / Medio Oriente
        "Asia/Dubai", "Asia/Riyadh", "Asia/Kuwait",
        "Asia/Baghdad", "Asia/Tehran", "Asia/Karachi",
        "Asia/Kolkata", "Asia/Dhaka", "Asia/Colombo",
        "Asia/Kathmandu", "Asia/Rangoon", "Asia/Bangkok",
        "Asia/Jakarta", "Asia/Singapore", "Asia/Kuala_Lumpur",
        "Asia/Hong_Kong", "Asia/Shanghai", "Asia/Taipei",
        "Asia/Tokyo", "Asia/Seoul",
        // África
        "Africa/Lagos", "Africa/Cairo", "Africa/Nairobi",
        "Africa/Johannesburg",
        // Oceanía
        "Australia/Sydney", "Australia/Melbourne", "Australia/Brisbane",
        "Australia/Perth", "Pacific/Auckland",
    ];

    private static readonly HashSet<string> AllowedDateFormats =
        ["DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD"];

    private static readonly HashSet<string> AllowedTimeFormats = ["12h", "24h"];

    private static readonly HashSet<string> AllowedCurrencies = ["COP", "USD", "EUR"];

    private static readonly HashSet<string> AllowedLanguages = ["es", "en"];

    private static readonly HashSet<string> AllowedNumberFormats = ["1.000,00", "1,000.00"];

    public UpdateTenantLocalizationCommandValidator()
    {
        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .Must(tz => AllowedTimezones.Contains(tz))
            .WithMessage("Timezone is not in the list of supported IANA identifiers.");

        RuleFor(x => x.DateFormat)
            .NotEmpty().WithMessage("DateFormat is required.")
            .Must(f => AllowedDateFormats.Contains(f))
            .WithMessage($"DateFormat must be one of: {string.Join(", ", AllowedDateFormats)}.");

        RuleFor(x => x.TimeFormat)
            .NotEmpty().WithMessage("TimeFormat is required.")
            .Must(f => AllowedTimeFormats.Contains(f))
            .WithMessage($"TimeFormat must be one of: {string.Join(", ", AllowedTimeFormats)}.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required.")
            .Must(l => AllowedLanguages.Contains(l))
            .WithMessage($"Language must be one of: {string.Join(", ", AllowedLanguages)}.");

        RuleFor(x => x.NumberFormat)
            .NotEmpty().WithMessage("NumberFormat is required.")
            .Must(f => AllowedNumberFormats.Contains(f))
            .WithMessage($"NumberFormat must be one of: {string.Join(", ", AllowedNumberFormats)}.");
    }
}
