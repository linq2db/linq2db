# linq2db.Analyzers

Roslyn analyzers and code fixes for [linq2db](https://github.com/linq2db/linq2db) users: they flag legacy
API usage and offer automatic migrations to the current API. The package ships no runtime assembly.

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

A rule withholds itself when the API it migrates to is absent from the compilation, so an analyzer newer
than the runtime it runs against degrades to silence rather than to a broken fix.

## Diagnostics

| Id | Severity | Description |
|----|----------|-------------|
| [L2DB1001](https://github.com/linq2db/linq2db/wiki/L2DB1001) | Info | Legacy `Sql.Ext` analytic / window-function API is superseded by `Sql.Window`. A code fix migrates convertible chains. |
| [L2DB1003](https://github.com/linq2db/linq2db/wiki/L2DB1003) | Info | A throw-only stub that nothing declares server-side-only. A code fix adds the marker. |
| [L2DB1004](https://github.com/linq2db/linq2db/wiki/L2DB1004) | Info | A server-side-only stub throwing something other than `ServerSideOnlyException`. A code fix replaces it. |

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

### L2DB1003 / L2DB1004 — keep a server-side-only member's declaration and implementation in step

A member that only makes sense on the server is declared so by `[ServerSideOnly]`, by `ServerSideOnly = true`
on an `Sql.*` attribute (every `Sql.Extension` constructor sets it, so a bare `[Sql.Extension("…")]` already
counts), by an `Sql.TableFunction`-derived attribute, or by `[ExpressionMethod]`. Its body is then a stub
that throws. These two rules check the halves agree.

```csharp
// L2DB1003 - a stub with nothing declaring it server-side only
[Sql.Function("MY_FUNC")]
public static int MyFunc(int x) => throw new ServerSideOnlyException(nameof(MyFunc));
// code fix: [Sql.Function("MY_FUNC", ServerSideOnly = true)]

// L2DB1004 - declared, but the stub throws the wrong thing
[ServerSideOnly]
public static int Other(int x) => throw new NotImplementedException();
// code fix: throw new ServerSideOnlyException(nameof(Other));
```

`ServerSideOnlyException` names the API that was called on the client; `NotImplementedException` tells the
caller nothing about why the call could not run.

L2DB1003's other remedy — give the member a real implementation — is never applied automatically, since
synthesising a body is not a mechanical rewrite. Where the member carries no marker-capable attribute at all,
the rule only treats it as a stub when it throws `ServerSideOnlyException`, so ordinary
`throw new NotImplementedException()` placeholders are left alone.

On a member carrying several configuration-scoped `Sql.*` attributes, the code fix sets `ServerSideOnly = true`
on one of them, which is enough to satisfy the rule — but the runtime resolves the attribute per
configuration, so add it to the rest by hand if the member should be server-side-only on every provider.

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

Both exception-type lists below are **additive** to their defaults and match type names **exactly**, not by
subclass. Add exception types your own stubs throw, so L2DB1004 accepts them:

```ini
linq2db.L2DB1004.allowed_exception_types = MyCompany.ServerSideException, MyCompany.SqlOnlyException
```

Add exception types that mark an *unattributed* stub as server-side-only, widening what L2DB1003 reports
(the default is `LinqToDB.ServerSideOnlyException` alone, which keeps ordinary `NotImplementedException`
placeholders out of the results):

```ini
linq2db.L2DB1003.unmarked_stub_exception_types = MyCompany.ServerSideException
```

Disable every rule of this package for a project — including when it arrives as a dependency of
`linq2db` rather than as a direct reference:

```xml
<PropertyGroup>
	<EnableLinqToDBAnalyzers>false</EnableLinqToDBAnalyzers>
</PropertyGroup>
```
