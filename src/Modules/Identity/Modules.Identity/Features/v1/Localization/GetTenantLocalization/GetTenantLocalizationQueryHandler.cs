using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Localization.GetTenantLocalization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Localization.GetTenantLocalization;

public sealed class GetTenantLocalizationQueryHandler : IQueryHandler<GetTenantLocalizationQuery, TenantLocalizationDto>
{
    private readonly IdentityDbContext _dbContext;

    public GetTenantLocalizationQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<TenantLocalizationDto> Handle(
        GetTenantLocalizationQuery query,
        CancellationToken cancellationToken)
    {
        // Finbuckle global query filter scopes this to the current tenant automatically.
        // GET is read-only: if no row exists yet, return defaults. The first PUT will
        // create the row via UpdateTenantLocalizationCommandHandler's upsert logic.
        var localization = await _dbContext.TenantLocalizations
            .AsNoTracking()
            .Where(l => l.UserId == null)   // tenant-wide row only (future: per-user override)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (localization is null)
        {
            var defaults = TenantLocalization.CreateDefault();
            return new TenantLocalizationDto(
                defaults.Timezone,
                defaults.DateFormat,
                defaults.TimeFormat,
                defaults.Currency,
                defaults.Language,
                defaults.NumberFormat);
        }

        return new TenantLocalizationDto(
            localization.Timezone,
            localization.DateFormat,
            localization.TimeFormat,
            localization.Currency,
            localization.Language,
            localization.NumberFormat);
    }
}
