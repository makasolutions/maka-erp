using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.V2.Exceptions;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-D3 — jerarquía de terceros end-to-end: recursive CTE de ancestros, validación de ciclos
/// real, y el Restrict del self-FK (no se puede hard-deletear una matriz con hijos).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartiesV2HierarchyTests
{
    private readonly FshWebApplicationFactory _factory;

    public PartiesV2HierarchyTests(FshWebApplicationFactory factory) => _factory = factory;

    private static AppTenantInfo Root => new(TestConstants.RootTenantId, TestConstants.RootTenantId);

    private async Task<T> InTenant<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Root);
        return await action(scope.ServiceProvider);
    }

    private static Party NewParty(string suffix) =>
        Party.Create("NIT", $"h-{suffix}-{Guid.NewGuid():N}"[..14], 1, PartyKind.Juridica, $"Tercero {suffix}");

    [Fact]
    public async Task GetAncestorIds_Returns_Full_Chain_Via_Recursive_Cte()
    {
        // matriz ← sucursal ← contacto (3 niveles).
        var (matrizId, sucursalId, contactoId) = await InTenant(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var hier = sp.GetRequiredService<PartyHierarchyService>();

            var matriz = NewParty("matriz");
            db.Parties.Add(matriz);
            await db.SaveChangesAsync();

            var sucursal = NewParty("sucursal");
            sucursal.AssignParent(matriz.Id, await hier.GetAncestorIdsAsync(matriz.Id));
            db.Parties.Add(sucursal);
            await db.SaveChangesAsync();

            var contacto = NewParty("contacto");
            contacto.AssignParent(sucursal.Id, await hier.GetAncestorIdsAsync(sucursal.Id));
            db.Parties.Add(contacto);
            await db.SaveChangesAsync();

            return (matriz.Id, sucursal.Id, contacto.Id);
        });

        await InTenant(async sp =>
        {
            var hier = sp.GetRequiredService<PartyHierarchyService>();
            var ancestors = await hier.GetAncestorIdsAsync(contactoId);
            ancestors.ShouldBe(new[] { sucursalId, matrizId }, ignoreOrder: true);
            return 0;
        });
    }

    [Fact]
    public async Task AssignParent_Creating_Transitive_Cycle_Throws()
    {
        // matriz ← sucursal. Intentar matriz.AssignParent(sucursal) cierra el ciclo.
        await Should.ThrowAsync<PartyHierarchyCycleException>(async () =>
        {
            await InTenant(async sp =>
            {
                var db = sp.GetRequiredService<PartiesDbContext>();
                var hier = sp.GetRequiredService<PartyHierarchyService>();

                var matriz = NewParty("m");
                db.Parties.Add(matriz);
                await db.SaveChangesAsync();

                var sucursal = NewParty("s");
                sucursal.AssignParent(matriz.Id, await hier.GetAncestorIdsAsync(matriz.Id));
                db.Parties.Add(sucursal);
                await db.SaveChangesAsync();

                // Ancestros de la sucursal = {matriz}; matriz está entre ellos → ciclo.
                var matrizTracked = await db.Parties.SingleAsync(p => p.Id == matriz.Id);
                matrizTracked.AssignParent(sucursal.Id, await hier.GetAncestorIdsAsync(sucursal.Id));
                return 0;
            });
        });
    }

    [Fact]
    public async Task Matriz_With_Children_Restrict_Blocks_Real_Delete()
    {
        // Party es ISoftDeletable: Remove() se intercepta a soft-delete (UPDATE IsDeleted), que NO
        // dispara el Restrict (de hecho deja a los hijos apuntando a una matriz soft-deleted — la
        // deuda D5 anotada en PartyConfiguration). El Restrict del self-FK es backstop de un DELETE
        // REAL (out-of-band); lo provocamos con ExecuteDelete, que bypassa el interceptor.
        var matrizId = await InTenant(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var hier = sp.GetRequiredService<PartyHierarchyService>();

            var matriz = NewParty("del-m");
            db.Parties.Add(matriz);
            await db.SaveChangesAsync();

            var sucursal = NewParty("del-s");
            sucursal.AssignParent(matriz.Id, await hier.GetAncestorIdsAsync(matriz.Id));
            db.Parties.Add(sucursal);
            await db.SaveChangesAsync();
            return matriz.Id;
        });

        await Should.ThrowAsync<Npgsql.PostgresException>(async () =>
        {
            await InTenant(async sp =>
            {
                var db = sp.GetRequiredService<PartiesDbContext>();
                await db.Parties.Where(p => p.Id == matrizId).ExecuteDeleteAsync(); // Restrict bloquea
                return 0;
            });
        });
    }
}
