using FSH.Framework.Eventing.Abstractions;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Doctrina de eventing tras la Fase 5 (eliminación del bus de eventos propio). La publicación
/// de integration events va SIEMPRE por la fachada <c>IIntegrationEventPublisher&lt;TDbContext&gt;</c>,
/// que rutea por el outbox transaccional y tenant-aware de Wolverine. Ningún módulo debe inyectar
/// el bus global de Wolverine (<c>Wolverine.IMessageBus</c>) directamente: eso saltaría la fachada,
/// el outbox transaccional y la propagación de TenantId (INV-9).
///
/// La regla prohíbe el TIPO exacto del bus global, no el assembly de Wolverine: los módulos
/// dependen legítimamente de <c>IIntegrationEvent</c>/<c>IIntegrationEventHandler&lt;T&gt;</c>
/// (Eventing.Abstractions). El control positivo de abajo garantiza que ese uso legítimo no
/// dispara falsos positivos.
/// </summary>
public class EventingArchitectureTests
{
    // El bus global de Wolverine — publicar por aquí evita la fachada + outbox transaccional.
    private const string WolverineMessageBusTypeFullName = "Wolverine.IMessageBus";

    [Fact]
    public void Modules_Should_Not_Depend_On_Wolverine_MessageBus_Publish_Via_Facade_Instead()
    {
        var result = Types
            .InAssemblies(ModuleAssemblyDiscovery.GetModuleAssemblies())
            .ShouldNot()
            .HaveDependencyOn(WolverineMessageBusTypeFullName)
            .GetResult();

        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);

        result.IsSuccessful.ShouldBeTrue(
            $"Estos tipos de módulo dependen de Wolverine.IMessageBus (prohibido — publicar vía " +
            $"IIntegrationEventPublisher<TDbContext>, que usa el outbox transaccional tenant-aware): {offenders}");
    }

    [Fact]
    public void Control_Modules_Using_IIntegrationEvent_Are_Allowed()
    {
        // Control positivo: los módulos SÍ dependen de Eventing.Abstractions a través de
        // IIntegrationEventHandler<T> (p. ej. Notifications, Webhooks). Si este control
        // encuentra >0 consumidores y la regla principal sigue verde, queda probado que el
        // match es por tipo exacto y no arrastra al resto del assembly.
        var consumers = Types
            .InAssemblies(ModuleAssemblyDiscovery.GetModuleAssemblies())
            .That()
            .ImplementInterface(typeof(IIntegrationEventHandler<>))
            .GetTypes()
            .ToList();

        consumers.ShouldNotBeEmpty(
            "Se esperaba al menos un IIntegrationEventHandler<T> en los módulos (uso legítimo de " +
            "Eventing.Abstractions que la regla principal NO debe marcar)");

        // Y ninguno de esos consumidores legítimos depende del bus global prohibido.
        var consumerCheck = Types
            .InAssemblies(ModuleAssemblyDiscovery.GetModuleAssemblies())
            .That()
            .ImplementInterface(typeof(IIntegrationEventHandler<>))
            .ShouldNot()
            .HaveDependencyOn(WolverineMessageBusTypeFullName)
            .GetResult();

        consumerCheck.IsSuccessful.ShouldBeTrue();
    }
}
