namespace FSH.Modules.NamingSeries.Contracts.Dtos;

public sealed record NamingSeriesDto(
    Guid Id,
    string TenantId,
    string DocumentType,
    string Pattern,
    int From,
    int To,
    int CurrentValue,
    int Capacity,
    int PercentUsed,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    DateTimeOffset? ClosedAtUtc,
    string? ClosedBy,
    bool Notified80Pct,
    string? ResolutionNumber,
    DateTimeOffset? ResolutionDate,
    string? ResolutionTechnicalKey,
    DateTimeOffset CreatedOnUtc);

public sealed record NamingSeriesUsageReportRow(
    Guid Id,
    string DocumentType,
    int CurrentValue,
    int Capacity,
    int PercentUsed,
    DateTimeOffset? ValidUntil,
    bool Notified80Pct);
