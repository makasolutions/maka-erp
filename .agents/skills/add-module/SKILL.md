---
name: add-module
description: Scaffold a new bounded context — backend (runtime + Contracts) AND frontend (API module, TanStack Query, MakaGrid page, i18n, routes). Use when adding a distinct business domain (e.g. Warranties, HR). For a feature inside an existing module, use add-feature.
argument-hint: [ModuleName] [order-number]
---

# Add Module (Maka Full-Stack)

High-ceremony. The part people most often get wrong is **registration — a module must be wired in FOUR places** (Step 6).
Always read `.agents/rules/architecture.md` before starting.

Naming: Maka uses `Maka.Modules.{Name}` (not `FSH.Modules.{Name}` like the boilerplate).

---

## BACKEND

### Projects structure

```
src/Modules/{Name}/
├── Maka.Modules.{Name}/              ← runtime (internal)
│   ├── Domain/
│   ├── Data/
│   │   └── {Name}DbContext.cs
│   ├── Features/v1/
│   └── {Name}Module.cs
└── Maka.Modules.{Name}.Contracts/   ← public API only
    └── v1/
        ├── Authorization/
        │   └── {Name}Permissions.cs
        └── (Commands / Queries / DTOs / Events)
```

**Copy `.csproj` files from `Modules.Catalog`** and rename — don't hand-write project references.

---

### Step 1 — Module class (`{Name}Module.cs`)

`[FshModule]` is an **assembly attribute** (not class-level):

```csharp
[assembly: FshModule(typeof(Maka.Modules.{Name}.{Name}Module), {order})]

namespace Maka.Modules.{Name};

public sealed class {Name}Module : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        PermissionConstants.Register({Name}Permissions.All);
        builder.Services.AddHeroDbContext<{Name}DbContext>();
        builder.Services.AddScoped<IDbInitializer, {Name}DbInitializer>();

        // Only if this module publishes/handles integration events:
        // builder.Services.AddEventingCore(builder.Configuration);
        // builder.Services.AddEventingForDbContext<{Name}DbContext>();
        // builder.Services.AddIntegrationEventHandlers(typeof({Name}Module).Assembly);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<{Name}DbContext>(name: "db:{name}");
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();
        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/{name}")
            .WithTags("{Name}")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();
        // group.MapCreate{Entity}Endpoint();
    }
}
```

**Load order reference** (set `{order}` accordingly):

| Order | Module |
|-------|--------|
| 300 | Auditing |
| 350 | Files |
| 400 | Webhooks |
| 500 | Billing |
| 600 | Catalog |
| 650 | Inventory |
| 700 | Orders |
| 750 | CRM |
| 800 | WhatsApp |
| 850 | Logistics |
| 900 | Warranties |

If your module consumes events from another module, load **after** it.

---

### Step 2 — Permissions (`Contracts/Authorization/{Name}Permissions.cs`)

```csharp
namespace Maka.Modules.{Name}.Contracts.v1.Authorization;

public static class {Name}Permissions
{
    private const string Prefix = "{name}";

    public static class {Entity}
    {
        public const string View   = $"{Prefix}.{entity}.view";
        public const string Create = $"{Prefix}.{entity}.create";
        public const string Update = $"{Prefix}.{entity}.update";
        public const string Delete = $"{Prefix}.{entity}.delete";
    }

    public static readonly string[] All =
    [
        {Entity}.View,
        {Entity}.Create,
        {Entity}.Update,
        {Entity}.Delete,
    ];
}
```

---

### Step 3 — DbContext

```csharp
namespace Maka.Modules.{Name}.Data;

public sealed class {Name}DbContext : BaseDbContext
{
    public const string Schema = "{name}";  // lowercase snake_case

    public {Name}DbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<{Name}DbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<{Entity}> {Entities} => Set<{Entity}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Name}DbContext).Assembly);
        base.OnModelCreating(modelBuilder);   // MUST be last — applies tenant + soft-delete filters
    }
}
```

---

### Step 4 — Add projects to solution

```bash
dotnet sln src/FSH.Starter.slnx add \
  src/Modules/{Name}/Maka.Modules.{Name}/Maka.Modules.{Name}.csproj

dotnet sln src/FSH.Starter.slnx add \
  src/Modules/{Name}/Maka.Modules.{Name}.Contracts/Maka.Modules.{Name}.Contracts.csproj
```

Add `<ProjectReference>` to the runtime module from:
- `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj`
- `src/Host/FSH.Starter.Migrations.PostgreSQL/FSH.Starter.Migrations.PostgreSQL.csproj`

(No DbMigrator project in this repo — migrations run at startup via `FSH.Starter.Migrations.PostgreSQL`.)

---

### Step 5 — Migrations folder + initial migration

```bash
# Create the folder first
mkdir src/Host/FSH.Starter.Migrations.PostgreSQL/{Name}

# Initial migration
dotnet ef migrations add {Name}_Initial \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context {Name}DbContext \
  --output-dir {Name}
```

See **create-migration** skill for full details.

---

### Step 6 — ⚠️ Register in ALL FOUR places

Edit **only** `src/Host/FSH.Starter.Api/Program.cs` (this repo has a single host):

```csharp
// 1. Mediator assemblies — add TWO entries per module:
options.Assemblies = [
    // ... existing entries ...
    typeof(Maka.Modules.{Name}.Contracts.{Name}ContractsMarker).Assembly,  // Contracts
    typeof(Maka.Modules.{Name}.{Name}Module).Assembly,                      // Runtime
];

// 2. moduleAssemblies array:
Assembly[] moduleAssemblies =
[
    // ... existing entries ...
    typeof(Maka.Modules.{Name}.{Name}Module).Assembly,
];
```

**Missing Mediator marker → handlers silently undiscovered.**
**Missing assembly entry → module never loads.**

Create the `{Name}ContractsMarker` class in the Contracts project:

```csharp
namespace Maka.Modules.{Name}.Contracts;

/// <summary>Marker for Mediator assembly scanning.</summary>
public sealed class {Name}ContractsMarker;
```

---

### Step 7 — Build and test

```bash
dotnet build src/FSH.Starter.slnx                 # 0 errors
dotnet test src/Tests/Architecture.Tests           # boundary + tenant-isolation rules
```

Hit the new endpoint in Scalar (`https://localhost:7030/scalar`) to confirm it registers correctly.

---

## FRONTEND (`clients/dashboard`)

### Step 8 — API module (`src/api/{name}.ts`)

```typescript
import { apiFetch } from "@/api/api-fetch";

export type {Entity}Dto = {
  id: string;
  // mirror the backend DTO fields
};

export async function get{Entities}(): Promise<{Entity}Dto[]> {
  return apiFetch<{Entity}Dto[]>("/api/v1/{name}/{entities}");
}

export async function create{Entity}(
  data: Omit<{Entity}Dto, "id">
): Promise<{Entity}Dto> {
  return apiFetch<{Entity}Dto>("/api/v1/{name}/{entities}", {
    method: "POST",
    body: JSON.stringify(data),
  });
}
```

---

### Step 9 — List page with MakaGrid (`src/pages/{name}/{entities}.tsx`)

```tsx
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { MakaGrid } from "@/components/maka";
import { get{Entities}, type {Entity}Dto } from "@/api/{name}";

export function {Entities}Page() {
  const { t } = useTranslation("{name}");

  const { data = [], isLoading } = useQuery<{Entity}Dto[]>({
    queryKey: ["{entities}"],
    queryFn: get{Entities},
  });

  const columns: ColumnModel[] = [
    { field: "id", headerText: t("{name}:{entity}.id"), width: 100, isPrimaryKey: true },
    // Add more columns mirroring {Entity}Dto
  ];

  return (
    <div className="flex flex-col gap-4 p-6">
      <h1 className="text-xl font-semibold text-[var(--color-text-primary)]">
        {t("{name}:{entity}.title")}
      </h1>
      <MakaGrid
        dataSource={data}
        columns={columns}
        isLoading={isLoading}
        fileName="{entities}-export"
        height="calc(100vh - 200px)"
      />
    </div>
  );
}
```

---

### Step 10 — i18n keys

**Spanish first**, then English. Never skip.

`public/locales/es/{name}.json`:
```json
{
  "{entity}": {
    "title": "{Nombre en español}",
    "id": "ID"
  }
}
```

`public/locales/en/{name}.json`:
```json
{
  "{entity}": {
    "title": "{Name in English}",
    "id": "ID"
  }
}
```

---

### Step 11 — Route + navigation

**`src/routes.tsx`** — add lazy import and route:

```typescript
const {Entities}Page = lazyNamed(
  () => import("@/pages/{name}/{entities}"),
  "{Entities}Page"
);

// Inside AppShell children:
{ path: "{entities}", element: withSuspense(<{Entities}Page />) }
```

**`src/components/layout/nav-data.ts`** — add nav entry in the correct section:

```typescript
{
  to: "/{entities}",
  label: t("{name}:{entity}.title"),   // or hardcoded label key
  icon: YourIconFromLucide,
}
```

---

### Step 12 — Frontend build check

```bash
cd clients/dashboard
npm run build   # 0 TypeScript errors
npm run dev     # open http://localhost:5174/{entities}
```

Verify in both **Light** and **Dark** mode.

---

## Checklist

**Backend:**
- [ ] Two projects (`Maka.Modules.{Name}` + `.Contracts`), copied from Catalog csproj, renamed
- [ ] Projects added to `.slnx`, referenced from `Api` + `Migrations.PostgreSQL`
- [ ] `[assembly: FshModule(typeof({Name}Module), {order})]` (assembly-level, NOT class-level)
- [ ] `IModule`: `AddHeroDbContext<T>()`, `PermissionConstants.Register`, versioned group
- [ ] Eventing trio added **only if** module publishes/handles integration events
- [ ] `{Name}DbContext : BaseDbContext`, 4-arg ctor, schema set, `base.OnModelCreating` **last**
- [ ] `{Name}Permissions` in `Contracts/Authorization/`
- [ ] `{Name}ContractsMarker` class in Contracts root
- [ ] Registered in **all four** places in `Program.cs` (2× Mediator markers + 2× moduleAssemblies)
- [ ] Migrations folder created + initial migration runs without error
- [ ] `dotnet build` → 0 errors; Architecture.Tests → green

**Frontend:**
- [ ] `src/api/{name}.ts` with DTO type + `apiFetch` functions
- [ ] `{Entities}Page` uses `MakaGrid` (never `GridComponent` directly)
- [ ] All column `headerText` use `t()`
- [ ] i18n keys added to `es/{name}.json` **first**, then `en/{name}.json`
- [ ] Route added to `src/routes.tsx`
- [ ] Nav entry added to `nav-data.ts`
- [ ] `npm run build` → 0 TypeScript errors
- [ ] Verified visually in Light **and** Dark mode
