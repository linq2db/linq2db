# linq2db.Analyzers

Roslyn analyzers and code fixes for [linq2db](https://github.com/linq2db/linq2db) users: they flag legacy
API usage and offer automatic migrations to the current API, and they report mistakes the compiler cannot
see — a query that is valid C# but cannot mean what it says. The package ships no runtime assembly.

You normally don't reference this package: `linq2db` depends on it, so the rules arrive with the library —
including in a project that references only a satellite package (`linq2db.Tools`, `linq2db.Remote.*`,
`linq2db.EntityFrameworkCore`). They require Roslyn 4.8 or later (.NET SDK 8.0+, Visual Studio 2022 17.8+)
and are silently skipped on older toolchains.

Reference it directly only to run the rules against an **older** linq2db — e.g. to size and apply a
migration before upgrading. It carries no `linq2db` dependency, so any combination restores:

```xml
<PackageReference Include="linq2db"           Version="6.3.0" />
<PackageReference Include="linq2db.Analyzers" Version="6.4.0" PrivateAssets="all" />
```

A rule withholds itself when the API it depends on is absent from the compilation, so an analyzer newer
than the runtime it runs against degrades to silence rather than to a broken fix.

## Diagnostics

| Id | Severity | Description |
|----|----------|-------------|
| [L2DB1001](https://github.com/linq2db/linq2db/wiki/L2DB1001) | Info | Legacy `Sql.Ext` analytic / window-function API is superseded by `Sql.Window`. A code fix migrates convertible chains. |
| [L2DB1002](https://github.com/linq2db/linq2db/wiki/L2DB1002) | Info | An equality against a `[Duration]` column compares a duration the declared unit cannot represent, so it can never match. Reported only; no code fix. |

### L2DB1001 — migrate `Sql.Ext` window functions to `Sql.Window`

```csharp
// before
var r = Sql.Ext.RowNumber().Over().PartitionBy(x.Category).OrderBy(x.Date).ToValue();

// after (code fix)
var r = Sql.Window.RowNumber(f => f.PartitionBy(x.Category).OrderBy(x.Date));
```

The `Sql.Ext` window API still works but will be removed in a future major release. The code fix preserves
your comments and formatting. Chains that have no direct `Sql.Window` equivalent (e.g. an aggregate without
`.Over()`) are reported but left for you to migrate manually.

The code fix withholds itself when the `Sql.Window` return type differs from the legacy `ToValue<TR>()`
slot (e.g. a `double` slot vs `Sql.Window`'s `double?`), so it never turns compiling code into a type
error. To apply it anyway and resolve the type change yourself, opt in (see below).

### L2DB1002 — a duration comparison the declared unit can never match

```csharp
[Column, Duration(DurationUnit.Second)] public TimeSpan InSeconds { get; set; }

// L2DB1002: InSeconds stores whole seconds, so this can never match
q.Where(r => r.InSeconds == TimeSpan.FromSeconds(1.5));
```

A column declared with `[Duration(DurationUnit.Second)]` holds a whole number of seconds, so asking whether
it equals one and a half of them asks for a value it cannot hold. The query is translated correctly — the
comparison becomes a range whose ends cross over, which is what equality means for an unrepresentable value —
and it returns nothing, with nothing in the log to say why. The rule reports the cases that are decidable
without running the query: where the compared duration is a constant, or reaches the comparison through a
local or a loop over constants.

Ordering operators are not reported. `> 1.5s` is exactly `> 1s` for a column of whole seconds, which is
already the right question.

There is no code fix: the intended value cannot be inferred — 1 second, 2 seconds, or a different column
unit are all plausible — so the rule reports and leaves the choice to you.

Only a unit declared through the **attribute** is visible to an analyzer. One configured through
`HasDuration` or a mapping schema is not, and is not diagnosed.

## Configuration

Adjust severity in `.editorconfig`:

```ini
dotnet_diagnostic.L2DB1001.severity = warning
```

Every rule in this package is in the `LinqToDB` analyzer category, so one line sets them all:

```ini
dotnet_analyzer_diagnostic.category-LinqToDB.severity = warning
```

Apply the L2DB1001 fix even when the `Sql.Window` return type diverges from the legacy slot (default
`false`; when enabled you resolve any resulting type change, e.g. widening `int` to `long`, by hand):

```ini
linq2db.L2DB1001.apply_fix_on_return_type_mismatch = true
```

Disable every rule of this package for a project — including when it arrives as a dependency of
`linq2db` rather than as a direct reference:

```xml
<PropertyGroup>
	<EnableLinqToDBAnalyzers>false</EnableLinqToDBAnalyzers>
</PropertyGroup>
```
