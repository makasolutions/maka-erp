using FSH.Modules.SharedRecords.Contracts.v1.Phones;
using FSH.Modules.SharedRecords.Domain;

namespace FSH.Modules.SharedRecords.Features;

internal static class PhoneMapping
{
    public static PhoneDto ToDto(this Phone p) => new(
        p.Id, p.OwnerType, p.OwnerId, p.TypeCode, p.IsActive, p.IsPrimary,
        p.Number, p.Extension, p.CountryCode);
}
