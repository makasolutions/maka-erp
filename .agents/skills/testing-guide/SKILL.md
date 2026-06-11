---
name: testing-guide
description: Write unit tests, integration tests, and architecture tests for FSH/Maka features using xUnit + Shouldly + NSubstitute. Use when adding tests or understanding the testing strategy.
---

# Testing Guide

Estrategia por capas con los architecture tests como guardarraíl. Stack **real** (verificado en
`src/Directory.Packages.props`): **xUnit + Shouldly + NSubstitute + AutoFixture + NetArchTest +
Testcontainers**. FluentAssertions y Moq NO se usan. Convenciones completas: `.agents/rules/testing.md`.

## Test Project Structure

```
src/Tests/
├── Architecture.Tests/     # Boundaries + handler↔validator pairing (obligatorios)
├── Integration.Tests/      # WebApplicationFactory + Testcontainers (requiere Docker)
├── Integration.Middleware.Tests/
├── {Module}.Tests/         # Unit: Catalog, Parties, Identity, Chat, Files, Auditing, …
├── Framework.Tests/ Caching.Tests/ Multitenancy.Tests/
└── Generic.Tests/          # utilidades compartidas
```

## Handler test (NSubstitute + Shouldly)

Los handlers inyectan el DbContext (no hay repositorio); para unit tests se usan dobles de los
servicios colaboradores y/o un DbContext sobre SQLite/InMemory según el patrón del proyecto del
módulo — copiar el arreglo de un test existente del mismo módulo (ej. `Catalog.Tests`).

```csharp
public class Create{Entity}CommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    [Fact]
    public async Task Handle_Should_ReturnId_When_CommandIsValid()
    {
        // Arrange
        _currentUser.GetTenant().Returns("test-tenant");
        var handler = /* construir con DbContext de test + _currentUser */;
        var command = new Create{Entity}Command("Test", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBe(Guid.Empty);
    }
}
```

⚠️ Al verificar un `CancellationToken` reenviado, asertar el token **específico**
(`service.Received(1).DoAsync(arg, ct)`): NSubstitute rellena parámetros opcionales con `default`,
así que `Received(1).DoAsync(arg)` asertaría `CancellationToken.None` silenciosamente.

## Validator test

```csharp
public class Create{Entity}CommandValidatorTests
{
    private readonly Create{Entity}CommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_When_NameIsEmpty()
    {
        var result = _validator.Validate(new Create{Entity}Command("", 99.99m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("Valid Name", 10)]
    public void Validate_Should_Pass_When_CommandIsValid(string name, decimal price)
    {
        _validator.Validate(new Create{Entity}Command(name, price)).IsValid.ShouldBeTrue();
    }
}
```

## Entity test

```csharp
public class {Entity}Tests
{
    [Fact]
    public void Create_Should_SetProperties_And_RaiseEvent()
    {
        var entity = {Entity}.Create("Test", 99.99m);

        entity.Id.ShouldNotBe(Guid.Empty);
        entity.Name.ShouldBe("Test");
        entity.DomainEvents.ShouldContain(e => e is {Entity}CreatedEvent);
    }

    [Fact]
    public void Create_Should_Throw_When_NameIsEmpty()
    {
        Should.Throw<ArgumentException>(() => {Entity}.Create("", 99.99m));
    }
}
```

## Architecture tests

Viven en `Architecture.Tests` y deben quedar verdes siempre: módulos solo via `.Contracts`,
handlers `sealed`, tenant-isolation en entidades, y **todo command/paginated-query handler con su
`{Name}Validator`** (`HandlerValidatorPairingTests`). No escribir nuevos a mano sin mirar los existentes.

## Running

```bash
dotnet test src/FSH.Starter.slnx                      # todo (Integration requiere Docker)
dotnet test src/Tests/{Module}.Tests                  # un proyecto
dotnet test --filter "FullyQualifiedName~{Clase}"     # un test
dotnet test --collect "XPlat Code Coverage" --settings coverage.runsettings
```

Si Docker está caído, Integration.Tests fallan con `DockerUnavailableException` — ambiental, no regresión.

## Key Rules

1. **Architecture tests obligatorios** — protegen los límites de módulo.
2. **Naming**: `MethodName_Should_ExpectedBehavior_When_Condition`; Arrange-Act-Assert.
3. **Shouldly** (`x.ShouldBe(...)`) — nunca FluentAssertions.
4. **NSubstitute** (`Substitute.For<T>()`) — nunca Moq.
5. Gotchas de integración (tenant AsyncLocal inline, storage eager, SignalR long-polling):
   `.agents/rules/integration-testing.md`.
