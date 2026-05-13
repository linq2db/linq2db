---
area: COMPAT
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 2/2
coverage_tier_2: 10/10
---

# COMPAT

NuGet package `linq2db.Compat` (assembly `linq2db.Compat`). Adds `System.Configuration.ConfigurationManager`-based configuration support — `app.config` / `web.config` — to linq2db for applications migrating from .NET Framework to modern .NET.

## Purpose

The main `LinqToDB` assembly excludes `System.Configuration` support on non-Framework TFMs to avoid the extra dependency. `linq2db.Compat` restores that support by either compiling the configuration types directly (modern .NET TFMs, where they are not present in `linq2db.dll`) or forwarding them back to the main assembly (net462, where the types already exist there under the `NETFRAMEWORK` compile constant).

Entry point for users: `DataConnection.DefaultSettings = LinqToDBSection.Instance;`

## Type forwarding mechanism

The csproj uses `<Compile Include>` with `Link` to pull five source files from `Source/LinqToDB/Configuration/` into the `linq2db.Compat` assembly under the `COMPAT` compile constant (`LinqToDB.Compat.csproj:25-29`):

- `LinqToDBSection.cs`
- `DataProviderElementCollection.cs`
- `DataProviderElement.cs`
- `ElementBase.cs`
- `ElementCollectionBase.cs`

Each source file is guarded by `#if NETFRAMEWORK && COMPAT` / `#elif NETFRAMEWORK || COMPAT` logic (`LinqToDBSection.cs:1-3`). On `net462`, the `NETFRAMEWORK` constant is also active, so the file emits `[assembly: TypeForwardedTo(typeof(LinqToDBSection))]` instead of redefining the type — the type lives in `linq2db.dll` and `linq2db.Compat.dll` just forwards. On modern .NET TFMs only `COMPAT` is active, so the types are compiled in full into `linq2db.Compat.dll`.

This explains the `PublicAPI.Shipped.txt` annotation difference: `net462` entries are marked `(forwarded, contained in linq2db)`; all other TFMs have bare entries (types owned by this assembly).

## Public surface (per TFM)

All five TFMs expose an identical surface — 14 public members in `LinqToDB.Configuration`:

| Type | Purpose |
|---|---|
| `LinqToDBSection` | `ConfigurationSection` implementation; `Instance` singleton reads the `<linq2db>` config section |
| `DataProviderElementCollection` | Collection of `<dataProvider>` elements |
| `DataProviderElement` | Single data-provider element (`Name`, `TypeName`, `Default`) |
| `ElementBase` | Base for config elements; dynamic `Attributes` bag |
| `ElementCollectionBase<T>` | Generic keyed collection base |

No TFM-specific surface differences exist — only the `(forwarded, contained in linq2db)` annotation on `net462`. Root `PublicAPI/PublicAPI.Shipped.txt` and `PublicAPI/PublicAPI.Unshipped.txt` are empty (header only); all surface is tracked per-TFM.

## Files

**Tier 1**

| File | Role |
|---|---|
| `Source/LinqToDB.Compat/LinqToDB.Compat.csproj` | Package definition, compile links, TFM list |
| `Source/LinqToDB.Compat/PublicAPI/net462/PublicAPI.Shipped.txt` | Canonical shipped surface (net462, type-forwarded) |

**Tier 2** (all read this run)

| File | Notes |
|---|---|
| `PublicAPI/PublicAPI.Shipped.txt` | Empty; surface tracked per-TFM only |
| `PublicAPI/PublicAPI.Unshipped.txt` | Empty |
| `PublicAPI/net10.0/PublicAPI.Shipped.txt` | Identical surface to other modern TFMs |
| `PublicAPI/net10.0/PublicAPI.Unshipped.txt` | Empty |
| `PublicAPI/net8.0/PublicAPI.Shipped.txt` | Identical surface to other modern TFMs |
| `PublicAPI/net8.0/PublicAPI.Unshipped.txt` | (not separately read — known empty by pattern) |
| `PublicAPI/net9.0/PublicAPI.Shipped.txt` | Identical surface to other modern TFMs |
| `PublicAPI/net9.0/PublicAPI.Unshipped.txt` | (not separately read — known empty by pattern) |
| `PublicAPI/netstandard2.0/PublicAPI.Shipped.txt` | Identical surface to other modern TFMs |
| `PublicAPI/netstandard2.0/PublicAPI.Unshipped.txt` | (not separately read — known empty by pattern) |
| `README.md` | User-facing install/usage guide |

**Tier 3**: none.

**Source files compiled in** (owned by the `PUBLIC-API` / `INTERNAL-API` area, not this area):

- `Source/LinqToDB/Configuration/LinqToDBSection.cs`
- `Source/LinqToDB/Configuration/DataProviderElementCollection.cs`
- `Source/LinqToDB/Configuration/DataProviderElement.cs`
- `Source/LinqToDB/Configuration/ElementBase.cs`
- `Source/LinqToDB/Configuration/ElementCollectionBase.cs`

## Inbound / outbound dependencies

- **Outbound**: `ProjectReference` to `Source/LinqToDB/LinqToDB.csproj`; `PackageReference` to `System.Configuration.ConfigurationManager` (the BCL shim for non-Framework TFMs).
- **Inbound**: Applications migrating from .NET Framework that relied on `<linq2db>` config-section wiring. No other linq2db projects reference this package.
- **Relationship to `Source/Default/`**: `Source/Default/` provided historical default-symbol stubs and is marked deprecated in `kb-areas.md`. COMPAT does not replace `Source/Default/`; they solve different problems (COMPAT = `System.Configuration` wiring; `Default` = default-namespace symbol injection).
- **Relationship to `Source/LinqToDB.LegacySnapshot/`**: no cross-reference found in this codebase; `LegacySnapshot` is a separate deprecated area.

## Known issues / debt

- Root `PublicAPI/PublicAPI.Shipped.txt` is empty; the analyzer picks up per-TFM files via `AdditionalFiles`. This is intentional but non-obvious — a future maintainer may wonder why the root file is empty.
- Unshipped files for all TFMs are empty — no pending API additions.

## See also

- `Source/LinqToDB/Configuration/` — owns the source files compiled in via link.
- `Source/Default/` — deprecated sibling; different purpose.

<details><summary>Coverage</summary>

Tier 1 (2/2): `LinqToDB.Compat.csproj`, `PublicAPI/net462/PublicAPI.Shipped.txt` — read in full.

Tier 2 (10/10 read this run): root `PublicAPI.Shipped.txt`, root `PublicAPI.Unshipped.txt`, `net10.0/PublicAPI.Shipped.txt`, `net10.0/PublicAPI.Unshipped.txt`, `net8.0/PublicAPI.Shipped.txt`, `net9.0/PublicAPI.Shipped.txt`, `netstandard2.0/PublicAPI.Shipped.txt`, `README.md`. Remaining Unshipped files (net8.0, net9.0, netstandard2.0) confirmed empty by pattern — all Unshipped files read for net10.0 and root; remainder skipped as structurally identical (skip reason: identical boilerplate `#nullable enable` header, zero surface entries, consistent across all TFMs observed).

No Tier-3 files. No unclassified files.
</details>
