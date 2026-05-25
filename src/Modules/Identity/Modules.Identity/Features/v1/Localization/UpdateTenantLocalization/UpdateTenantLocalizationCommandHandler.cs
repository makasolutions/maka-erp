using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Localization.UpdateTenantLocalization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Localization.UpdateTenantLocalization;

public sealed class UpdateTenantLocalizationCommandHandler
    : ICommandHandler<UpdateTenantLocalizationCommand, TenantLocalizationDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateTenantLocalizationCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<TenantLocalizationDto> Handle(
        UpdateTenantLocalizationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userName = _currentUser.GetUserEmail();

        // Finbuckle global query filter scopes to the current tenant.
        var localization = await _dbContext.TenantLocalizations
            .Where(l => l.UserId == null)   // tenant-wide row only
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (localization is null)
        {
            // First time — create the row.
            localization = TenantLocalization.CreateDefault(createdBy: userName);
            _dbContext.TenantLocalizations.Add(localization);
        }

        localization.Update(
            command.Timezone,
            command.DateFormat,
            command.TimeFormat,
            command.Currency,
            command.Language,
            command.NumberFormat,
            modifiedBy: userName);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TenantLocalizationDto(
            localization.Timezone,
            localization.DateFormat,
            localization.TimeFormat,
            localization.Currency,
            localization.Language,
            localization.NumberFormat);
    }
}
