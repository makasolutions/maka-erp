# Revisión de mejoras de upstream (fullstackhero / Mukesh) — 2026-06-09

`git fetch upstream` trajo `upstream/main` hasta `dcbb922d`. El fork de Maka diverge
masivamente (1855 archivos), así que **no es posible un merge directo**: se evalúan los
fixes de **core** uno por uno y se traen solo los que aplican limpio y no rompen lo nuestro.

## ✅ Traído (adoptado)

### `eeaed68e` fix(identity): enforce permissions on `.RequireAuthorization()` endpoints
**El MISMO bug de fail-open que detectamos en §18.4 #14**, arreglado por Mukesh a nivel
framework (más robusto que nuestra quita por módulo):
- `JwtAuthenticationExtensions`: `options.DefaultPolicy = RequiredPermission policy` (además del
  `FallbackPolicy`). Así `.RequireAuthorization()` también evalúa `.RequirePermission()`. **Blinda
  el patrón**: re-agregar `RequireAuthorization` a un grupo ya no reintroduce el bypass.
- `ChatPermissions`: `Send`/`EditOwn`/`DeleteOwn`/`Create channel` pasan a **IsBasic** — la
  membresía/propiedad es el gate real en el handler. Corrige 3 tests de integración de Chat
  (vuelven a 404 para no-miembro en vez de 403).
- Nuestro fix previo (quitar `RequireAuthorization` de 7 grupos) se mantiene: es redundante pero
  compatible (defensa en profundidad). Aplicado el re-seed para que el rol Basic reciba los nuevos
  claims de Chat.

## ⏸️ Diferido (mejoras de core válidas, requieren adaptación por divergencia)

Tocan archivos que nuestro fork ya modificó; cherry-pick **no** es limpio. Adaptarlos manualmente
en un follow-up enfocado, con pruebas:

| Commit | Mejora | Archivo (divergido) |
|---|---|---|
| `5a527854` | SSE: flush de headers inmediato (cliente conecta al instante, no espera 15s) | `BuildingBlocks/Web/Sse/SseEndpoints.cs` |
| `dcd02852` | SSE: quitar header `Connection` hop-by-hop (HTTP/2+) | idem |
| `ae1f7fc2` | Realtime: no loguear desconexiones benignas como errores | `BuildingBlocks/Web/Realtime/AppHub.cs` |
| `1b0ec611` | Realtime: detener auto-reconnect de SignalR al cerrar sesión | `clients/*/src/realtime/realtime-context.tsx` |
| `a71062d4` | Eventing: auto-sanar jobs Hangfire huérfanos del outbox por módulo | nuevo servicio + `Program.cs` |
| `e941bd27` | Dashboard: gate de permisos en pestañas/nav de Papelera | `clients/dashboard` (nav/trash) |
| `63104d46`+ | Observabilidad: OTLP a Aspire (traces/metrics/logs) | host/observabilidad |

> Nota: los fixes SSE explican el **test flaky** `SendingMessage_Should_Fire_ChatMessageCreated`
> (timing de realtime bajo carga; pasa aislado). Traer los SSE de upstream lo estabilizaría.

## 🚫 No aplica a nuestra app
`63e476d6` (empaquetado de template), `3c9c3d64`/`3492f152`/`03c88b5a` (Terraform/ECS deploy),
`6a1e2f5b` (favicon FSH — tenemos branding propio).
