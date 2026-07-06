# Per-package update rules and release-notes URLs

Accrued across releases by [`release-deps`](../../skills/release-deps/SKILL.md). One entry per package that has a non-default update rule, a known release-notes URL, or a documented gotcha.

## Schema

Each entry is a heading + bullet list:

```markdown
## <PackageId>

- **Release notes URL:** <url>  <!-- omit if unknown -->
- **Update rule:** <terse rule, e.g. "always pin to .NET 8.0.0 unless flagged vulnerable" or "bump requires regenerating T4 templates in Tests.T4">
- **Co-bump:** <other-package-ids that must move together>  <!-- omit if not applicable -->
- **Last verified:** <iso-date> on release <version>
```

When a rule is added or amended, the modifying skill prompts session-reload.

## Categories

- **Shipping runtime packages:** `Microsoft.Extensions.*`, `System.*` referenced by published linq2db assemblies. Default rule: pin to the initial .NET version (e.g. 8.0.0 / 9.0.0); only bump if flagged vulnerable.
- **Test-only references:** opposite rule — latest stable is always proposed.
- **Analyzers:** prerelease versions allowed (analyzers are dev-time only, not transitively visible to consumers).
- **Database providers:** per-provider rules vary widely (some providers have schema-init compatibility quirks tied to specific versions). Capture each as a row when encountered.
- **Self-references:** `linq2db.t4models` is bumped post-release by `/release-postpublish` step 4 to the just-released version; not by `/release-deps`.
- **`*LatestForNuget` / `*LatestNuget` MSBuild properties** (e.g. `$(Net8LatestForNuget)`, `$(EF3LatestNuget)`): contain the **lowest** / initial version of a .NET-X / EF-X line, used by published nuspec contracts. **Always pinned at `X.0.0` (or for EF: at the lowest supported minor, e.g. `3.1.0` for EF Core 3.1).** Do **not** ask per-package for any `<PackageVersion>` whose `Version="$(*LatestForNuget|*LatestNuget)"`. Only change when the pinned version becomes unlisted on nuget.org or is flagged vulnerable — in that case bump to the next stable X.0.y.
- **`*Latest` MSBuild properties** (suffix `Latest` without `Nuget`, e.g. `$(Net8Latest)`, `$(Net9Latest)`, `$(Net10Latest)`, `$(EF3Latest)`): contain the **newest** stable version of the corresponding .NET-X / EF-X line. **Always update to the latest stable X.0.y (or for EF: highest supported X.M.y) without asking per package.** The bump is delivered by editing the property value in `Directory.Packages.props`; downstream `<PackageVersion ... Version="$(NetXLatest)" />` entries pick it up automatically.
- **Multi-row packages (TFM-conditional + `VersionOverride` sites):** ask **once per package** rather than once per row. The single decision applies to all of that package's conditional `<PackageVersion>` rows + every `VersionOverride` site referencing the same id.

## Cross-package procedures

### Fuget API-diff procedure

Some packages are flagged with this procedure (see per-package entries) — typically database clients and other shipping deps where surface change matters.

The bulk fetcher lives at [`fuget-api-diff.ps1`](../../scripts/fuget-api-diff.ps1). It accepts a manifest of `{id, old, new}` tuples + a fuget base URL, fetches every TFM diff in parallel, parses additions / removals from the `<span class="diff-Add|diff-Remove">` markers, and merges per package. Wall-clock ≈ 30-60s for the first uncached run on a package; second runs are faster.

On every bump:
1. Run `fuget-api-diff.ps1 -Action diff -ManifestFile <json> -FugetBase <url>` for all flagged packages of the release in one batch. Override `-FugetBase` with the user's self-hosted fuget URL (recorded in [`external-repos.md`](./external-repos.md) → user-specific paths); default is the public https://www.fuget.org.
2. **Apply universal filters** (always-on, applied during rendering, not in the script — see *Universal API-diff filters* below):
   - **Drop `protected` members.** They aren't public surface and only matter if linq2db subclasses, which is rare for transitive deps.
   - **Reconcile `diff-Update` double-counting.** When a member's signature changes, fuget renders it as both a removal (old sig) and an addition (new sig) inside a single `diff-Update` parent. After parsing, compute "real removals" = items in `mergedRemovals` that don't have an equivalent entry in `mergedAdditions`. The script does **not** do this normalisation today — the agent does it post-render.
3. **Apply per-package diff exclusions** (see *API-diff exclusion list* below) — namespaces / types the user has marked as not relevant for future reviews.
4. Show the filtered diff to the user before committing the bump — they decide whether to absorb the API change, revert the bump, or add new exclusions.
5. After the user has reviewed, ask whether any new namespaces/types should be added to the package's exclusion list (record under the package's entry as `**API-diff exclusions:**`).
6. The procedure is opt-in per package; the package's own entry above lists it as an update rule.

### Universal API-diff filters

These filters apply to **every** package's fuget API-diff render (they're not per-package). Recorded here so the agent knows to apply them automatically.

- **Protected members** (`protected\s+\w+`, `protected\s+(virtual|abstract|override|sealed)\s+\w+`, `protected\s+(static|async)\s+\w+`) — drop. Reason: not consumable except by subclassing, which the linq2db codebase rarely does for these dep packages.
- **`diff-Update` re-emissions** — drop entries from `mergedRemovals` whose signature also appears in `mergedAdditions`. They represent a member whose signature changed (typically nullable annotation, parameter rename, default-value change), not a removed-and-replaced API.

### EF Core provider packages — pin within EF major

EF Core provider packages (Pomelo.EntityFrameworkCore.MySql, Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime, Microsoft.EntityFrameworkCore.Sqlite/SqlServer/InMemory, etc.) **must update only within the same EF Core major** that pairs with each TFM:

| linq2db TFM | EF Core major | Provider major to use |
|---|---|---|
| `net462` | EF Core 3.1 | provider 3.x line _(some providers use a different but stable mapping — Pomelo: `3.2.x` for EF 3.1)_ |
| `net8.0` | EF Core 8.x | provider 8.0.x |
| `net9.0` | EF Core 9.x | provider 9.0.x |
| `net10.0` | EF Core 10.x | provider 10.0.x _(if released; otherwise stay on 9.0.x)_ |

For each provider's TFM-conditional row, find the latest stable in the matching X.M.x line and bump only within that line. Cross-major bumps break EF Core API compatibility on the affected TFM.

### Lowest-supported-TFM detection

Some package bumps **raise the lowest .NET TFM** the package supports (e.g. `Net.IBM.Data.Db2` 9.x supported `net8.0` but 10.x dropped down to `net10.0` only). Bumping such a package without action **breaks the build** for projects targeting the dropped TFMs.

**Detection (per bump, today manual):** before applying, read the target version's `<dependencies>` group on nuget.org for the per-TFM target frameworks listed; compare against linq2db's supported TFMs (per `CLAUDE.md`: `net462, netstandard2.0, net8.0–net10.0`). If any supported TFM is no longer listed, the package raised its lowest TFM.

**When raised, ask the user the resolution:**

1. **Drop the lower TFMs from linq2db's matrix** (rare — only when the user is OK with it).
2. **Pin the package per-TFM** — add a TFM-conditional `<PackageVersion>` entry for the new version targeting the higher TFM only, and add an inverse `Condition` to the existing entry so it stays the active row for the lower TFMs. Apply the same edit to the central `Directory.Packages.props` and to any `Tests/Tests.T4.Nugets/Directory.Packages.props` site.

The script does not yet auto-detect this — TODO: extend `release-deps-discover.ps1` to query the registration leaf's `dependencyGroups[].targetFramework` and surface a `tfmRaised` flag per row.

### API-diff exclusion list

Per-package list of namespaces / types that should be **stripped from fuget API-diff output** before the user reviews. Captured in each package's own entry as a bullet:

```
- **API-diff exclusions:** <namespace-or-type-pattern> ; <another-pattern>
```

Patterns are wildcard-friendly (`Internal.*`, `*.Internal.*`, `Foo.Bar.IBaz`). The fuget-diff renderer applies them before showing the user, so noise from auto-generated / internal / explicitly-deprecated surface stays out of the review window.

## Entries

<!-- entries below this line are appended by `release-deps` on first encounter -->

## Ydb.Sdk

- **Release notes URL:** https://github.com/ydb-platform/ydb-dotnet-sdk/releases
- **Update rule:** **Public-API surface diff via fuget** (see *Fuget API-diff procedure*) — show diff to user before committing.
- **API-diff exclusions:** `Ydb.Sdk.Services.*` (the high-level Query / Table services namespace; linq2db consumes only the lower-level `Ydb.Sdk.Ado` ADO.NET surface).
- **Last verified:** 2026-05-15 on release 6.3.0

## ClickHouse.Driver

- **Release notes URL:** https://github.com/ClickHouse/clickhouse-cs/releases
- **Update rule:** On every bump:
  1. **TFM support cap:** version `0.9.0` is the last release that supports `net462`. The net462 conditional row (the `!net8+` split) **must stay at `0.9.0`** unless / until linq2db drops net462 or ClickHouse re-adds netfx support.
  2. **Public-API surface diff via fuget** (see *Fuget API-diff procedure* category note). Show the diff to the user before committing the bump.
- **Last verified:** 2026-05-15 on release 6.3.0

## System.Data.SQLite

- **Release notes URL:** https://system.data.sqlite.org/home/doc/trunk/www/news.md
- **Update rule:** **Public-API surface diff via fuget** (see *Fuget API-diff procedure*) — show diff to user before committing.
- **Last verified:** 2026-05-15 on release 6.3.0

## dotMorten.Microsoft.SqlServer.Types

- **Update rule:** This package is a side-package coupled to the SqlClient line:
  - `1.x` line pairs with **System.Data.SqlClient** (legacy)
  - `2.x` line pairs with **Microsoft.Data.SqlClient**
  Do **not** cross-bump majors unless the corresponding SqlClient package is also moved across the `System.Data.SqlClient → Microsoft.Data.SqlClient` boundary in the same place. Today the references in `Directory.Packages.props` and `Tests/Tests.T4.Nugets/Directory.Packages.props` should stay on `1.x`.
- **Last verified:** 2026-05-15 on release 6.3.0

## Devart.Data.Oracle

- **Release notes URL:** https://www.devart.com/dotconnect/oracle/revision_history.html
- **Update rule:** **Public-API surface diff via fuget** (see *Fuget API-diff procedure*) — show diff to user before committing.
- **Last verified:** 2026-05-15 on release 6.3.0

## Meziantou.Polyfill

- **Release notes URL:** _none — package does not publish release notes; the list of polyfilled APIs lives in the repo README at https://github.com/meziantou/Meziantou.Polyfill/blob/main/README.md_
- **Update rule:** On every bump:
  1. Pull the README diff between the current and target version (`git log --diff-filter=M -p -- README.md` against the Meziantou.Polyfill repo, or compare README at the two release tags) to extract the **list of newly polyfilled APIs**.
  2. For each new API, search the linq2db codebase for our own polyfill or conditional-build (`#if`) implementing the same API. If found, propose to **delete** our copy and rely on Meziantou.Polyfill instead.
  3. **Always show the full list of new APIs to the user for review**, even if no internal duplicates are found — the user may want to start using one of the new polyfills somewhere.
- **Last verified:** 2026-05-15 on release 6.3.0

## Oracle.ManagedDataAccess.Core

- **Release notes URL:** _none — README in the nupkg on nuget.org (https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core#readme-body-tab)_
- **Update rule:** **Public-API surface diff via fuget** (see *Fuget API-diff procedure*) — show diff to user before committing.
- **Last verified:** 2026-05-15 on release 6.3.0

## Microsoft.Extensions.Logging.Console

- **Update rule:** The `!net9+` conditional row stays on a literal version (today `8.0.1`). **Do not switch this row to `$(Net8Latest)`** — the package's 8.0.x line shipped only `8.0.0` + `8.0.1` and then jumped straight to 9.0.x; `$(Net8Latest)` (which tracks the latest .NET 8 patch like `8.0.27` for EF.Core / runtime libs) has no matching `Microsoft.Extensions.Logging.Console` release. Future bumps for this row are **capped at the 8.0.x line** of this specific package — i.e. only viable if Microsoft ever re-opens 8.0.x patches for it.
- **Last verified:** 2026-05-15 on release 6.3.0

## Microsoft.NET.Test.Sdk

- **Release notes URL:** https://github.com/microsoft/vstest/releases
- **Last verified:** 2026-05-15 on release 6.3.0

## NUnit

- **Release notes URL:** https://docs.nunit.org/articles/nunit/release-notes/framework.html
- **Last verified:** 2026-05-15 on release 6.3.0

## NUnit.Analyzers

- **Release notes URL:** https://github.com/nunit/nunit.analyzers/blob/master/CHANGES.md
- **Last verified:** 2026-05-15 on release 6.3.0

## Pomelo.EntityFrameworkCore.MySql

- **Release notes URL:** https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/releases
- **Update rule:** Per *EF Core provider packages — pin within EF major* (cross-package procedure). Pomelo's mapping is **EF 3.1 → 3.2.x**, EF 8 → 8.0.x, EF 9 → 9.0.x. Update each TFM-conditional row only within its own major.
- **Last verified:** 2026-05-15 on release 6.3.0

## Npgsql

- **Release notes URL:** https://github.com/npgsql/npgsql/releases
- **Update rule:** Multiple rows pin different majors per TFM:
  - `!net8+` row → 8.0.x line (npgsql majors aligned with .NET LTS pairings).
  - `net8+` row → 10.0.x line.
  - `Tests/Tests.T4.Nugets/Directory.Packages.props` → matches the net8+ row's major.
  - `VersionOverride` site referencing `$(EF3NpgsqlVersion)` → pinned at `4.1.14` (last npgsql 4.1.x — paired with EF Core 3.1).
  Update each row only within its own major.
- **API surface diff via fuget** (see *Fuget API-diff procedure*) — apply per row that bumps.
- **Last verified:** 2026-05-15 on release 6.3.0

## Newtonsoft.Json

- **Update rule:** **Pinned at the current shipping version** (today `13.0.1`). Do not bump even when newer 13.0.x is available, unless flagged vulnerable. Reasoning: shipping with the lowest stable 13.0.x version keeps downstream consumers free of transitive constraints (same intent as runtime-pin policy, but for Newtonsoft specifically since it is referenced from shipping projects).
- **Last verified:** 2026-05-15 on release 6.3.0

## NUnit3TestAdapter

- **Release notes URL:** https://docs.nunit.org/articles/vs-test-adapter/AdapterV4-Release-Notes
- **Last verified:** 2026-05-15 on release 6.3.0

## Microsoft.AspNetCore.OData

- **Release notes URL:** https://github.com/OData/AspNetCoreOData/releases
- **Update rule:** Package is **only used by .NET-targeted projects** (not netfx/netstandard). The `!net8+` conditional `<PackageVersion>` entry is dead code from the pre-net8 era — keep only the unconditional entry.
- **Last verified:** 2026-05-15 on release 6.3.0

## FSharp.Core

- **Release notes URL:** https://github.com/dotnet/fsharp/releases
- **Update rule:** Same cleanup pattern as `Microsoft.AspNetCore.OData` — historical TFM split (net462 / net8+) is dead post-TFM-migration. Collapse to a single unconditional entry. Pinned at the latest 10.x line for net8+. If a downstream project actually requires the older line, the build will fail and the conditional entry can be re-added with documentation explaining why.
- **Last verified:** 2026-05-15 on release 6.3.0

## NodaTime

- **Release notes URL:** https://github.com/nodatime/nodatime/releases
- **Last verified:** 2026-05-15 on release 6.3.0

## System.Text.Json

- **Update rule:** Pinned to the **lowest non-vulnerable version of the lowest non-EOL .NET major** the package supports. Today that floor is `8.0.5` (versions before 8.0.5 carry CVEs we don't want to ship; the lowest non-EOL major is .NET 8, since older majors are EOL). Hold this row at `8.0.5` until either (a) a new CVE forces another bump, or (b) .NET 8 goes EOL and the floor moves to the next non-EOL major. Distinct from `$(Net8LatestForNuget)` (which is 8.0.0) — the literal here is intentionally higher.
- **Last verified:** 2026-05-15 on release 6.3.0

## System.Linq.Dynamic.Core

- **Release notes URL:** https://github.com/zzzprojects/System.Linq.Dynamic.Core/blob/master/CHANGELOG.md
- **Last verified:** 2026-05-15 on release 6.3.0

## MySql.Data

- **Release notes URL:** https://dev.mysql.com/doc/relnotes/connector-net/en/
- **Update rule:** **Public-API surface diff via fuget** (see *Fuget API-diff procedure*) — show diff to user before committing.
- **Last verified:** 2026-05-15 on release 6.3.0

## Net.IBM.Data.Db2 (and `-lnx`, `-osx` platform variants)

- **Release notes URL:** _none — README in the nupkg on nuget.org (https://www.nuget.org/packages/Net.IBM.Data.Db2#readme-body-tab)_
- **Update rule:**
  1. **TFM split required for 9.x → 10.x.** The 10.x line dropped `net8.0` support. We **do not** drop net8 from CI. Apply per-TFM-conditional split in `Directory.Packages.props`:
     ```xml
     <PackageVersion Include="Net.IBM.Data.Db2"     Version="9.0.0.400"   Condition="!$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net10.0'))" />
     <PackageVersion Include="Net.IBM.Data.Db2"     Version="10.0.0.100"  Condition="'$(TargetFramework)'=='net10.0'" />
     ```
     Same pattern for `-lnx` and `-osx`. Apply on every X.x → (X+1).x bump that drops a supported TFM.
  2. **CI install scripts.** Versions are also pinned in `Build/Azure/scripts/db2.provider.sh` (lnx) and `Build/Azure/scripts/mac.db2.provider.sh` (osx). **Do not** update these scripts when adding a new TFM-conditional entry — the CI runs against the lower-TFM matrix and needs the older client. Only update the scripts when the lower-TFM matrix is bumped or dropped.
  3. **API-surface diff via fuget** (see *Fuget API-diff procedure*) — diff between the previous active version and the new active version, reviewed by the user before commit.
- **Per-platform availability:** versions may differ across `-lnx` / `-osx` / windows variants. Update each to whatever's available; don't force them to the same version if one platform's release lags.
- **Last verified:** 2026-05-15 on release 6.3.0

## Oracle.ManagedDataAccess

- **Release notes URL:** _none — release notes ship in the package README on nuget.org (https://www.nuget.org/packages/Oracle.ManagedDataAccess#readme-body-tab)_
- **Update rule:** Two version sites with **different ceilings**:
  1. The plain `<PackageVersion Include="Oracle.ManagedDataAccess" />` entry in `Directory.Packages.props` is **capped at the latest 21.x** stable. Versions 23.x produce test failures in linq2db (currently). Do not bump past 21.x without an explicit retest pass.
  2. The `$(OracleManagedLinqPadVersion)` property (referenced by the LINQPad-side `VersionOverride` site) **may be bumped to the latest stable** of the 23.x line. Updating the property cascades to every site that references it.
- **API surface diff via fuget** (see *Fuget API-diff procedure*) — apply on every bump of either site.
- **Last verified:** 2026-05-15 on release 6.3.0

## linq2db4iSeries

- **Release notes URL:** https://github.com/LinqToDB4iSeries/Linq2DB4iSeries/releases
- **Update rule:** Third-party DB2-iSeries provider that **mirrors the linq2db version** it's built against. Important for LINQPad support: if linq2db has any provider-API breaking change (public **or** internal) between the version this package targets and the linq2db version we're releasing, the package may not work in LINQPad. **Alert the user** before merging when the linq2db4iSeries version lags behind the about-to-be-released linq2db version, with a one-line note about which provider-API surface area changed.
- **Last verified:** 2026-05-15 on release 6.3.0

## Microsoft.Data.SqlClient

- **Release notes URL:** https://github.com/dotnet/SqlClient/blob/main/release-notes/README.md
- **Update rule:** **Public-API surface diff via fuget** (see *Fuget API-diff procedure*) — show diff to user before committing.
- **Last verified:** 2026-05-15 on release 6.3.0

## Meziantou.Analyzer

- **Release notes URL:** https://github.com/meziantou/Meziantou.Analyzer/releases
- **Update rule:** On every bump:
  1. Read release notes between current and target version. Identify new rules (`MAxxxx`) and new rule options.
  2. Enable each new rule as **error** severity in the repo `.editorconfig`. For new rule **options** (not new rules), ask the user before enabling.
  3. After update + verification Release build, observe which new rules raised errors. For each rule that raised errors, ask the user: fix the errors, or disable the rule (set severity = none in `.editorconfig`).
  4. **First-time-this-rule update only:** also catch up on previously-missed rules — audit the analyzer's full rule catalog at the target version against the current `.editorconfig` and enable any missing rules using the same procedure. Do **not** touch rules that were already explicitly enabled or disabled in `.editorconfig`.
- **Last verified:** 2026-05-15 on release 6.3.0
