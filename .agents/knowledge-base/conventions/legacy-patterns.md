---
area: GLOBAL
kind: convention
sources: [code]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Legacy Patterns

Patterns the codebase has migrated away from, with their modern replacements.

## Pairs

### 1. Old Merge API -> Fluent Merge API

**Legacy:** Free-function `Merge`, `MergeWithOutput`, etc. on `DataConnection`, declared in `Source/LinqToDB/Data/LegacyMergeExtensions.cs`.

**Modern:** `ITable<T>.Merge()` fluent builder with typed operations.

**Signal:** `Source/LinqToDB/Data/LegacyMergeExtensions.cs:17` -- entire class marked `[Obsolete]`. Tests for old API are in `Tests/Linq/Update/OldMergeTests.cs:19`, itself marked `[Obsolete]`.

```csharp
// Legacy -- Source/LinqToDB/Data/LegacyMergeExtensions.cs:17
[Obsolete("Legacy Merge API obsoleted...")]
public static class LegacyMergeExtensions { ... }

// Modern -- Tests/Linq/Update/MergeTests.cs (fluent API)
// table.Merge().Using(source).On(...).WhenMatched(...).Merge()
```

### 2. MappingSchema.SetConvertExpression -> [ValueConverter] attribute

**Legacy:** Registering converters via `SetConvertExpression<TFrom, TTo>(expr)` on a `MappingSchema` instance.

**Modern:** `[ValueConverter(ConverterType = typeof(MyConverter))]` on a property. Defined in `Source/LinqToDB/Mapping/ValueConverterAttribute.cs:9`.

```csharp
// Legacy (infrastructure) -- Source/LinqToDB/Internal/Remote/SerializationMappingSchema.cs:20-24
SetConvertExpression<bool          , string>(value => value ? "1" : "0");
SetConvertExpression<int           , string>(value => value.ToString(CultureInfo.InvariantCulture));

// Modern -- Source/LinqToDB/Mapping/ValueConverterAttribute.cs:9
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, ...)]
public class ValueConverterAttribute : MappingAttribute { ... }
```

### 3. LegacyVisitorBase -> ExpressionVisitorBase

**Legacy:** Visitors inheriting `LegacyVisitorBase` -- a shim routing old-style `Visit` calls through `ExpressionVisitorBase` by delegating to `node.Reduce()`.

**Modern:** Direct subclasses of `ExpressionVisitorBase` overriding `VisitXxx` methods without the `Reduce()` indirection.

```csharp
// Legacy -- Source/LinqToDB/Internal/Expressions/ExpressionVisitors/LegacyVisitorBase.cs:7
abstract class LegacyVisitorBase : ExpressionVisitorBase {
    public override Expression VisitSqlValidateExpression(SqlValidateExpression node)
        => node.Update(Visit(node.InnerExpression));  // Reduce()-based delegation
}
```

### 4. DataContext constructor overloads -> DataOptions

**Legacy:** `new DataContext(providerName, connectionString)` and similar overloads.

**Modern:** `new DataContext(new DataOptions().UseConnectionString(providerName, connectionString))`.

```csharp
// Source/LinqToDB/DataContext.cs:67
[Obsolete("This API scheduled for removal in v7. Instead use: new DataContext(new DataOptions()...)")]
public DataContext(IDataProvider dataProvider, string connectionString) { ... }

// Source/LinqToDB/DataContext.cs:82
[Obsolete("This API scheduled for removal in v7. Instead use: new DataContext(new DataOptions()...)")]
public DataContext(string providerName, string connectionString) { ... }
```

### 5. Configuration.Linq.* static properties -> DataOptions

**Legacy:** Global static mutation via `LinqToDB.Common.Configuration.Linq.CompareNulls`, etc. Still present but marked `[Obsolete]`.

**Modern:** `DataOptions` instance passed to `DataContext` -- per-context, composable.

```csharp
// Source/LinqToDB/Common/Configuration.cs:338
[Obsolete("Use CompareNulls instead. This option will be removed in version 7")]
public static bool CompareNulls { ... }

// Modern: DataOptions.UseCompareNulls(CompareNulls.LikeSql)
```

## Identifying legacy code

Key signals: `[Obsolete]` on the class/method, a `Legacy*` type name, an `Old*` test file (e.g. `OldMergeTests.cs`), or a comment pointing to a migration guide.

## See also

- [conventions/naming.md](naming.md) -- `Legacy*` / `Old*` naming convention
- [conventions/public-api-discipline.md](public-api-discipline.md) -- `[Obsolete]` deprecation flow
