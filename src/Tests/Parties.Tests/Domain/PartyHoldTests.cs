using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.V2;
using FSH.Modules.Parties.Domain.V2.Exceptions;

namespace Parties.Tests.Domain;

public class PartyHoldTests
{
    private static readonly DateOnly Inicio = new(2026, 6, 1);

    [Fact]
    public void IsActiveOn_WithinOpenEndedPeriod_MatchingType_IsTrue()
    {
        var hold = PartyHold.Place(Guid.CreateVersion7(), HoldType.Ventas, "mora", Inicio, "user-1");

        hold.IsActiveOn(Inicio, HoldType.Ventas).ShouldBeTrue();
        hold.IsActiveOn(Inicio.AddYears(5), HoldType.Ventas).ShouldBeTrue(); // sin fecha liberación = indefinido
    }

    [Fact]
    public void IsActiveOn_BeforeStart_IsFalse()
    {
        var hold = PartyHold.Place(Guid.CreateVersion7(), HoldType.Ventas, "mora", Inicio, "user-1");
        hold.IsActiveOn(Inicio.AddDays(-1), HoldType.Ventas).ShouldBeFalse();
    }

    [Fact]
    public void IsActiveOn_AfterReleaseDate_IsFalse()
    {
        var hold = PartyHold.Place(Guid.CreateVersion7(), HoldType.Ventas, "mora", Inicio, "user-1",
            fechaLiberacion: Inicio.AddDays(30));

        hold.IsActiveOn(Inicio.AddDays(30), HoldType.Ventas).ShouldBeTrue();
        hold.IsActiveOn(Inicio.AddDays(31), HoldType.Ventas).ShouldBeFalse();
    }

    [Fact]
    public void IsActiveOn_DifferentType_IsFalse_UnlessTodo()
    {
        var ventas = PartyHold.Place(Guid.CreateVersion7(), HoldType.Ventas, "mora", Inicio, "user-1");
        ventas.IsActiveOn(Inicio, HoldType.Compras).ShouldBeFalse();

        var todo = PartyHold.Place(Guid.CreateVersion7(), HoldType.Todo, "fraude", Inicio, "user-1");
        todo.IsActiveOn(Inicio, HoldType.Ventas).ShouldBeTrue();
        todo.IsActiveOn(Inicio, HoldType.Pagos).ShouldBeTrue();
    }

    [Fact]
    public void IsActiveOn_AfterRelease_IsFalse()
    {
        var hold = PartyHold.Place(Guid.CreateVersion7(), HoldType.Ventas, "mora", Inicio, "user-1");
        hold.Release();
        hold.IsActiveOn(Inicio, HoldType.Ventas).ShouldBeFalse();
    }

    [Fact]
    public void Place_WithReleaseBeforeStart_Throws()
    {
        Should.Throw<InvalidHoldPeriodException>(() =>
            PartyHold.Place(Guid.CreateVersion7(), HoldType.Ventas, "x", Inicio, "user-1",
                fechaLiberacion: Inicio.AddDays(-1)));
    }
}
