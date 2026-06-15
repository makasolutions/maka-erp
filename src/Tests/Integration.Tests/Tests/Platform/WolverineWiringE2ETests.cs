using FSH.Modules.Identity.Data;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Integration.Tests.Tests.Platform;

/// <summary>
/// Fase 1 — Wolverine wired en paralelo en Identity (ADR-0001/0004).
///
/// Tests E2E del WIRING (NO de la atomicidad estructural). Verifican que las cuatro
/// piezas que Fase 1 cableó están vivas en el host del API:
///
///   1. El schema Wolverine se creó: las 4 tablas existen en 'identity'.
///   2. El handler test-only Phase1SmokeMessageHandler fue descubierto vía el override
///      del factory (ConfigureWolverine additive + IncludeType).
///   3. El codegen de Wolverine envuelve el handler — al invocar el mensaje, la stack
///      trace contiene el tipo generado Internal.Generated.WolverineHandlers.*.
///
/// La demostración positiva de ATOMICIDAD del outbox (envelope persistido en
/// wolverine_outgoing_envelopes en la misma tx del SaveChanges) queda diferida a Fase 2,
/// cuando un publicador real de Identity (handler de módulo, mensaje real, transporte
/// real activo) migre. Ver docs/specs/platform/wolverine-phase1-followups.md para la
/// 5ª capa no resuelta y las hipótesis a validar en Fase 2.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WolverineWiringE2ETests
{
    private const string SchemaName = "identity";

    private static readonly string[] RequiredTables =
    [
        "wolverine_outgoing_envelopes",
        "wolverine_incoming_envelopes",
        "wolverine_dead_letters",
        "wolverine_nodes",
    ];

    private readonly FshWebApplicationFactory _factory;

    public WolverineWiringE2ETests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ─── #1 — Las 4 tablas Wolverine existen en el schema 'identity' ────────
    [Fact]
    public async Task Wolverine_Schema_Tables_Should_Exist_In_Identity_Schema()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"SELECT tablename FROM pg_tables WHERE schemaname = '{SchemaName}' " +
                $"  AND tablename LIKE 'wolverine_%' ORDER BY tablename;";
            using var reader = await cmd.ExecuteReaderAsync();
            var found = new List<string>();
            while (await reader.ReadAsync())
            {
                found.Add(reader.GetString(0));
            }

            foreach (var required in RequiredTables)
            {
                found.ShouldContain(required, $"tabla Wolverine '{required}' debe existir en schema '{SchemaName}'");
            }
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    // ─── #2 — Discovery + codegen del handler test-only (combinados) ────────
    // El propio invoke ejerce ambas cosas: si el handler no fue descubierto por el
    // override additive del factory (ConfigureWolverine + IncludeType), Wolverine
    // fallaría por ausencia de ruta antes de llegar a Phase1SmokeException; y si el
    // codegen no envolviera la ejecución, el stack trace no contendría el tipo
    // generado. Un solo test cubre los dos puntos de wiring.
    [Fact]
    public async Task Invoking_Phase1SmokeMessage_Should_Execute_Through_Wolverine_Codegen()
    {
        var bus = _factory.Services.GetRequiredService<IMessageBus>();

        // El handler con ShouldFail=true lanza Phase1SmokeException. La stack trace de la
        // excepción debe contener el tipo generado Internal.Generated.WolverineHandlers.* —
        // ésa es la firma de que Wolverine codegen está envolviendo la ejecución, no nuestro
        // bus propio ni una invocación directa al método estático.
        var marker = $"wiring-{Guid.NewGuid():N}";
        var ex = await Should.ThrowAsync<Phase1SmokeException>(async () =>
            await bus.InvokeAsync(new Phase1SmokeMessage(marker, ShouldFail: true)));

        ex.Message.ShouldContain(marker);
        (ex.StackTrace ?? string.Empty).ShouldContain("Internal.Generated.WolverineHandlers");
    }
}

/// <summary>
/// Mensaje raíz del wiring test. Se invoca via <c>IMessageBus.InvokeAsync</c> desde el
/// test (patrón canónico Wolverine para tests E2E). <see cref="Phase1SmokeMessageHandler"/>
/// lo procesa inline. NO es un integration event del sistema — vive en el assembly de
/// tests y se descubre vía <c>opts.Discovery.IncludeType(typeof(Phase1SmokeMessageHandler))</c>
/// agregado por el factory.
/// </summary>
public sealed record Phase1SmokeMessage(string Marker, bool ShouldFail);

/// <summary>
/// Mensaje child — vestigial. Quedó del intento de demostrar atomicidad estructural
/// del outbox; ningún patrón out-of-tx-real probado (PublishAsync, cascading return,
/// scheduled) produjo envelope persistido visible al test. Mantenido como anclaje del
/// 5º capa documentada en wolverine-phase1-followups.md para que Fase 2 retome desde
/// aquí con un publicador real.
/// </summary>
public sealed record Phase1SmokeChildMessage(string Marker);

/// <summary>
/// Excepción propia del handler — permite al test asertar con tipo específico.
/// </summary>
public sealed class Phase1SmokeException : System.Exception
{
    public Phase1SmokeException(string message) : base(message) { }
    public Phase1SmokeException() { }
    public Phase1SmokeException(string message, System.Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Handler test-only. Discoverable solo cuando el factory llama
/// <c>opts.Discovery.IncludeType(typeof(Phase1SmokeMessageHandler))</c> vía
/// <c>ConfigureWolverine</c> (additive sobre el discovery selectivo de Identity en Program.cs).
/// Convención Wolverine: nombre &lt;Message&gt;Handler + método <c>Handle</c>.
/// </summary>
public static class Phase1SmokeMessageHandler
{
    public static async Task Handle(
        Phase1SmokeMessage message,
        FSH.Modules.Identity.Data.IdentityDbContext db)
    {
        _ = await db.Database.CanConnectAsync();

        if (message.ShouldFail)
        {
            throw new Phase1SmokeException($"smoke handler intentional throw for marker '{message.Marker}'");
        }
    }
}

/// <summary>
/// Handler stub del child. Vestigial: existe solo para que <see cref="Phase1SmokeChildMessage"/>
/// tenga ruta cuando Fase 2 retome los intentos out-of-tx documentados en followups.
/// </summary>
public static class Phase1SmokeChildMessageHandler
{
    public static void Handle(Phase1SmokeChildMessage _)
    {
        // Stub deliberadamente vacío — solo existe para darle ruta al mensaje.
    }
}
