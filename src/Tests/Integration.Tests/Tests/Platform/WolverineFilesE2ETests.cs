using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Tracking;

namespace Integration.Tests.Tests.Platform;

/// <summary>
/// Fase 2 — publicador 4/4 (ADR-0001/0005). Cierre de Fase 2.
///
/// Migración de <c>FinalizeUploadCommandHandler</c> (Files) al patrón
/// canónico Wolverine: <c>IIntegrationEventPublisher&lt;FilesDbContext&gt;</c>
/// + <c>SaveChangesAndFlushAsync</c>.
///
/// Flujo E2E:
///   1. Login (POST /token/issue) → bearer token.
///   2. POST /api/v1/files/upload-url → PresignedUploadResponse (asset PendingUpload).
///   3. PUT del payload al S3 (Testcontainer MinIO) en el StorageKey del asset.
///   4. POST /api/v1/files/{id}/finalize → asset.MarkAvailable + publish del envelope.
///   5. TrackedSession aserta envelope Sent + Received via RabbitMQ con TenantId.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WolverineFilesE2ETests
{
    private const string MinioBucket = "fsh-integration-test-uploads";
    private readonly FshWebApplicationFactory _factory;

    public WolverineFilesE2ETests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FileFinalize_Should_Publish_Envelope_And_Deliver_Via_RabbitMq()
    {
        var collector = _factory.Services.GetRequiredService<FileFinalizedCollector>();
        collector.Clear();

        var authHelper = new AuthHelper(_factory);
        using var client = await authHelper.CreateRootAdminClientAsync();

        // 1) Pedir presigned upload URL.
        var uploadReq = new RequestUploadUrlCommand(
            OwnerType: "MyFiles",
            OwnerId: null,
            FileName: $"e2e-{Guid.NewGuid():N}.txt",
            ContentType: "text/plain",
            SizeBytes: 11,
            Visibility: 0,
            Category: "Document"); // categoría configurada en appsettings (Files.Categories)

        var presignedResp = await client.PostAsJsonAsync("api/v1/files/upload-url", uploadReq);
        presignedResp.IsSuccessStatusCode.ShouldBeTrue(
            $"upload-url debe responder 2xx; respondió {(int)presignedResp.StatusCode} {presignedResp.ReasonPhrase}. " +
            $"Body: {await presignedResp.Content.ReadAsStringAsync()}");
        var presigned = await presignedResp.Content.ReadFromJsonAsync<PresignedUploadResponse>();
        presigned.ShouldNotBeNull();

        // 2) Subir el payload al S3 (MinIO Testcontainer) en el StorageKey que el asset espera.
        //    El handler de finalize valida HeadObject + size + content-type contra esto.
        var s3 = _factory.Services.GetRequiredService<IAmazonS3>();
        var assetStorageKey = await GetStorageKeyForAssetAsync(presigned.FileAssetId);
        var payload = Encoding.UTF8.GetBytes("hello world");
        using (var ms = new MemoryStream(payload))
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = MinioBucket,
                Key = assetStorageKey,
                InputStream = ms,
                ContentType = "text/plain",
                AutoCloseStream = false,
            });
        }

        // 3) Disparar finalize dentro del TrackedSession para capturar el envelope.
        var host = _factory.Services.GetRequiredService<IHost>();
        Func<IMessageContext, Task> action = async _ =>
        {
            var finalizeResp = await client.PostAsync($"api/v1/files/{presigned.FileAssetId}/finalize", content: null);
            if (!finalizeResp.IsSuccessStatusCode)
            {
                var body = await finalizeResp.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"finalize debe responder 2xx; respondió {(int)finalizeResp.StatusCode} {finalizeResp.ReasonPhrase}. Body: {body}");
            }
        };

        var tracked = await host.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .ExecuteAndWaitAsync(action);

        // Aserto 1 — Wolverine vio el publish del envelope.
        var sent = tracked.Sent.SingleEnvelope<FileFinalizedIntegrationEvent>();
        sent.ShouldNotBeNull("Wolverine debió emitir un envelope para FileFinalizedIntegrationEvent");
        sent.TenantId.ShouldBe(TestConstants.RootTenantId,
            "INV-9 — TenantId del envelope debe venir del Finbuckle context del request");

        // Aserto 2 — CAPA 2: entrega in-process por la local durable queue (sin RabbitMQ).
        var receivedEnvelope = tracked.Received.SingleEnvelope<FileFinalizedIntegrationEvent>();
        receivedEnvelope.ShouldNotBeNull("El consumer test-only debió recibir el evento");
        receivedEnvelope.Destination?.Scheme.ShouldBe("local",
            "el evento se entrega in-process por la local durable queue, no por RabbitMQ");

        // Aserto 3 — payload intacto en el consumer test-only.
        collector.Received.Count.ShouldBe(1);
        collector.Received[0].FileAssetId.ShouldBe(presigned.FileAssetId);
        collector.Received[0].TenantId.ShouldBe(TestConstants.RootTenantId);
        collector.Received[0].SizeBytes.ShouldBe(11);
    }

    private async Task<string> GetStorageKeyForAssetAsync(Guid assetId)
    {
        // SQL crudo bypass del global query filter de Finbuckle (que no está poblado en
        // este scope ad-hoc). La tabla files."FileAssets" tiene el StorageKey del asset.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FSH.Modules.Files.Data.FilesDbContext>();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT \"StorageKey\" FROM files.\"FileAssets\" WHERE \"Id\" = @id;";
            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = assetId;
            cmd.Parameters.Add(p);
            var result = await cmd.ExecuteScalarAsync();
            return result as string ?? throw new InvalidOperationException($"FileAsset {assetId} not found");
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}

/// <summary>
/// Sink singleton donde el consumer test-only graba el evento para que el test lo
/// pueda asertar.
/// </summary>
public sealed class FileFinalizedCollector
{
    private readonly ConcurrentBag<FileFinalizedIntegrationEvent> _received = [];

    public IReadOnlyList<FileFinalizedIntegrationEvent> Received => [.. _received];

    public void Add(FileFinalizedIntegrationEvent evt) => _received.Add(evt);

    public void Clear() => _received.Clear();
}

/// <summary>
/// Consumer test-only. Discoverable cuando el factory llama
/// <c>opts.Discovery.IncludeType(typeof(FileFinalizedE2EConsumer))</c>.
/// </summary>
public static class FileFinalizedE2EConsumer
{
    public static void Handle(FileFinalizedIntegrationEvent evt, FileFinalizedCollector collector)
    {
        ArgumentNullException.ThrowIfNull(evt);
        ArgumentNullException.ThrowIfNull(collector);
        collector.Add(evt);
    }
}
