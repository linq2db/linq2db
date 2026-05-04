---
area: SHARED-INTERNAL
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 2/2
coverage_tier_2: 1/1
---

# SHARED-INTERNAL

Three `.cs` files under `Source/Shared/` that are **linked** (not compiled as a separate project) into every C# project in the solution via `Directory.Build.props:193` and into the test project via an explicit `<Compile Include>`. There is no `.csproj` for this folder.

## Subsystems

**Assembly-level initialization.** `SharedAssemblyInfo.cs:3` applies `[module: SkipLocalsInit]` solution-wide, suppressing zero-initialization of locals for performance across all TFMs.

**JetBrains static-analysis shims.** `JetBrains.Annotations.cs` contains internal copies of ReSharper/Rider annotation attributes so all projects get IDE analysis hints without a NuGet dependency on `JetBrains.Annotations`. All types are `internal sealed`. Attributes provided: `StringFormatMethodAttribute`, `UsedImplicitlyAttribute`, `MeansImplicitUseAttribute`, `PublicAPIAttribute`, `InstantHandleAttribute`, `PureAttribute`, `LinqTunnelAttribute`, `NoEnumerationAttribute`. Supporting enums: `ImplicitUseKindFlags`, `ImplicitUseTargetFlags`.

**TFM-compatibility polyfills.** `Extensions.cs` provides `internal static` extension classes active on `NETFRAMEWORK || NETSTANDARD2_0` and, in a smaller form, on modern TFMs:
- `StringExtensions` — C# 14 extension-member block: `AsSpan()` / `AsSpan(int)` / `AsSpan(int, int)` as `string`-returning fallbacks; `string.Create(IFormatProvider, FormattableString)` proxy; `JoinStrings(char, IEnumerable<string>)` wrapper that avoids the banned `string.Join` overload.
- `StringBuilderExtensions` — `AppendBuilder`, `InsertBuilder`, `AppendLine(IFormatProvider, FormattableString)`, `AppendJoinStrings`; on legacy TFMs these use `.ToString()` intermediates; on modern TFMs they delegate to native `Append(StringBuilder)` / `AppendJoin`.
- `CharExtensions` — `IsAsciiDigit`, `IsAsciiLetter`, `IsAsciiLetterOrDigit`; on legacy TFMs implemented inline; on modern TFMs delegating to `char.IsAsciiDigit` etc. (`Extensions.cs:157-160`).
- `AdoAsyncDispose` (legacy TFMs only) — `DisposeAsync` extension methods for `DbCommand`, `DbDataReader`, `DbConnection`, `DbTransaction` that check `IAsyncDisposable` at runtime and fall back to synchronous `Dispose` (`Extensions.cs:136-151`).

## Linkage

| Consumer | Mechanism |
|---|---|
| All C# projects in the solution | `Directory.Build.props:192-194` — `<Compile Include="$(MSBuildThisFileDirectory)/Source/Shared/*.cs" LinkBase="Compatibility" />` under `Condition="'$(Language)'=='C#'"` |
| `Tests/Linq/Tests.csproj` | Explicit `<Compile Include=".../Source/Shared/JetBrains.Annotations.cs" Link="..." />` at `Tests.csproj:75` (supplements the props include) |

Files appear in consuming projects under the virtual `Compatibility/` link folder.

## Key types

| Type | File | Notes |
|---|---|---|
| `StringFormatMethodAttribute` | `JetBrains.Annotations.cs:29` | Marks format-string parameters for ReSharper/Rider |
| `UsedImplicitlyAttribute` | `JetBrains.Annotations.cs:48` | Suppresses "unused symbol" warnings for reflection-used members |
| `PublicAPIAttribute` | `JetBrains.Annotations.cs:144` | Marks public API surfaces; implies `WithMembers` implicit-use |
| `PureAttribute` | `JetBrains.Annotations.cs:185` | Marks side-effect-free methods |
| `StringExtensions` | `Extensions.cs:14` / `Extensions.cs:162` | `AsSpan` polyfills + `JoinStrings` |
| `StringBuilderExtensions` | `Extensions.cs:83` / `Extensions.cs:175` | `AppendBuilder`, `InsertBuilder`, `AppendJoinStrings` |
| `CharExtensions` | `Extensions.cs:128` / `Extensions.cs:155` | `IsAsciiDigit/Letter/LetterOrDigit` |
| `AdoAsyncDispose` | `Extensions.cs:136` | Legacy-TFM async-dispose shim for ADO.NET types |

## Files (Tier 1 / Tier 2)

**Tier 1** (read in full):

| File | Purpose |
|---|---|
| `Source/Shared/JetBrains.Annotations.cs` | Internal JetBrains annotation attribute shims |
| `Source/Shared/SharedAssemblyInfo.cs` | `[module: SkipLocalsInit]` applied solution-wide |

**Tier 2** (read in full — area is small enough):

| File | Purpose |
|---|---|
| `Source/Shared/Extensions.cs` | TFM-compat polyfill extensions for `string`, `StringBuilder`, `char`, ADO.NET async-dispose |

## Inbound / outbound dependencies

- **Inbound:** every C# project in the solution (via `Directory.Build.props`). No project reference required — files are compiled inline.
- **Outbound:** `Extensions.cs` references `System.Data.Common` (for `DbCommand`, `DbDataReader`, etc.) on legacy TFMs only. No references to any linq2db assembly.

## Known issues / debt

- `Tests.csproj:75` explicitly re-includes `JetBrains.Annotations.cs` even though `Directory.Build.props` already covers `Source/Shared/*.cs` for all C# projects. This produces a duplicate-compile warning risk; may have been added before the blanket `Directory.Build.props` glob existed.
- `SharedAssemblyInfo.cs` contains only `[module: SkipLocalsInit]`. It previously held `[assembly: AssemblyTitle]` / `[assembly: AssemblyCopyright]` style attributes (common pattern) but those have been removed; the file is retained for the module attribute.
- `Extensions.cs` is not in the current Tier-1 pin list in `kb-areas.md` despite carrying the most logic in this area — see AUDIT-NOTE below.

## See also

- `Directory.Build.props:192-194` — the `<Compile Include>` that wires this folder into every project.
- [architecture/overview.md](../architecture/overview.md) — solution-level structure.

<details><summary>Coverage</summary>

Read (Tier 1): `Source/Shared/JetBrains.Annotations.cs`, `Source/Shared/SharedAssemblyInfo.cs`
Read (Tier 2 — full, area is 1 file): `Source/Shared/Extensions.cs`
Tier 3: none
Total files: 3 | Visited: 3/3

</details>
