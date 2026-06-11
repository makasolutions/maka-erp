---
paths:
  - "src/Tests/**/*"
---

# ⛔ Superseded — ver `testing.md`

Stack real (verificado en `src/Directory.Packages.props` y los `.csproj` de `src/Tests/`):
**xUnit + Shouldly (`x.ShouldBe(...)`) + NSubstitute (`Substitute.For<T>()`) + AutoFixture +
NetArchTest + Testcontainers.**

FluentAssertions (`.Should()`) y Moq (`Mock<T>`) **NO se usan** (0 referencias en `src/Tests`).
No introducirlos.

Convenciones, naming, proyectos y gotchas: `.agents/rules/testing.md` e `integration-testing.md`.
