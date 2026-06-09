using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.Geography;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.Geography;

public static class GeographyEndpoints
{
    public static void MapGeographyEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/departments",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetDepartmentsQuery(), ct)))
            .WithName("GetDepartments")
            .WithSummary("DIVIPOLA departments with their municipalities (for city pickers)")
            .RequirePermission(LookupsPermissions.Tables.View)
            .Produces<IReadOnlyList<DepartmentDto>>(StatusCodes.Status200OK);

        group.MapGet("/municipalities",
                async (string dept, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetMunicipalitiesQuery(dept), ct)))
            .WithName("GetMunicipalities")
            .WithSummary("DIVIPOLA municipalities of a department by 2-digit code")
            .RequirePermission(LookupsPermissions.Tables.View)
            .Produces<IReadOnlyList<MunicipalityDto>>(StatusCodes.Status200OK);
    }
}
