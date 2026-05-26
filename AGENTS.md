# Maka Omni-Commerce ERP

> A production-ready modular .NET 10 monolith + two React 19 apps, built on FullStackHero (FSH).

This file is the canonical technical guide for **all** AI coding tools.
`CLAUDE.md` is the bridge that imports this file and adds Maka business rules on top.
Edit FSH/technical conventions **here**; edit Maka business rules in `CLAUDE.md`.

Detailed conventions live in `.agents/rules/` — **read the relevant rule file before working in that area**.

---

## What this is

A **modular monolith** (Vertical Slice Architecture) ERP for Maka Solutions SAS (Colombia).
Backend in .NET 10 + two React 19 frontends (operator `clients/admin`, tenant `clients/dashboard`).

---

## Repo map

| Path | What |
|------|------|
| `src/BuildingBlocks/` | Shared framework libraries. **Protected — read `buildingblocks-protection.md` first.** |
| `src/Modules/{Name}/` | Bounded contexts. Runtime project + `.Contracts` project (its only public API). |
| `src/Host/FSH.Starter.Api` | Composition-root Web API host → https://localhost:7030 (`/scalar`) |
| `src/Host/FSH.Starter.AppHost` | .NET Aspire orchestrator |
| `src/Host/FSH.Starter.Migrations.PostgreSQL` | All EF migrations, per-module folders |
| `src/Tests/` | Per-module unit tests + Architecture.Tests + Integration.Tests |
| `clients/admin` | Operator dashboard → http://localhost:5173 |
| `clients/dashboard` | Tenant dashboard → http://localhost:5174 |

---

## Tech stack

### Backend

| Concern | Technology |
|---|---|
| Runtime | .NET 10 / C# latest |
| CQRS / Mediator | **Mediator 3.x** (source-gen — NOT MediatR) |
| Validation | FluentValidation 12.x |
| ORM / DB | EF Core 10 / PostgreSQL (Npgsql) |
| Auth | JWT Bearer + ASP.NET Identity |
| Multitenancy | Finbuckle 10.x |
| Cache | Redis (HybridCache) |
| Jobs | Hangfire + Hangfire.PostgreSql |
| Events | MassTransit 8.5.7 + RabbitMQ (**v8 only — Apache 2.0; v9 is commercial**) |
| Eventing (dev) | **InMemory** (default, no Docker dependency) |
| Eventing (prod) | RabbitMQ (configure in appsettings.Production.json) |
| Docs | OpenAPI + **Scalar** (NOT Swashbuckle) |
| Hosting | .NET Aspire |
| Testing | xUnit, Shouldly, NSubstitute, AutoFixture, NetArchTest, Testcontainers |

### Frontend (both apps)

| Concern | Technology |
|---|---|
| Framework | React 19 + Vite 7 + TypeScript |
| Data fetching | TanStack Query v5 |
| Routing | React Router 7 |
| UI | Radix + Tailwind v4 + CVA (shadcn-style) |
| Realtime | SignalR + SSE |
| i18n | **i18next + react-i18next** (`/locales/{lng}/{ns}.json`) |
| Complex UI | **Syncfusion 33.x** via `MakaGrid`, `MakaChart`, `MakaKanban`, `MakaPivot`, `MakaScheduler` wrappers |
| API client | Hand-written `apiFetch` (no codegen) |

---

## Build & run

```bash
# API only → http://localhost:5030 (http) / https://localhost:7030
dotnet run --project src/Host/FSH.Starter.Api

# Build backend
dotnet build src/FSH.Starter.slnx

# Tests (integration tests require Docker)
dotnet test src/FSH.Starter.slnx

# Dashboard frontend
cd clients/dashboard && npm install && npm run dev   # → http://localhost:5174

# Admin frontend
cd clients/admin && npm install && npm run dev       # → http://localhost:5173
```

Migrations (separate step — DB is **NOT** migrated at API startup):
```bash
dotnet ef migrations add {Name} \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {X}DbContext \
  --output-dir {X}
```

---

## Golden rules (do not break)

1. **Module boundaries** — a module references another module **only** through its `.Contracts` project, never its runtime. Enforced by `Architecture.Tests`.
2. **Four-place registration footgun** — adding a module requires editing `Program.cs` (Mediator assemblies × 2 + moduleAssemblies) AND the identical pair in `DbMigrator/Program.cs`. A missing Mediator marker = handlers silently undiscovered.
3. **Tenant isolation is default-ON** via `BaseDbContext`. Opt out only via `IGlobalEntity`. Subclass DbContexts call `base.OnModelCreating` **last**.
4. **Do NOT modify `src/BuildingBlocks`** without explicit approval from Juan — shared by every module, wide blast radius.
5. **Mediator handlers must be `public sealed`**, return `ValueTask<T>`, and `.ConfigureAwait(false)` every await.
6. **Structured logging only** — no string interpolation in log messages; use message templates / `[LoggerMessage]`.
7. **Propagate `CancellationToken`** into every EF/IO call.
8. **Every command handler + paginated query handler needs a validator**. Enforced by `Architecture.Tests`.
9. **i18n is mandatory** — every user-visible string must use `t('namespace:key')`. Add the key to `es/` **first**, then `en/`. See `add-translation` skill.
10. **Syncfusion wrappers are mandatory** — use `MakaGrid`, `MakaChart`, `MakaKanban`, `MakaPivot`, `MakaScheduler`. Never instantiate Syncfusion components directly.
11. **CSS tokens, never hardcoded colors** — use `var(--color-text-primary)`, `var(--color-accent)`, etc. Syncfusion canvas components resolve via `getComputedStyle` on mount.
12. **RabbitMQ = production only** — development uses `EventingOptions.Provider: "InMemory"` to avoid Docker network hangs.
13. **MassTransit v8.5.7 only** — v9 is commercial. Do not upgrade.
14. **Frontend: pass per-call data through `mutate(arg)`**, never via state the mutation callbacks close over (execute-time race).
15. **GET handlers are read-only** — never write to the database in a query handler. Use the upsert pattern in the PUT handler instead.

---

## Rules index — read the relevant file before you work

**Backend / cross-cutting** (`.agents/rules/`)

| Working on… | Read |
|---|---|
| Module structure, boundaries, registration, DI | `architecture.md` |
| Endpoints, CQRS, validation, exceptions, permissions | `api-conventions.md` |
| EF Core, entities, migrations, tenant isolation | `database.md` |
| Cross-module events, Outbox/Inbox, idempotent handlers | `eventing.md` |
| Caching (HybridCache/Redis), keys, invalidation | `caching.md` |
| Background jobs (Hangfire), recurring jobs | `jobs.md` |
| Real-time (SignalR/SSE) | `realtime.md` |
| Files/blobs, presigned uploads | `storage.md` |
| CORS, security headers, rate limiting | `security.md` |
| Logging, correlation, OpenTelemetry | `logging.md` |
| Unit tests, NetArchTest | `testing.md` |
| Integration tests (Testcontainers) | `integration-testing.md` |
| **Modifying `src/BuildingBlocks`** | `buildingblocks-protection.md` |

**Frontend** (`.agents/rules/frontend/`)

| Working on… | Read |
|---|---|
| Any React work (shared stack, API client, Query, Tailwind, i18n) | `frontend/shared.md` |
| The operator app (`clients/admin`) | `frontend/admin.md` |
| The tenant app (`clients/dashboard`) | `frontend/dashboard.md` |

---

## Skills index

| Task | Skill |
|---|---|
| Add command/query + handler + endpoint to existing module | `add-feature` |
| Add Syncfusion grid page | `add-syncfusion-grid` |
| Scaffold a new business module (backend + frontend) | `add-module` |
| Add a React page (list + CRUD) | `add-react-page` |
| Add integration event (cross-module) | `add-integration-event` |
| Create EF Core migration | `create-migration` |
| Add i18n translation keys | `add-translation` |

---

## Coding style (backend)

File-scoped namespaces · 4-space indent · explicit types (`var` only when RHS-obvious) ·
`is null` / `is not null` · pattern matching + switch expressions ·
`ArgumentNullException.ThrowIfNull` guards · records for DTOs/events/value objects ·
`default!` for required non-nullable strings. Build runs with `TreatWarningsAsErrors`.

## Commit conventions

```
feat(inventory): add SerialNumber entity with warranty tracking
fix(identity): correct outbox dispatcher to prevent duplicate RabbitMQ publishes
refactor(catalog): extract Product mapping to extension methods
chore(db): add migration Identity_AddTenantLocalization
test(inventory): add integration tests for stock transfer flow
```
