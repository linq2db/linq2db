# linq2db.Analyzers

Roslyn analyzers and code fixes for [linq2db](https://github.com/linq2db/linq2db) users. This is an
opt-in, development-only package (it ships no runtime assembly) that flags legacy API usage and offers
automatic migrations to the current API.

## Diagnostics

| Id | Severity | Description |
|----|----------|-------------|
| [L2DB1001](https://github.com/linq2db/linq2db/wiki/L2DB1001) | Info | Legacy `Sql.Ext` analytic / window-function API is superseded by `Sql.Window`. A code fix migrates convertible chains. |

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

## Configuration

Adjust severity in `.editorconfig`:

```ini
dotnet_diagnostic.L2DB1001.severity = warning
```
