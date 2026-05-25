using FSH.Framework.Core.Context;
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
    private readonly ICurrentUser _currentUser;

    public GetTenantLocalizationQueryHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<TenantLocalizationDto> Handle(
        GetTenantLocalizationQuery query,
        CancellationToken cancellationToken)
    {
        // Finbuckle global query filter scopes this to the current tenant automatically.
        var localization = await _dbContext.TenantLocalizations
            .AsNoTracking()
            .Where(l => l.UserId == null)   // tenant-wide row only (future: per-user override)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (localization is null)
        {
            // Seed default row if none exists yet.
            var userEmail = _currentUser.GetUserEmail();
            localization = TenantLocalization.CreateDefault(createdBy: userEmail);
            _dbContext.TenantLocalizations.Add(localization);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
