---
area: GLOBAL
kind: convention
sources: [code]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Public-API Discipline

## Rule

**Anything outside `LinqToDB.Internal.*` is stable public API.** Types, method signatures, and observable generated SQL in non-`Internal.*` namespaces under `Source/LinqToDB/` are a stability contract for downstream consumers. See [code-design.md](../../docs/code-design.md) section Public API is a contract.

**`LinqToDB.Internal.*` is internal by convention, not by C# access modifier.** Types in the `Internal.*` hierarchy are `public` C# members (so companion libraries -- EFCore integration, Scaffold, Remote -- can reference them) but reside in a namespace documenting their non-stable intent. There is no `[InternalsVisibleTo]` declaration in `Source/LinqToDB`; the namespace is the only gate.

**`ApiCompat` and `PublicAPI.Shipped.txt` are the enforcement mechanisms.** `Source/LinqToDB/CompatibilitySuppressions.xml` is the ApiCompat baseline; `Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` are the Roslyn Microsoft.CodeAnalysis.PublicApiAnalyzers snapshots. Any surface change -- add, modify, remove -- requires regenerating these via `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` or the `api-baselines` skill. **Never hand-edit `CompatibilitySuppressions.xml`** -- see [agent-rules.md](../../docs/agent-rules.md) section Agent Guardrails.

**`Source/BannedSymbols.txt`** lists types and members that must not be called inside `Source/LinqToDB/`. Enforced by the BannedApiAnalyzers Roslyn extension in Release builds. Categories include: flawed BCL types (`ConcurrentBag<T>`), ADO.NET interfaces replaced by concrete base classes, reflection methods replaced by `AttributesExtensions` cached helpers, culture-sensitive formatting, and raw `Expression.Compile` replaced by `CompileExpression` extension.

**`[Obsolete]` is the deprecation flow.** When a public API is removed, it is first marked `[Obsolete(...), EditorBrowsable(EditorBrowsableState.Never)]` with a message pointing to the replacement and a version target. The member remains functional until the breaking-change milestone.

## Examples

```csharp
// Source/LinqToDB/IDataContext.cs:22 -- anchor of the public API surface
[PublicAPI]
public interface IDataContext : IConfigurationID, IDisposable, IAsyncDisposable
```

```xml
// Source/LinqToDB/CompatibilitySuppressions.xml:5-9 -- ApiCompat baseline (generated, never hand-edited)
<Suppression>
  <DiagnosticId>CP0001</DiagnosticId>
  <Target>T:LinqToDB.Internal.SqlQuery.SqlSimpleCaseExpression</Target>
  <IsBaselineSuppression>true</IsBaselineSuppression>
</Suppression>
```

```
// Source/BannedSymbols.txt:1-2 -- banned ADO.NET interfaces
T:System.Data.IDataReader;Use DbDataReader class instead of ADO.NET interfaces
T:System.Data.IDbCommand;Use DbCommand class instead of ADO.NET interfaces
```

```csharp
// Source/LinqToDB/DataContext.cs:67 -- Obsolete deprecation pattern
[Obsolete("This API scheduled for removal in v7. Instead use: new DataContext(new DataOptions()...)"),
 EditorBrowsable(EditorBrowsableState.Never)]
```

```
// Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt:1-5 -- Roslyn public-API snapshot
#nullable enable
abstract LinqToDB.Data.RetryPolicy.RetryPolicyBase.ShouldRetryOn(...) -> bool
abstract LinqToDB.Internal.Common.ValueComparer.Equals(...) -> bool
// Note: Internal.* types appear here because they are public C# members;
//       their presence does NOT make them stable API -- the namespace convention governs.
```

## Boundary enforcement

The `Internal.*` namespace boundary is not a build-time C# barrier -- it is a reviewer and agent barrier. Adding a public type to `LinqToDB.Internal.SqlQuery` is fine; adding one to `LinqToDB.SqlQuery` is an API surface addition that requires baseline regeneration and milestone review.

ApiCompat runs during `dotnet pack` and on CI. If it reports a `CP0001` (removed type/member) or `CP0002` (changed member) for a symbol that is actually internal AST evolution, the correct fix is to move the type to `LinqToDB.Internal.*` -- not to suppress the violation. See [code-design.md](../../docs/code-design.md) section SQL AST types live in LinqToDB.Internal.SqlQuery.

## See also

- [code-design.md](../../docs/code-design.md) -- canonical public-API contract statement
- [conventions/naming.md](naming.md) -- `LinqToDB.Internal.*` namespace naming convention
- [architecture/public-api.md](../architecture/public-api.md) -- public-API architecture overview
