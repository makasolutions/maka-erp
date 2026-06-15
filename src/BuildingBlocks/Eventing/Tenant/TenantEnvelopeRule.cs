// Outgoing rule INV-9 diferido a Fase 4 — ver
// docs/specs/platform/wolverine-phase1-followups.md, sección "Outgoing rule INV-9".
//
// Causa raíz: WolverineOptions.MetadataRules requiere instancias eager-build, antes
// de que el IServiceProvider esté disponible. La implementación que dependa de
// IMultiTenantContextAccessor<AppTenantInfo> (Finbuckle) requiere un hook post-DI-build
// que la API XML doc de Wolverine 6.8 no clarifica. Investigación adicional en Fase 4.
//
// Mientras tanto: los publicadores de Fase 2 propagan TenantId vía DeliveryOptions.TenantId
// explícito en IIntegrationEventPublisher<T>.PublishAsync. El rule sería defensa
// adicional para eventos cascadeados — escenario que no existe aún.
