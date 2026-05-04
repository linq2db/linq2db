---
area: CODEGEN
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 3/3
coverage_tier_2: 0/0
---

# CODEGEN — Roslyn Source Generator

Standalone Roslyn incremental source-generator project (`Source/CodeGenerators/`). Produces `ExpressionBuilder.g.cs` at compile time, replacing a hand-maintained dispatch table inside `ExpressionBuilder.FindBuilderImpl` with a generated `switch` over `ExpressionType` and `MethodInfo.Name`.

## Subsystems

**Attribute scanning.** `BuildersGenerator.Initialize` (`BuildersGenerator.cs:17`) registers three `ForAttributeWithMetadataName` syntax providers — one per attribute:

- `BuildsAnyAttribute` → `TransformBuildsAny` → `BuilderNode` with `BuilderKind.Any`
- `BuildsExpressionAttribute` → `TransformBuildsExpression` → `EquatableReadOnlyList<BuilderNode>` with `BuilderKind.Expr`
- `BuildsMethodCallAttribute` → `TransformBuildsMethodCall` → `EquatableReadOnlyList<BuilderNode>` with `BuilderKind.Call`

All three streams are `Collect()`-ed and combined via `Combine` before being fed to `RegisterImplementationSourceOutput` (`BuildersGenerator.cs:45–56`).

**Parameter reflection.** `GetMethodParameters` (`BuildersGenerator.cs:109`) inspects the `CanBuild`/`CanBuildMethod` method on each decorated type and records which of `(MethodCallExpression/Expression, BuildInfo, ExpressionBuilder)` the method expects. This is encoded as a `CallParams` flags enum (`BuildersGenerator.Models.cs:12`) so the renderer can emit exactly the right call signature without runtime overhead.

**Code emission.** `GenerateCode` (`BuildersGenerator.cs:165`) emits a single source file `ExpressionBuilder.g.cs` containing a `partial class ExpressionBuilder` with a `private static partial ISequenceBuilder? FindBuilderImpl(BuildInfo, ExpressionBuilder)`. The body is a three-level dispatch: `switch (expr.NodeType)` → `case ExpressionType.Call: switch (call.Method.Name)` → per-builder `CanBuildMethod(call[, info][, builder])` guards. The `Any`-kind builders fall outside the switch and are tried unconditionally after all typed cases.

## Key types

| Type | File | Role |
|---|---|---|
| `BuildersGenerator` | `BuildersGenerator.cs` | `[Generator]` / `IIncrementalGenerator` entry point |
| `BuilderNode` | `BuildersGenerator.Models.cs:14` | Sealed record: fully-qualified builder class name, dispatch key (method name or `ExpressionType` string), `BuilderKind`, check-method name, `CallParams` bitmask |
| `BuilderKind` | `BuildersGenerator.Models.cs:9` | `Any`, `Expr`, `AnyCall`, `Call` — dispatch tier |
| `CallParams` | `BuildersGenerator.Models.cs:12` | `[Flags]` bitmask: `Call=1`, `Info=2`, `Builder=4`; drives which overload the renderer emits |
| `EquatableReadOnlyList<T>` | `EquatableReadOnlyList.cs:22` | Value-equality `IReadOnlyList<T>` wrapper; required for incremental SG cache stability (plain `List<T>` breaks caching) |

## Generator pipeline

```
SyntaxProvider.ForAttributeWithMetadataName(BuildsAny/Expression/MethodCall)
  → Transform* (symbol → BuilderNode / EquatableReadOnlyList<BuilderNode>)
  → .Collect()
  → .Combine(…)
  → RegisterImplementationSourceOutput → GenerateCode → ExpressionBuilder.g.cs
```

The `EquatableReadOnlyList<T>` wrapper (`EquatableReadOnlyList.cs:28–30`) is what allows the incremental engine to skip re-generation when the builder list hasn't changed — standard `ImmutableArray<T>` is used after `Collect()`, but the pre-collect transform step returns `EquatableReadOnlyList<T>` so the pipeline's equality check is meaningful.

## Files (Tier 1 / Tier 2)

**Tier 1 (all read):**

| File | Role |
|---|---|
| `BuildersGenerator.cs` | Generator entry point, all transform and render methods |
| `BuildersGenerator.Models.cs` | Internal enums and `BuilderNode` record |
| `EquatableReadOnlyList.cs` | Value-equality list wrapper |

**Tier 2:** none (all `.cs` files are Tier 1).

**Tier 3 (counted, not read):** `CodeGenerators.csproj`, `Directory.Build.props` — project metadata, read for hook-in verification.

## Project configuration

`CodeGenerators.csproj` (`Source/CodeGenerators/CodeGenerators.csproj:3–8`):
- `TargetFramework`: `netstandard2.0` — Roslyn SG requirement.
- `IsRoslynComponent`: `true` — enables Roslyn-component analyzer rules.
- `EnforceExtendedAnalyzerRules`: `true`.
- `IsPackable`: `false` — never shipped as a NuGet package directly.
- References: `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.Analyzers`, `Meziantou.Polyfill` (polyfills `System.HashCode` and `IsExternalInit` for netstandard2.0 targets).

Hook-in from `LinqToDB.csproj` (`Source/LinqToDB/LinqToDB.csproj:26`):

```xml
<ProjectReference Include="../CodeGenerators/CodeGenerators.csproj"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false" />
```

`OutputItemType="Analyzer"` registers the project as a source generator; `ReferenceOutputAssembly="false"` ensures `CodeGenerators.dll` is not part of the `LinqToDB` public API surface.

`Directory.Build.props` overrides: enables `RunAnalyzersDuringBuild`, `EnforceCodeStyleInBuild`, `AnalysisLevel=preview-All`, `TreatWarningsAsErrors`, `Nullable=enable`, `LangVersion=14` — stricter than the repo root defaults to keep the generator itself clean. Does **not** suppress anything.

## Inbound / outbound dependencies

**Consumed by:** `LinqToDB.csproj` (via `OutputItemType="Analyzer"`) — the generator runs during the `LinqToDB` build and injects `ExpressionBuilder.g.cs` into the `LinqToDB.Internal.Linq.Builder` namespace.

**Reads at compile time:** every class in `Source/LinqToDB/Internal/Linq/Builder/` that carries `[BuildsAnyAttribute]`, `[BuildsExpressionAttribute]`, or `[BuildsMethodCallAttribute]` — see [EXPR-TRANS](../EXPR-TRANS/INDEX.md).

**NuGet dependency:** `Microsoft.CodeAnalysis.CSharp` (Roslyn SDK, `PrivateAssets="all"`, never transitive).

## Known issues / debt

None identified. Generator is self-contained; adding a new builder requires only decorating the class with the appropriate attribute — no manual registration step.

## See also

- [EXPR-TRANS area](../EXPR-TRANS/INDEX.md) — `ExpressionBuilder` and the builder registry that this generator populates.
- `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.cs` — declares `partial ISequenceBuilder? FindBuilderImpl` that the generator implements.

<details><summary>Coverage</summary>

Tier 1 (3/3 read this run):
- `Source/CodeGenerators/BuildersGenerator.cs` — generator entry point, all render methods
- `Source/CodeGenerators/BuildersGenerator.Models.cs` — enums and BuilderNode record
- `Source/CodeGenerators/EquatableReadOnlyList.cs` — value-equality list wrapper

Tier 2: 0 files (all .cs files promoted to Tier 1).

Tier 3 (counted, not read):
- `Source/CodeGenerators/CodeGenerators.csproj` — project metadata (read for hook-in verification; excluded from coverage count per Tier-3 rule)
- `Source/CodeGenerators/Directory.Build.props` — build overrides (same)

Cross-area read (hook-in verification):
- `Source/LinqToDB/LinqToDB.csproj:26` — confirms `OutputItemType="Analyzer"` reference

</details>
