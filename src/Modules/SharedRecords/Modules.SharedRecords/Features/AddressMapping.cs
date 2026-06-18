using FSH.Modules.SharedRecords.Contracts.v1.Addresses;
using FSH.Modules.SharedRecords.Domain;

namespace FSH.Modules.SharedRecords.Features;

internal static class AddressMapping
{
    public static AddressDto ToDto(this Address a) => new(
        a.Id, a.OwnerType, a.OwnerId, a.LabelCode, a.IsActive, a.IsPrimary,
        a.Country, a.Department, a.City, a.DepartmentCode, a.MunicipalityCode,
        a.Line, a.Barrio, a.Reference, a.Latitude, a.Longitude);
}
