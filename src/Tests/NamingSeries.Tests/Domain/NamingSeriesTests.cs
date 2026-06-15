using FSH.Framework.Core.Domain;
using FSH.Modules.NamingSeries.Domain;

namespace NamingSeries.Tests.Domain;

public class NamingSeriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static FSH.Modules.NamingSeries.Domain.NamingSeries Open(
        int from = 1, int to = 100,
        DateTimeOffset? validFrom = null, DateTimeOffset? validUntil = null) =>
        FSH.Modules.NamingSeries.Domain.NamingSeries.Create(
            tenantId: "acme",
            documentType: "SalesInvoice",
            pattern: NamingPattern.Parse("FV-{YYYY}-{####}"),
            from: from, to: to,
            validFrom: validFrom, validUntil: validUntil,
            resolutionNumber: "18760000001",
            resolutionDate: Now,
            resolutionTechnicalKey: "abc-def",
            createdBy: "user-1",
            now: Now);

    [Fact]
    public void Allocate_FromDraft_IncrementsCurrentValue_FormatsNumber()
    {
        var series = Open();
        var number = series.Allocate(Now);

        series.CurrentValue.ShouldBe(1);
        number.ShouldBe("FV-2026-0001");
        series.LastModifiedOnUtc.ShouldBe(Now);
    }

    [Fact]
    public void Allocate_AtUpperBound_Throws_NamingSeriesExhausted()
    {
        var series = Open(from: 1, to: 1);
        series.Allocate(Now);                                            // 1/1 OK
        Should.Throw<NamingSeriesExhaustedException>(() => series.Allocate(Now));
    }

    [Fact]
    public void Allocate_BeforeValidFrom_Throws()
    {
        var series = Open(validFrom: Now.AddDays(1));
        Should.Throw<NamingSeriesNotYetValidException>(() => series.Allocate(Now));
    }

    [Fact]
    public void Allocate_AfterValidUntil_Throws()
    {
        var series = Open(validUntil: Now.AddDays(-1));
        Should.Throw<NamingSeriesExpiredException>(() => series.Allocate(Now));
    }

    [Fact]
    public void Allocate_CrossingThreshold80Pct_RaisesEvent_SetsFlag()
    {
        var series = Open(from: 1, to: 10);
        for (int i = 0; i < 7; i++) series.Allocate(Now);                 // hasta 7/10 — sin evento (threshold ceil(0.8*10)=8)
        series.Notified80Pct.ShouldBeFalse();
        series.DomainEvents.OfType<NamingSeriesThresholdReachedDomainEvent>().ShouldBeEmpty();

        series.Allocate(Now);                                             // 8/10 — cruza umbral
        series.Notified80Pct.ShouldBeTrue();
        series.DomainEvents.OfType<NamingSeriesThresholdReachedDomainEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void Allocate_AlreadyNotified80Pct_DoesNotDuplicateEvent()
    {
        var series = Open(from: 1, to: 10);
        for (int i = 0; i < 8; i++) series.Allocate(Now);                 // alcanza 8/10 — emite una vez
        series.ClearDomainEvents();

        series.Allocate(Now);                                             // 9/10 — no debe emitir otra
        series.Allocate(Now);                                             // 10/10 — no debe emitir otra
        series.DomainEvents.OfType<NamingSeriesThresholdReachedDomainEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void Allocate_OnClosedSeries_Throws()
    {
        var series = Open();
        series.Close("admin", Now);
        Should.Throw<NamingSeriesClosedException>(() => series.Allocate(Now.AddMinutes(1)));
    }

    [Fact]
    public void Close_SetsTimestampAndUser()
    {
        var series = Open();
        series.Close("admin", Now);
        series.IsOpen.ShouldBeFalse();
        series.ClosedAtUtc.ShouldBe(Now);
        series.ClosedBy.ShouldBe("admin");
    }

    [Fact]
    public void Close_TwiceThrows()
    {
        var series = Open();
        series.Close("admin", Now);
        Should.Throw<NamingSeriesClosedException>(() => series.Close("admin", Now));
    }

    [Fact]
    public void Create_WithInvalidRange_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            FSH.Modules.NamingSeries.Domain.NamingSeries.Create(
                "acme", "SalesInvoice", NamingPattern.Parse("{####}"),
                from: 100, to: 50,
                validFrom: null, validUntil: null,
                resolutionNumber: null, resolutionDate: null, resolutionTechnicalKey: null,
                createdBy: "u", now: Now));
    }
}
