using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Appearance.UpdateTenantAppearance;

namespace FSH.Modules.Identity.Features.v1.Appearance.UpdateTenantAppearance;

public sealed class UpdateTenantAppearanceCommandValidator : AbstractValidator<UpdateTenantAppearanceCommand>
{
    private static readonly string[] ValidThemes = ["light", "dark", "system"];
    private static readonly string[] ValidDensities = ["compact", "default", "comfortable"];

    public UpdateTenantAppearanceCommandValidator()
    {
        RuleFor(x => x.Theme)
            .NotEmpty()
            .Must(t => Array.Exists(ValidThemes, v => v == t))
            .WithMessage("Theme must be 'light', 'dark', or 'system'.");

        RuleFor(x => x.Accent)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Font)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Density)
            .NotEmpty()
            .Must(d => Array.Exists(ValidDensities, v => v == d))
            .WithMessage("Density must be 'compact', 'default', or 'comfortable'.");

        RuleFor(x => x.CustomAccentJson)
            .MaximumLength(4000)
            .When(x => x.CustomAccentJson is not null);
    }
}
