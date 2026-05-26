using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Appearance.GetTenantAppearance;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Appearance.GetTenantAppearance;

public sealed class GetTenantAppearanceQueryHandler : IQueryHandler<GetTenantAppearanceQuery, TenantAppearanceDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetTenantAppearanceQueryHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<TenantAppearanceDto> Handle(
        GetTenantAppearanceQuery query,
        CancellationToken cancellationToken)
    {
        // Finbuckle global query filter scopes this to the current tenant automatically.
        var appearance = await _dbContext.TenantAppearances
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (appearance is null)
        {
            // Seed default row if none exists yet.
            var userEmail = _currentUser.GetUserEmail();
            appearance = TenantAppearance.CreateDefault(createdBy: userEmail);
            _dbContext.TenantAppearances.Add(appearance);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new TenantAppearanceDto(
            appearance.Theme,
            appearance.Accent,
            appearance.Font,
            appearance.Density,
            appearance.CustomAccentJson);
    }
}
