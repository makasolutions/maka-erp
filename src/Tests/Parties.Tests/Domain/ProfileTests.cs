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
    public void SupplierProfile_AllowsNullCurrency()
    {
        // v1 no tiene catálogo de monedas con Guid; el backfill (PR-C) crea el perfil sin moneda.
        // Guid.Empty se normaliza a null. Ver SPEC §6.2 (DefaultCurrencyId nullable).
        var withoutCurrency = SupplierProfile.Create(Guid.CreateVersion7());
        withoutCurrency.DefaultCurrencyId.ShouldBeNull();

        var emptyNormalized = SupplierProfile.Create(Guid.CreateVersion7(), Guid.Empty);
        emptyNormalized.DefaultCurrencyId.ShouldBeNull();

        var currency = Guid.CreateVersion7();
        var withCurrency = SupplierProfile.Create(Guid.CreateVersion7(), currency);
        withCurrency.DefaultCurrencyId.ShouldBe(currency);
    }
}
