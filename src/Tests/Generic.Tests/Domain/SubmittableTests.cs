using FSH.Framework.Core.Domain;

namespace Generic.Tests.Domain;

/// <summary>
/// Tests del building block <see cref="ISubmittable"/> (ADR-0006): transiciones de estado y
/// helpers de guardia. Valida el patrón sin tener una entidad de dominio que lo adopte aún
/// — la fixture <see cref="TestDoc"/> implementa el interfaz con un Submit/Cancel mínimo
/// que cualquier agregado real seguirá.
/// </summary>
public class SubmittableTests
{
    private static TestDoc Draft() => new();

    private static TestDoc Submitted()
    {
        var d = new TestDoc();
        d.Submit("user-1");
        return d;
    }

    private static TestDoc Cancelled()
    {
        var d = Submitted();
        d.Cancel("equivocado", "user-2");
        return d;
    }

    [Fact]
    public void Submit_FromDraft_ChangesToSubmitted_StampsTimestampAndUser()
    {
        var doc = Draft();

        doc.Submit("user-1");

        doc.DocStatus.ShouldBe(DocStatus.Submitted);
        doc.SubmittedAt.ShouldNotBeNull();
        doc.SubmittedBy.ShouldBe("user-1");
        doc.CancelledAt.ShouldBeNull();
        doc.CancelledBy.ShouldBeNull();
        doc.CancellationReason.ShouldBeNull();
    }

    [Fact]
    public void Submit_FromSubmitted_Throws()
    {
        var doc = Submitted();

        Should.Throw<InvalidOperationException>(() => doc.Submit("user-x"));
    }

    [Fact]
    public void Submit_FromCancelled_Throws()
    {
        var doc = Cancelled();

        Should.Throw<InvalidOperationException>(() => doc.Submit("user-x"));
    }

    [Fact]
    public void Cancel_FromSubmitted_WithReason_ChangesToCancelled()
    {
        var doc = Submitted();

        doc.Cancel("error de digitación", "user-2");

        doc.DocStatus.ShouldBe(DocStatus.Cancelled);
        doc.CancelledAt.ShouldNotBeNull();
        doc.CancelledBy.ShouldBe("user-2");
        doc.CancellationReason.ShouldBe("error de digitación");
    }

    [Fact]
    public void Cancel_FromSubmitted_WithoutReason_Throws()
    {
        var doc = Submitted();

        Should.Throw<ArgumentException>(() => doc.Cancel("   ", "user-2"));
        Should.Throw<ArgumentException>(() => doc.Cancel(string.Empty, "user-2"));
    }

    [Fact]
    public void Cancel_FromDraft_Throws()
    {
        var doc = Draft();

        var ex = Should.Throw<InvalidOperationException>(() => doc.Cancel("razón", "user-x"));
        ex.Message.ShouldContain("Draft");
    }

    [Fact]
    public void Cancel_FromCancelled_Throws()
    {
        var doc = Cancelled();

        var ex = Should.Throw<InvalidOperationException>(() => doc.Cancel("otra", "user-x"));
        ex.Message.ShouldContain("terminal");
    }

    [Fact]
    public void EnsureMutable_FromSubmitted_Throws()
    {
        ISubmittable doc = Submitted();

        Should.Throw<InvalidOperationException>(() => doc.EnsureMutable());
    }

    [Fact]
    public void EnsureMutable_FromDraft_DoesNotThrow()
    {
        ISubmittable doc = Draft();

        Should.NotThrow(() => doc.EnsureMutable());
    }

    [Fact]
    public void Amend_AmendedFrom_PointsToCancelledOriginal()
    {
        var original = Cancelled();
        original.Id.ShouldNotBe(Guid.Empty);

        var amendment = TestDoc.CreateAmendment(amendedFrom: original.Id);

        amendment.AmendedFrom.ShouldBe(original.Id);
        amendment.DocStatus.ShouldBe(DocStatus.Draft);
    }

    /// <summary>
    /// Fixture mínima — implementa <see cref="ISubmittable"/> con métodos <c>Submit</c>/<c>Cancel</c>
    /// que reflejan el patrón canónico que cada agregado real debe seguir.
    /// </summary>
    private sealed class TestDoc : ISubmittable
    {
        public Guid Id { get; } = Guid.CreateVersion7();
        public DocStatus DocStatus { get; private set; } = DocStatus.Draft;
        public DateTimeOffset? SubmittedAt { get; private set; }
        public string? SubmittedBy { get; private set; }
        public DateTimeOffset? CancelledAt { get; private set; }
        public string? CancelledBy { get; private set; }
        public string? CancellationReason { get; private set; }
        public Guid? AmendedFrom { get; private set; }

        public void Submit(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            this.EnsureCanSubmit();
            DocStatus = DocStatus.Submitted;
            SubmittedAt = DateTimeOffset.UtcNow;
            SubmittedBy = userId;
        }

        public void Cancel(string reason, string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            this.EnsureCanCancel();
            DocStatus = DocStatus.Cancelled;
            CancelledAt = DateTimeOffset.UtcNow;
            CancelledBy = userId;
            CancellationReason = reason;
        }

        public static TestDoc CreateAmendment(Guid amendedFrom) =>
            new() { AmendedFrom = amendedFrom };
    }
}
