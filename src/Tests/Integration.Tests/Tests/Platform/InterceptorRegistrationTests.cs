using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Platform;

/// <summary>
/// Guard de regresión para la clase de bug "Cannot resolve scoped service ISaveChangesInterceptor
/// from root provider": los DbContext con integración Wolverine registran sus DbContextOptions como
/// SINGLETON, así que EF resuelve los <see cref="ISaveChangesInterceptor"/> desde el ROOT provider al
/// construir las options. Un interceptor registrado SCOPED rompe esa resolución → 500 en el login.
///
/// Inspecciona SOLO los descriptores del host compartido (<see cref="FshWebApplicationFactory.CapturedServices"/>),
/// que es el punto ciego que dejó pasar el bug (el test host usa InMemory y no monta las options
/// singleton de Wolverine). NO construye un 2.º host: hacerlo dispondría el estático global
/// JobStorage.Current de Hangfire y rompería los tests posteriores que crean tenants.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class InterceptorRegistrationTests
{
    private readonly FshWebApplicationFactory _factory;

    public InterceptorRegistrationTests(FshWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void No_ISaveChangesInterceptor_Is_Registered_Scoped()
    {
        var captured = _factory.CapturedServices;
        captured.ShouldNotBeEmpty(
            "El host compartido debe haberse construido y capturado sus descriptores.");

        var scoped = captured
            .Where(d => d.ServiceType == typeof(ISaveChangesInterceptor)
                && d.Lifetime == ServiceLifetime.Scoped)
            .Select(d => d.ImplementationType?.Name ?? d.ImplementationInstance?.GetType().Name ?? "?")
            .ToList();

        scoped.ShouldBeEmpty(
            "Todo ISaveChangesInterceptor DEBE registrarse Singleton (las options singleton de los "
            + "DbContext con Wolverine resuelven los interceptores desde el root provider; uno scoped "
            + $"rompe el login). Scoped encontrados: {string.Join(", ", scoped)}");

        // sanity: deben existir los interceptores (Auditable + DomainEvents + Auditing). Si esto
        // bajara de 3, el guard dejó de verificar lo que debe — falla.
        captured.Count(d => d.ServiceType == typeof(ISaveChangesInterceptor))
            .ShouldBeGreaterThanOrEqualTo(3);
    }

    /// <summary>
    /// Meta-guard: prueba que el predicado SÍ detecta un ISaveChangesInterceptor scoped. Sin esto, un
    /// refactor del filtro podría dejar pasar un scoped y el guard pasaría "porque dejó de verificar".
    /// Usa un ServiceCollection aislado (sin host) — seguro y rápido.
    /// </summary>
    [Fact]
    public void Detection_Predicate_Flags_A_Scoped_Interceptor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISaveChangesInterceptor, FakeSingletonInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, FakeScopedInterceptor>();

        var scoped = services
            .Where(d => d.ServiceType == typeof(ISaveChangesInterceptor)
                && d.Lifetime == ServiceLifetime.Scoped)
            .Select(d => d.ImplementationType?.Name ?? "?")
            .ToList();

        scoped.ShouldContain(nameof(FakeScopedInterceptor));
        scoped.ShouldNotContain(nameof(FakeSingletonInterceptor));
    }

    private sealed class FakeSingletonInterceptor : SaveChangesInterceptor;
    private sealed class FakeScopedInterceptor : SaveChangesInterceptor;
}
