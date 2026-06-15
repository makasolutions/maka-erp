using System.Globalization;
using JasperFx;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Postgresql;

namespace FSH.Starter.DbMigrator;

/// <summary>
/// Fase 1 (ADR-0001/0004) — orquesta el setup de las 4 tablas Wolverine
/// (<c>wolverine_outgoing_envelopes</c>, <c>wolverine_incoming_envelopes</c>,
/// <c>wolverine_dead_letters</c>, <c>wolverine_nodes</c>) en un schema dado,
/// post-EF-migrations. Originalmente solo soportaba <c>identity</c>; desde
/// Fase 2 publicador 4/4 (Files) acepta cualquier nombre de schema y se
/// invoca una vez por DbContext de módulo enrolado con Wolverine.
///
/// Diseño: arrancar un <see cref="IHost"/> efímero con <c>UseWolverine</c> y
/// <c>AutoBuildMessageStorageOnStartup = CreateOrUpdate</c>; el DDL se aplica
/// durante <see cref="IHost.StartAsync"/> (path canónico de Wolverine). Tras
/// confirmar las 4 tablas se detiene y descarta el mini-host.
///
/// Reusado por:
///   - <c>FSH.Starter.DbMigrator</c> (Step 2b en producción)
///   - <c>FshWebApplicationFactory</c> (harness de tests post-migrations)
///
/// La connection string y el nombre del schema viajan SIEMPRE por parámetro —
/// el helper no lee configuración por su cuenta. Esto bloquea por construcción
/// el bug donde la connection string se capturaba fuera del callback
/// <c>UseWolverine</c> en el API, apuntando al Postgres equivocado.
/// </summary>
#pragma warning disable CA1515 // Public on purpose: shared by Integration.Tests harness — single source of truth.
public static partial class WolverineSchemaSetup
#pragma warning restore CA1515
{
    private static readonly string[] RequiredTables =
    [
        "wolverine_outgoing_envelopes",
        "wolverine_incoming_envelopes",
        "wolverine_dead_letters",
        "wolverine_nodes",
    ];

    public static async Task ApplyAsync(string connectionString, string schemaName, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentNullException.ThrowIfNull(logger);

        WolverineSchemaSetupLog.Applying(logger, schemaName);

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });

        // Captura local: el callback de UseWolverine corre durante Build(). La
        // connection string llega por parámetro, así que cualquier lectura tardía
        // de configuración queda fuera del path (mismo motivo: blindar contra el
        // bug del API que capturaba la connection string fuera del callback).
        var cs = connectionString;
        var schema = schemaName;
        builder.UseWolverine(opts =>
        {
            // Discovery desactivado: el mini-host SOLO existe para aplicar DDL,
            // no descubre handlers ni publica.
            opts.Discovery.DisableConventionalDiscovery();
            opts.PersistMessagesWithPostgresql(cs, schema);
            opts.AutoBuildMessageStorageOnStartup = AutoCreate.CreateOrUpdate;
        });

        using var host = builder.Build();

        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        WolverineSchemaSetupLog.Started(logger);

        try
        {
            var existing = await ListSchemaTablesAsync(cs, schema, cancellationToken).ConfigureAwait(false);
            var missing = RequiredTables.Where(t => !existing.Contains(t, StringComparer.Ordinal)).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Wolverine schema setup did not produce the expected tables in schema '{0}'. Missing: {1}. Present: {2}.",
                    schema,
                    string.Join(", ", missing),
                    string.Join(", ", existing.Where(t => t.StartsWith("wolverine_", StringComparison.Ordinal)))));
            }

            WolverineSchemaSetupLog.Confirmed(logger, schema);
        }
        finally
        {
            try
            {
                await host.StopAsync(cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // log-and-continue: stop-failure must not mask the success/failure of the actual setup
            catch (Exception stopEx)
#pragma warning restore CA1031
            {
                WolverineSchemaSetupLog.StopFailed(logger, stopEx);
            }
        }
    }

    private static partial class WolverineSchemaSetupLog
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "[wolverine-setup] applying schema in '{Schema}'…")]
        public static partial void Applying(ILogger logger, string schema);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "[wolverine-setup] mini-host started — DDL applied during StartAsync")]
        public static partial void Started(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "[wolverine-setup] confirmed 4/4 tables in schema '{Schema}'")]
        public static partial void Confirmed(ILogger logger, string schema);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "[wolverine-setup] mini-host StopAsync raised — ignored")]
        public static partial void StopFailed(ILogger logger, Exception ex);
    }

    private static async Task<List<string>> ListSchemaTablesAsync(string connectionString, string schemaName, CancellationToken cancellationToken)
    {
        await using var conn = new Npgsql.NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT tablename FROM pg_tables WHERE schemaname = @schema ORDER BY tablename;";
        cmd.Parameters.AddWithValue("@schema", schemaName);
        var names = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }
}
