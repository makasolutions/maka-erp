using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Appearance.UpdateTenantAppearance;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Appearance.UpdateTenantAppearance;

public sealed class UpdateTenantAppearanceCommandHandler
    : ICommandHandler<UpdateTenantAppearanceCommand, TenantAppearanceDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateTenantAppearanceCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<TenantAppearanceDto> Handle(
        UpdateTenantAppearanceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userName = _currentUser.GetUserEmail();

        // Finbuckle global query filter scopes to the current tenant.
        var appearance = await _dbContext.TenantAppearances
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (appearance is null)
        {
            // First time — create the row.
            appearance = TenantAppearance.CreateDefault(createdBy: userName);
            _dbContext.TenantAppearances.Add(appearance);
        }

        appearance.Update(
            command.Theme,
            command.Accent,
            command.Font,
            command.Density,
            command.CustomAccentJson,
            modifiedBy: userName);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TenantAppearanceDto(
            appearance.Theme,
            appearance.Accent,
            appearance.Font,
            appearance.Density,
            appearance.CustomAccentJson);
    }
}
