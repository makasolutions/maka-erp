using System.Reflection;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.Credit;
using FSH.Modules.Parties.Domain.Exceptions;

namespace Parties.Tests.Domain;

public class CreditAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static CreditAccount Open(decimal cupo = 1000m) =>
        CreditAccount.Open(Guid.CreateVersion7(), "acme", cupo, "user-1", now: Now);

    [Fact]
    public void Open_RecordsInitialAssignment_SetsBaseline()
    {
        var acc = Open(1000m);

        acc.CupoAsignado.ShouldBe(1000m);
        acc.SaldoDisponible.ShouldBe(1000m);
        acc.Movements.Count.ShouldBe(1);
        acc.Movements[0].Tipo.ShouldBe(CreditMovementType.AsignacionInicial);
        acc.Movements[0].SaldoResultante.ShouldBe(1000m);
    }

    [Fact]
    public void AddMovement_RecalculatesSaldoResultante_InChain()
    {
        var acc = Open(1000m);

        acc.AddMovement(CreditMovementType.Consumo, 300m, "user-1", now: Now);   // 1000 - 300 = 700
        acc.AddMovement(CreditMovementType.Liberacion, 100m, "user-1", now: Now); // 700 + 100 = 800

        acc.SaldoDisponible.ShouldBe(800m);
        acc.CupoAsignado.ShouldBe(1000m); // consumo/liberación no cambian el cupo asignado

        var saldos = acc.Movements.Select(m => m.SaldoResultante).ToArray();
        saldos.ShouldBe([1000m, 700m, 800m]);
    }

    [Fact]
    public void AddMovement_AumentoAndReduccion_AdjustCupoAsignado()
    {
        var acc = Open(1000m);

        acc.AddMovement(CreditMovementType.Aumento, 500m, "user-1", now: Now);   // cupo 1500, saldo 1500
        acc.AddMovement(CreditMovementType.Reduccion, 200m, "user-1", now: Now); // cupo 1300, saldo 1300

        acc.CupoAsignado.ShouldBe(1300m);
        acc.SaldoDisponible.ShouldBe(1300m);
    }

    [Fact]
    public void AddMovement_InitialAssignmentAgain_Throws()
    {
        var acc = Open();
        Should.Throw<InvalidCreditMovementException>(() =>
            acc.AddMovement(CreditMovementType.AsignacionInicial, 100m, "user-1"));
    }

    [Fact]
    public void AddMovement_NonPositiveAmount_Throws()
    {
        var acc = Open();
        Should.Throw<InvalidCreditMovementException>(() => acc.AddMovement(CreditMovementType.Consumo, 0m, "user-1"));
        Should.Throw<InvalidCreditMovementException>(() => acc.AddMovement(CreditMovementType.Consumo, -5m, "user-1"));
    }

    [Fact]
    public void CreditMovement_IsImmutable_NoPublicSetters_OnlyFactoryCreation()
    {
        var props = typeof(CreditMovement).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Inmutable: ninguna propiedad expone setter público.
        props.Where(p => p.SetMethod is { IsPublic: true }).ShouldBeEmpty();

        // Único punto de creación: el factory Record(...) interno. Sin constructores públicos.
        typeof(CreditMovement)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .ShouldBeEmpty();
        typeof(CreditMovement)
            .GetMethod("Record", BindingFlags.NonPublic | BindingFlags.Static)
            .ShouldNotBeNull();
    }
}
