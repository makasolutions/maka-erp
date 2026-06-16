using FSH.Modules.Parties.Domain.V2.Profiles;

namespace Parties.Tests.Domain;

public class ProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CustomerProfile_Activation_IsIdempotent()
    {
        var p = CustomerProfile.Create(Guid.CreateVersion7(), now: Now);
        p.IsActive.ShouldBeTrue();
        p.ActivatedOn.ShouldBe(Now);

        // Activar de nuevo no cambia estado ni fecha.
        p.Activate(Now.AddDays(10));
        p.IsActive.ShouldBeTrue();
        p.ActivatedOn.ShouldBe(Now);

        // Desactivar es idempotente.
        p.Deactivate();
        p.Deactivate();
        p.IsActive.ShouldBeFalse();

        // Reactivar sella nueva fecha de activación.
        p.Activate(Now.AddDays(20));
        p.IsActive.ShouldBeTrue();
        p.ActivatedOn.ShouldBe(Now.AddDays(20));
    }

    [Fact]
    public void EmployeeProfile_Activation_IsIdempotent()
    {
        var e = EmployeeProfile.Create(Guid.CreateVersion7(), isSalesperson: true);
        e.IsActive.ShouldBeTrue();
        e.Activate();
        e.IsActive.ShouldBeTrue();
        e.Deactivate();
        e.Deactivate();
        e.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void SupplierProfile_RequiresCurrency()
    {
        Should.Throw<ArgumentException>(() => SupplierProfile.Create(Guid.CreateVersion7(), Guid.Empty));
    }
}
