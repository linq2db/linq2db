---
uid: Merge-API-Description
---
# Merge API Description

Merge API contains four groups of methods:

- `Merge`, `MergeInto`, `Using`, `UsingTarget` methods to configure merge command's source and target
- `On`, `OnTargetKey` methods to configure merge command's match condition
- `InsertWhenNotMatched*`, `UpdateWhenMatched*`, `DeleteWhenMatched*`, `UpdateWhenNotMatchedBySource*`, `DeleteWhenNotMatchedBySource*`, `UpdateWhenMatched*ThenDelete` methods to add operations to merge command
- `Merge` and `MergeAsync` methods to execute command against database

To create and execute merge command you should first configure target, source and match conditions. Then you must add at least one operation to merge builder. After that you should call `Merge` method to execute command.
Note that all operation methods returns new merge builder, so code like that:

```cs
// WRONG
var db.Table.Merge().UsingTarget().OnTargetKey().DeleteWhenMatched();
// wrong, it will not modify merge object, but will create new one
merge.InsertWhenNotMatched();
// execute merge with only one command - Delete
merge.Merge();

// CORRECT
db.Table.Merge().UsingTarget().OnTargetKey().DeleteWhenMatched().InsertWhenNotMatched().Merge();
```

## General notes on API

All API parameters are required and cannot be null. If you what to skip some parameter, check for a method without it. If there is no such method - this parameter cannot be ommited.

## Validation

Before command execution, linq2db will try to validate your command and throw `LinqToDBException` if it detects use of feature, unsupported by provider or general misconfiguration. It will not detect all issues, but will greatly reduce number of errors from user side. Also validation error contains message that points to error in your command. Database engine errors sometimes require research to understand what they mean in current specific context.

## Operations API

Merge operations will be added to generated query in the same order as they were called on command builder, because it is possible to specify several operations that could match the same record using operation conditions. In such cases database engine choose first matching operation as a winner.
Also dont forget to check what your database engine could [[support|Merge-API-:-Background-Information-and-Providers-Support]] to understand what API you can use.

## Methods

[Target and Source Configuration Methods](#target-and-source-configuration-methods)

[Match Configuration Methods](#match-configuration-methods)

[InsertWhenNotMatched*](#insertwhennotmatched)

[UpdateWhenMatched*](#updatewhenmatched)

[DeleteWhenMatched*](#deletewhenmatched)

[UpdateWhenNotMatchedBySource*](#updatewhennotmatchedbysource)

[DeleteWhenNotMatchedBySource*](#deletewhennotmatchedbysource)

[UpdateWhenMatched*ThenDelete](#updatewhenmatchedthendelete)

[Merge and MergeAsync](#merge-6)

## Target and Source Configuration Methods

```cs
// starts merge command and use table parameter as target
IMergeableUsing<TTarget> Merge<TTarget>(this ITable<TTarget> target);

// adds source query to merge, started by Merge() method
IMergeableOn<TTarget, TSource> Using<TTarget, TSource>(this IMergeableUsing<TTarget> merge, IQueryable<TSource> source);

// adds source collection to merge, started by Merge() method
IMergeableOn<TTarget, TSource> Using<TTarget, TSource>(this IMergeableUsing<TTarget> merge, IEnumerable<TSource> source);

// adds target as source to merge, started by Merge() method
IMergeableOn<TTarget, TTarget> UsingTarget<TTarget>(this IMergeableUsing<TTarget> merge);

// starts merge command using source query and target table
IMergeableOn<TTarget, TSource> MergeInto<TTarget, TSource>(this IQueryable<TSource> source, ITable<TTarget> target);
```

Those methods allow you to create merge builder and specify source and target.
To do it you can use:

- `MergeInto` method, which setups both source and target
- `Merge` + `Using`\`UsingTarget` method sequence, where target and source specified by separate method.

Methods could accept following parameters:

### `target`

Target table, that should be modified by merge command.

### `source`

Source data set, that should be merged into target table. Could be a client-side collection, table or query.

## Match Configuration Methods

```cs
// adds match condition using specified key from target and source record
// Examples:
// merge.On(target => new { target.Field1, target.Field2 }, source => new { source.Field1, source.Field2 })
// merge.On(target => target.Id, source => source.Id)
IMergeable<TTarget, TSource> On<TTarget, TSource, TKey>(this IMergeableOn<TTarget, TSource> merge, Expression<Func<TTarget, TKey>> targetKey, Expression<Func<TSource, TKey>> sourceKey);

// add match condition using boolean expression over target and source record
IMergeable<TTarget, TSource> On<TTarget, TSource>(this IMergeableOn<TTarget, TSource> merge, Expression<Func<TTarget, TSource, bool>> matchCondition);

// adds match condition using primary key columns
IMergeable<TTarget, TTarget> OnTargetKey<TTarget>(this IMergeableOn<TTarget, TTarget> merge);
```

`On`\`OnTargetKey` adds match condition to merge command builder.

### Notes

- `matchCondition` should be used only for rows matching. Any source filters must be applied to source directly to avoid database engine-specific side-effects (e.g. see Oracle limitations).
- `matchCondition` or match using keys shouldn't match more than one source record to one target record.

## InsertWhenNotMatched

```cs
IMergeable<TTarget, TTarget> InsertWhenNotMatched<TTarget>(this IMergeableSource<TTarget, TTarget> merge);

IMergeable<TTarget, TTarget> InsertWhenNotMatchedAnd<TTarget>(this IMergeableSource<TTarget, TTarget> merge, Expression<Func<TTarget, bool>> searchCondition);
```
```cs
IMergeable<TTarget, TSource> InsertWhenNotMatched<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TSource, TTarget>> setter);

IMergeable<TTarget, TSource> InsertWhenNotMatchedAnd<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TSource, bool>> searchCondition, Expression<Func<TSource, TTarget>> setter)
```

`InsertWhenNotMatched` takes insert operation options and returns new merge command builder with new operation. `InsertWhenNotMatchedAnd` method additionally takes operation condition expression.

### `merge`

Merge command builder. Method will return new builder with new insert operation. It will not modify original object.

### `searchCondition`

Operation execution condition. Operation without condition will be applied to all matching records.  
If there are multiple operations within same group - only last one allowed to have no condition. `WhenNotMatched` match group could contain only `Insert` operations.

### `setter`

Record creation expression. Defines set ex`InsertWhenNotMatched` takes insert operation options and returns new merge command builder with new operation. `InsertWhenNotMatchedAnd` method additionally takes operation condition expression.
pressions for values in new record. For methods without this parameters source record inserted into target (except fields marked with `SkipOnInsert` attribute or `IsIdentity` for provider without identity insert support).

```cs
db.Table
    .Merge()
    .Using(source)
    .OnTargetKey()
    .InsertWhenNotMatched(source => new TargetRecord()
    {
        Field1 = 10,
        Field2 = source.Field2,
        Field3 = source.Field1 + source.Field2
    })
    .Merge();
```

## UpdateWhenMatched

```cs
IMergeable<TTarget, TTarget> UpdateWhenMatched<TTarget>(this IMergeableSource<TTarget, TTarget> merge);

IMergeable<TTarget, TTarget> UpdateWhenMatchedAnd<TTarget>(this IMergeableSource<TTarget, TTarget> merge, Expression<Func<TTarget, TTarget, bool>> searchCondition)
```
```cs
IMergeable<TTarget, TSource> UpdateWhenMatched<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, TSource, TTarget>> setter);

IMergeable<TTarget, TSource> UpdateWhenMatchedAnd<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, TSource, bool>> searchCondition, Expression<Func<TTarget, TSource, TTarget>> setter);
```

`UpdateWhenMatched` takes update operation options and returns new merge command builder with new operation. `UpdateWhenMatchedAnd` method additionally takes operation condition expression.

### `merge`

Merge command builder. `UpdateWhenMatched` method will return new builder with new update operation. It will not modify original object.

### `searchCondition`

Operation execution condition. Operation without condition will be applied to all matching records.  
If there are multiple operations within same group - only last one could omit condition. `WhenMatched` match group could contain only `Update` and `Delete` operations.

### `setter`

Record update expression. Defines update expressions for values in target record. When not specified, source record values used to update target record (except fields marked with `SkipOnUpdate` or `IsIdentity` attributes).

```cs
db.Table
    .Merge()
    .Using(source)
    .OnTargetKey()
    .UpdateWhenMatched((target, source) => new TargetRecord()
    {
        Field1 = target.Field10,
        Field2 = source.Field2,
        Field3 = source.Field1 + target.Field2
    })
    .Merge();
```

## DeleteWhenMatched

```cs
IMergeable<TTarget, TSource> DeleteWhenMatched<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge);

IMergeable<TTarget, TSource> DeleteWhenMatchedAnd<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, TSource, bool>> searchCondition);
```

`DeleteWhenMatched` takes delete operation options and returns new merge command builder with new operation.

### `merge`

Merge command builder. `DeleteWhenMatched` method will return new builder with new delete operation. It will not modify original object.

### `searchCondition`

Operation execution condition. Operation without condition will be applied to all matching records.  
If there are multiple operations within same match group - only last one could omit condition. `WhenMatched` match group could contain only `Update` and `Delete` operations.

## UpdateWhenNotMatchedBySource

```cs
IMergeable<TTarget, TSource> UpdateWhenNotMatchedBySource<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, TTarget>> setter);

IMergeable<TTarget, TSource> UpdateWhenNotMatchedBySourceAnd<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, bool>> searchCondition, Expression<Func<TTarget, TTarget>> setter);
```

IMPORTANT: This method could be used only with SQL Server.

`UpdateWhenNotMatchedBySource` takes update operation options and returns new merge command builder with new operation.

### `merge`

Merge command builder. `UpdateWhenNotMatchedBySource` method will return new builder with new update operation. It will not modify original object.

### `searchCondition`

Operation execution condition. Operation without condition will be applied to all matching records.  
If there are multiple operations within same group - only last one could omit condition. `WhenNotMatchedBySource` match group could contain only `UpdateWhenNotMatchedBySource` and `DeleteWhenNotMatchedBySource` operations. But due to SQL Server limitations you can use only one `UpdateWhenNotMatchedBySource` and `DeleteWhenNotMatchedBySource` operation in single command.

### `setter`

Record update expression. Defines update expressions for values in target record.

```cs
db.Table
    .Merge()
    .Using(source)
    .OnTargetKey()
    .UpdateWhenNotMatchedBySource(target => new TargetRecord()
    {
        Field1 = target.Field10,
        Field2 = target.Field2,
        Field3 = target.Field3 + 10
    })
    .Merge();
```

## DeleteWhenNotMatchedBySource

```cs
IMergeable<TTarget, TSource> DeleteWhenNotMatchedBySource<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge);

IMergeable<TTarget, TSource> DeleteWhenNotMatchedBySourceAnd<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, bool>> searchCondition);
```

IMPORTANT: This method could be used only with SQL Server.

`DeleteWhenNotMatchedBySource` takes delete operation options and returns new merge command builder with new operation.

### `merge`

Merge command builder. `DeleteWhenNotMatchedBySource` method will return new builder with new delete operation. It will not modify original object.

### `searchCondition`

Operation execution condition. Operation without condition will be applied to all matching records.  
If there are multiple operations within same group - only last one could omit condition. `WhenNotMatchedBySource` match group could contain only `UpdateWhenNotMatchedBySource` and `DeleteWhenNotMatchedBySource` operations. But due to SQL Server limitations you can use only one `UpdateWhenNotMatchedBySource` and `DeleteWhenNotMatchedBySource` operation in single command.

## UpdateWhenMatchedThenDelete

```cs
IMergeable<TTarget, TTarget> UpdateWhenMatchedThenDelete<TTarget>(this IMergeableSource<TTarget, TTarget> merge, Expression<Func<TTarget, TTarget, bool>> deleteCondition);

IMergeable<TTarget, TTarget> UpdateWhenMatchedAndThenDelete<TTarget>(this IMergeableSource<TTarget, TTarget> merge, Expression<Func<TTarget, TTarget, bool>> searchCondition, Expression<Func<TTarget, TTarget, bool>> deleteCondition);
```

```cs
IMergeable<TTarget, TSource> UpdateWhenMatchedThenDelete<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, TSource, TTarget>> setter, Expression<Func<TTarget, TSource, bool>> deleteCondition);

IMergeable<TTarget, TSource> UpdateWhenMatchedAndThenDelete<TTarget, TSource>(this IMergeableSource<TTarget, TSource> merge, Expression<Func<TTarget, TSource, bool>> searchCondition, Expression<Func<TTarget, TSource, TTarget>> setter, Expression<Func<TTarget, TSource, bool>> deleteCondition);
```

IMPORTANT: This method could be used only with Oracle Database.

`UpdateWhenMatchedThenDelete` method takes update and delete operation options and returns new merge command builder with new operation.

### `merge`

Merge command builder. `UpdateWhenMatchedThenDelete` method will return new builder with new update with delete operation. It will not modify original object.

### `searchCondition`

Update operation execution condition. Operation without condition will be applied to all matching records. Oracle doesn't support multiple commands in current match group. You can use only `UpdateWhenMatchedThenDelete` or `UpdateWhenMatched` in single command.

### `setter`

Record update expression. Optional. Defines update expressions for values in target record.

```cs
db.Table
    .Merge()
    .From(source)
    .OnTargetKey()
    .UpdateWhenMatchedThenDelete((target, source) => new TargetRecord()
    {
        Field1 = target.Field10,
        Field2 = source.Field2,
        Field3 = source.Field1 + target.Field2
    }, (updatedTarget, source) => updatedTarget.Field3 > 100)
    .Merge();
```

### `deleteCondition`

Delete operation execution condition. Identifies **updated** records that should be deleted. Note that this condition applied to updated target record with new field values.

## Merge

```cs
int Merge<TTarget, TSource>(this IMergeable<TTarget, TSource> merge);
Task<int> MergeAsync<TTarget, TSource>(this IMergeable<TTarget, TSource> merge, CancellationToken token = default);
```

`Merge` method builds and executes merge command against database and returns number of affected records. `MergeAsync` does the same job asynchronously.

### `merge`
Merge command builder.

### Notes
`Merge` returns number of affected records. Consult your database documentation for more details, but in general except SAP/Sybase ASE it is the same for all databases.