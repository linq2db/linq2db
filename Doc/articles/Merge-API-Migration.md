---
uid: Merge-API-Migration
---
# Migrating from old Merge API to new

This page contains information how to replace old Merge API calls with new API calls.  

## Breaking changes

Old API consider empty source list as noop operation and returns 0 without request to database. New version allways send command to database because:

- it will help to find errors in your command
- it will fix `by source` operations for SQL Server, which make sense for empty source

Exception: Oracle, Sybase and SAP HANA implementations still use noop approach due to too aggressive type checking.

## Code migration

Old API has 4x2 Merge methods. One method accepts target table as first parameter, another - `DataConnection` instance. New API works only with tables as target so you will need to get table from data connection using following code:

```cs
dataConnection.GetTable<TTable>()
```

If you used `tableName`, `databaseName` or `schemaName` parameters, replace them with follwing calls on table:
```cs
db.GetTable<T>()
    .TableName(tableName)
    .DatabaseName(databaseName)
    .SchemaName(schemaName);
```

### Method 1

Parameters `tableName`, `databaseName` and `schemaName` omitted.

```cs
// Old API
int Merge<T>(this DataConnection dataConnection, IQueryable<T> source, Expression<Func<T,bool>> predicate);
int Merge<T>(this ITable<T> table, IQueryable<T> source, Expression<Func<T,bool>> predicate);

// New API
// You can (and should) remove .AsEnumerable() - it was added to copy old behavior
db.GetTable<T>()
    .Merge()
    .Using(source.Where(predicate).AsEnumerable())
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .DeleteWhenNotMatchedBySourceAnd(predicate)
    .Merge();
```

### Method 2

Parameters `tableName`, `databaseName` and `schemaName` omitted.

```cs
// Old API
int Merge<T>(this DataConnection dataConnection, Expression<Func<T,bool>> predicate, IEnumerable<T> source)
int Merge<T>(this ITable<T> table, Expression<Func<T,bool>> predicate, IEnumerable<T> source);

// New API
db.GetTable<T>()
    .Merge()
    .Using(source)
    .OnTatgetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .DeleteWhenNotMatchedBySourceAnd(predicate)
    .Merge();
```

### Method 3

Parameters `tableName`, `databaseName` and `schemaName` omitted.

```cs
// Old API
int Merge<T>(this DataConnection dataConnection, bool delete, IEnumerable<T> source);
int Merge<T>(this ITable<T> table, bool delete, IEnumerable<T> source);

// New API
// (delete = true)
db.GetTable<T>()
    .Merge()
    .Using(source)
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .DeleteWhenNotMatchedBySource()
    .Merge();
// (delete = false)
db.GetTable<T>()
    .Merge()
    .Using(source)
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```

### Method 4

Parameters `tableName`, `databaseName` and `schemaName` omitted.

```cs
// Old API
int Merge<T>(this DataConnection dataConnection, IEnumerable<T> source);
int Merge<T>(this ITable<T> table, IEnumerable<T> source);

// New API
db.GetTable<T>()
    .Merge()
    .Using(source)
    .OnTargetKey()
    .UpdateWhenMatched()
    .InsertWhenNotMatched()
    .Merge();
```