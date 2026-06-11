using FSH.Framework.Eventing.Abstractions;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Blinda la doctrina Outbox-first (eventing.md + ADR-0001): la publicación de integration
/// events va SIEMPRE por <c>IOutboxStore</c>; el tipo <c>IEventBus</c> queda reservado a
/// BuildingBlocks (OutboxDispatcher + implementaciones InMemory/RabbitMq). Ningún tipo de un
/// assembly de módulo puede depender de él — ni handler, ni service, ni registro.
///
/// La regla prohíbe el TIPO exacto, no el assembly: los módulos dependen legítimamente de
/// <c>IIntegrationEvent</c>/<c>IIntegrationEventHandler&lt;T&gt;</c>, que viven en el mismo
/// <c>Eventing.Abstractions</c>. El test de control positivo de abajo garantiza que ese uso
/// legítimo no dispara falsos positivos (si alguien cambia el match a prefijo de namespace,
/// el control lo delata).
/// </summary>
public class EventingArchitectureTests
{
    private const string EventBusTypeFullName = "FSH.Framework.Eventing.Abstractions.IEventBus";

    [Fact]
    public void Modules_Should_Not_Depend_On_IEventBus_Publish_Via_Outbox_Instead()
    {
        var result = Types
            .InAssemblies(ModuleAssemblyDiscovery.GetModuleAssemblies())
            .ShouldNot()
            .HaveDependencyOn(EventBusTypeFullName)
            .GetResult();

        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);

        result.IsSuccessful.ShouldBeTrue(
            $"Estos tipos de módulo dependen de IEventBus (prohibido — publicar vía IOutboxStore, " +
            $"ver eventing.md y ADR-0001): {offenders}");
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

        // Y ninguno de esos consumidores legítimos depende del tipo prohibido.
        var consumerCheck = Types
            .InAssemblies(ModuleAssemblyDiscovery.GetModuleAssemblies())
            .That()
            .ImplementInterface(typeof(IIntegrationEventHandler<>))
            .ShouldNot()
            .HaveDependencyOn(EventBusTypeFullName)
            .GetResult();

        consumerCheck.IsSuccessful.ShouldBeTrue();
    }
}
