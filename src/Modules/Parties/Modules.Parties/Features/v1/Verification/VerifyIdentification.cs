using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Verification;
using FSH.Modules.Parties.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Verification;

public sealed class VerifyIdentificationQueryHandler(IIdentityVerificationProvider provider)
    : IQueryHandler<VerifyIdentificationQuery, VerifyIdentificationResult>
{
    public async ValueTask<VerifyIdentificationResult> Handle(VerifyIdentificationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string? error = IdentificationValidator.Validate(query.IdentificationTypeCode, query.Number, query.VerificationDigit);
        int? dv = IdentificationValidator.NitVerificationDigit(query.Number);

        string? legalName = null, status = null, source = "local";
        if (error is null)
        {
            var lookup = await provider.VerifyAsync(query.IdentificationTypeCode, query.Number, cancellationToken).ConfigureAwait(false);
            if (lookup is { Found: true })
            {
                legalName = lookup.LegalName;
                status = lookup.RegistryStatus;
                source = lookup.Source;
            }
        }

        return new VerifyIdentificationResult(error is null, error, dv, legalName, status, source);
    }
}

public static class VerifyIdentificationEndpoint
{
    public static RouteHandlerBuilder MapVerifyIdentificationEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/verify-identification",
                async (VerifyIdentificationQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    VerifyIdentificationResult result = await mediator.Send(query, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("VerifyIdentification")
            .WithSummary("Validate an identification (DV/format) and optionally fetch legal name")
            .RequirePermission(PartiesPermissions.Parties.Create)
            .Produces<VerifyIdentificationResult>(StatusCodes.Status200OK);
}
