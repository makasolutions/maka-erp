using FSH.Modules.Lookups.Contracts.v1.Geography;
using FSH.Modules.Lookups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Lookups.Features.v1.Geography;

public sealed class GetDepartmentsQueryHandler(LookupsDbContext db)
    : IQueryHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async ValueTask<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery query, CancellationToken cancellationToken)
    {
        var depts = await db.Departments.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new { d.Code, d.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var munis = await db.Municipalities.AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new { m.Code, m.Name, m.DepartmentCode })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var byDept = munis.GroupBy(m => m.DepartmentCode)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MunicipalityDto>)g
                .Select(m => new MunicipalityDto(m.Code, m.Name)).ToList());

        return depts.Select(d => new DepartmentDto(
            d.Code, d.Name,
            byDept.TryGetValue(d.Code, out var list) ? list : [])).ToList();
    }
}

public sealed class GetMunicipalitiesQueryHandler(LookupsDbContext db)
    : IQueryHandler<GetMunicipalitiesQuery, IReadOnlyList<MunicipalityDto>>
{
    public async ValueTask<IReadOnlyList<MunicipalityDto>> Handle(GetMunicipalitiesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        string dept = (query.DepartmentCode ?? string.Empty).Trim();
        if (dept.Length == 0) return [];

        return await db.Municipalities.AsNoTracking()
            .Where(m => m.DepartmentCode == dept)
            .OrderBy(m => m.Name)
            .Select(m => new MunicipalityDto(m.Code, m.Name))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
