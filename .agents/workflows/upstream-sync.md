---
description: Bring improvements from the FullStackHero (FSH) upstream into the Maka fork safely and selectively, without clobbering Maka work. Use when checking what's new upstream or pulling a specific upstream fix/feature.
---

# Upstream sync (FSH → Maka) — selective & safe

Mukesh keeps improving the FSH core. We want to keep pulling the valuable bits
**without** colliding with what we've built on top. We do **selective** syncs,
never a blind full merge.

## Remotes

```
origin    → github.com/makasolutions/maka-erp.git   (our fork)
upstream  → github.com/fullstackhero/dotnet-starter-kit.git  (FSH)
```

`git fetch upstream` is read-only and safe to run anytime.

## 1. Triage — what's new and what's safe

```bash
bash scripts/upstream-report.sh        # defaults: upstream main
```

It fetches upstream and lists every upstream commit not in our `HEAD`, classified:

- **✅ SAFE** — touches only files we have **not** customized since the fork
  point (`merge-base`). These apply with no conflict against our work.
- **⚠️ REVIEW** — touches at least one file we changed → must be resolved by hand
  (the report prints which files overlap).

> **SAFE ≠ "must take".** SAFE only means *it won't clobber our work*. A brand-new
> upstream **feature** (e.g. the tenant-billing lifecycle) shows up as SAFE because
> it's all new files — but adopting a feature is a **product decision**, not an
> automatic pull. Bring **fixes** freely; bring **features** deliberately.

## 2. Apply a single fix surgically (the proven pattern)

For a SAFE commit whose valuable change is one (or few) FSH file(s):

```bash
git tag -f safety/pre-pick HEAD                      # instant rollback point

# Only when OUR file equals the commit's PARENT (no local divergence on it):
#   git diff <commit>~1:<path> HEAD:<path>  →  empty  ⇒ checkout = exactly the fix
git checkout <commit> -- <path/to/file>

git diff --cached -- <path/to/file>                  # review: is it ONLY the intended change?
```

Build & verify (the API locks output DLLs while running — **stop it first**):

```bash
# PowerShell: Get-Process dotnet | Stop-Process -Force
dotnet build src/FSH.Starter.slnx                    # must be 0 errors / 0 warnings
# (or build just the touched module to skip the host copy: dotnet build src/Modules/<M>/<M>.csproj)
```

Commit atomically (one fix per commit), crediting upstream:

```bash
git commit -m "fix(<area>): <short desc> (cherry-pick from FSH <commit>)"
```

Rollback if anything looks off: `git reset --hard safety/pre-pick`.

When our file has diverged (REVIEW), don't `checkout` — open the upstream diff
(`git show <commit> -- <path>`) and hand-apply only the relevant hunk.

## 3. Never auto-pull (decide case by case, off this flow)

- **Redis → Valkey** and other infra swaps (`deploy/docker/**`, `AppHost.cs`).
- **`Directory.Packages.props` / package-version bumps** (React especially — a
  19.x bump has bitten the Syncfusion+Vite dev render before).
- **`clients/dashboard/**`** — heavy Maka customization (i18n, Syncfusion `Maka*`
  wrappers, theming). Hand-review only.
- **`CLAUDE.md` / `AGENTS.md` / `.agents/**`** — ours are Maka-tailored; merge by
  hand, keep our content.
- Full upstream **features** (billing lifecycle, etc.) — product call.

## 4. Keep the conflict surface small (the real long-term fix)

The fewer FSH-owned files we edit, the more upstream merges cleanly:

- Keep Maka work **isolated**: future Maka modules (`Inventory`, `CRM`, `Orders`,
  `Logistics`, `Warehouse`, `Imports`), the `Maka*` Syncfusion wrappers, and
  `clients/dashboard`.
- **Do not edit `src/BuildingBlocks`** or other FSH-owned files directly (already
  a golden rule) — so Mukesh's changes there land conflict-free.
- Prefer composition/extension over modifying FSH files in place.

## Conflict-priority cheatsheet (if a manual merge is ever unavoidable)

| Area | Prefer |
|---|---|
| `src/BuildingBlocks/**` | upstream |
| FSH modules (Catalog, Billing, Chat, Files, Tickets, Notifications, Webhooks) | upstream |
| Identity, Multitenancy, Auditing | review each hunk |
| Maka modules (Inventory, CRM, Orders, Logistics, Warehouse, Imports) | ours |
| `clients/admin/**` | upstream |
| `clients/dashboard/**` | review (ours by default) |
| `CLAUDE.md` / `AGENTS.md` / `.agents/**` | ours (fold in useful upstream bits) |
| `docker-compose*`, `Directory.Packages.props` | review manually |
