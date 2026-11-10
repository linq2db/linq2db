# Query Extensions

LinqToDB contains different mechanisms to extend and customize generated SQL. Query Extensions are designed to extend SQL on clause and statement level such as table, join, query hints, etc.

## Common Hint Extensions

The `QueryExtensionScope` enumeration defines a scope where this extension is applied to. 

| Value | Method | Applied to | Description |
| --- | --- | --- | --- |
| None | Path through methods | `IQueryable` | This type of extension should not generate SQL and can be used to implement path through methods such as `AsSqlServer()` that converts `IQueryable` sequence to `ISqlServerSpecificQueryable`. |
| TableHint | `TableHint`, `With` | `ITable` | Generates table hints. |
| TablesInScopeHint | `TablesInScopeHint` | `IQueryable` | This method applies provided hint to all the tables in scope of this method. It is supported by the same database providers as TableHint. |
| IndexHint | `IndexHint` | `ITable` | MySql supports both hint styles: Oracle Optimizer Hints and SqlServer Table Hints. The TableHint extension generates Oracle hints, whereas this extension supports SqlServer hint style. |
| JoinHint | `JoinHint` | `IQueryable` | Generates join hints. |
| SubQueryHint | `SubQueryHint` | `IQueryable` | Generates subquery or statement hints. Supported by PostgreSQL. |
| QueryHint | `QueryHint` | `IQueryable` | Generates statement hints. |

### TableID method and Sql.SqlID class

Some hints require references to table specifications or aliases. LinqToDB automatically generates table and subquery aliases, so the idea to use generated names for hints is definitely not the best. The `TableID` method assigns provided identifier to a table and this ID can be used later to generate references to the table. The following methods can be used as hint parameters to generate table references:

| Method | Description |
| --- | --- |
| `Sql.TableAlias("id")` | Generates table alias. |
| `Sql.TableName("id")` | Generates table name. |
| `Sql.TableSpec("id")`| Generates table specification. May include query block name. |

### Naming Query Blocks

Oracle and MySql table-level, index-level, and subquery optimizer hints permit specific query blocks to be named as part of their argument syntax. To create these names, use the following methods:

```c#
AsSubQuery("qb_name")
QueryName("qb_name")
```

### Examples

#### Access

```c#
var q =
(
    from p in db.Parent
    select p
)
.QueryHint(AccessHints.Query.WithOwnerAccessOption);
```

```sql
SELECT
    [p].[ParentID],
    [p].[Value1]
FROM
    [Parent] [p]
WITH OWNERACCESS OPTION
```

#### MySql

```c#
var q =
(
    from p in
        (
            from p in db.Parent.TableID("Pr")
                .TableHint(MySqlHints.Table.NoBka)
                .TableHint(MySqlHints.Table.Index, "PK_Parent")
            from c in db.Child.TableID("Ch")
                .IndexHint(MySqlHints.Table.UseKeyForOrderBy, "IX_ChildIndex", "IX_ChildIndex2")
            select p
        )
        .AsSubQuery("qq")
    select p
)
.QueryHint(MySqlHints.Query.NoBka,  Sql.TableSpec("Pr"), Sql.TableSpec("Ch"))
.QueryHint(MySqlHints.Query.SetVar, "sort_buffer_size=16M");
```

```sql
SELECT /*+ NO_BKA(p@qq) INDEX(p@qq PK_Parent) NO_BKA(p@qq, c_1@qq) SET_VAR(sort_buffer_size=16M) */
    `p_1`.`ParentID`,
    `p_1`.`Value1`
FROM
    (
        SELECT /*+ QB_NAME(qq) */
            `p`.`ParentID`,
            `p`.`Value1`
        FROM
            `Parent` `p`,
            `Child` `c_1` USE KEY FOR ORDER BY(IX_ChildIndex, IX_ChildIndex2)
    ) `p_1`
```

#### Oracle

```c#
var q =
(
    from p in
        (
            from c in db.Child
                .TableHint(OracleHints.Hint.Full)
                .TableHint(OracleHints.Hint.Parallel, "DEFAULT")
            join p in db.Parent
                .TableHint(OracleHints.Hint.DynamicSampling, 1)
                .TableHint(OracleHints.Hint.Index, "parent_ix")
                .AsSubQuery("Parent")
            on c.ParentID equals p.ParentID
            select p
        )
        .AsSubQuery()
    select p
)
.QueryHint(OracleHints.Hint.NoUnnest, "@Parent");
```

```sql
SELECT /*+ FULL(p_1.c_1) PARALLEL(p_1.c_1 DEFAULT) DYNAMIC_SAMPLING(t1@Parent 1) INDEX(t1@Parent parent_ix) NO_UNNEST(@Parent) */
    p_1."ParentID",
    p_1."Value1"
FROM
    (
        SELECT
            p."ParentID",
            p."Value1"
        FROM
            "Child" c_1
                INNER JOIN (
                    SELECT /*+ QB_NAME(Parent) */
                        t1."ParentID",
                        t1."Value1"
                    FROM
                        "Parent" t1
                ) p ON c_1."ParentID" = p."ParentID"
    ) p_1
```

#### PostgreSQL

```c#
var q =
(
    from p in
        (
            from p in
                (
                    from p in db.Parent
                    from c in db.Child
                    where c.ParentID == p.ParentID
                    select p
                )
                .SubQueryHint(PostgreSQLHints.ForUpdate)
                .AsSubQuery()
            where p.ParentID < -100
            select p
        )
        .SubQueryHint(PostgreSQLHints.ForShare)
    select p
)
.SubQueryHint(PostgreSQLHints.ForKeyShare + " " + PostgreSQLHints.SkipLocked);
```

```sql
SELECT
    p_1."ParentID",
    p_1."Value1"
FROM
    (
        SELECT
            p."ParentID",
            p."Value1"
        FROM
            "Parent" p,
            "Child" c_1
        WHERE
            c_1."ParentID" = p."ParentID"
        FOR UPDATE
    ) p_1
WHERE
    p_1."ParentID" < -100
FOR SHARE
FOR KEY SHARE SKIP LOCKED
```

#### SqlCe

```c#
from p in db.Person
    .TableHint(SqlCeHints.Table.Index, "PK_Person")
    .With(SqlCeHints.Table.NoLock)
select p;
```

```sql
SELECT
    [p].[FirstName],
    [p].[PersonID],
    [p].[LastName],
    [p].[MiddleName],
    [p].[Gender]
FROM
    [Person] [p] WITH (Index(PK_Person), NoLock)
```

#### SQLite

```c#
from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
where p.ID > 0
select p;
```

```sql
SELECT
    [p].[FirstName],
    [p].[PersonID],
    [p].[LastName],
    [p].[MiddleName],
    [p].[Gender]
FROM
    [Person] [p] INDEXED BY IX_PersonDesc
WHERE
    [p].[PersonID] > 0
```

#### SqlServer

```c#
var q =
(
    from c in db.Child
        .TableHint(SqlServerHints.Table.SpatialWindowMaxCells(10))
        .IndexHint(SqlServerHints.Table.Index, "IX_ChildIndex")
    join p in
        (
            from t in db.Parent.With(SqlServerHints.Table.NoLock)
            where t.Children.Any()
            select new { t.ParentID, t.Children.Count }
        )
        .JoinHint(SqlServerHints.Join.Hash) on c.ParentID equals p.ParentID
    select p
)
.QueryHint(SqlServerHints.Query.Recompile)
.QueryHint(SqlServerHints.Query.Fast(10))
.QueryHint(SqlServerHints.Query.MaxGrantPercent(25));
```

```sql
SELECT
    [p].[ParentID],
    [p].[Count_1]
FROM
    [Child] [c_1] WITH (SPATIAL_WINDOW_MAX_CELLS=10, Index(IX_ChildIndex))
        INNER HASH JOIN (
            SELECT
                [t].[ParentID],
                (
                    SELECT
                        Count(*)
                    FROM
                        [Child] [t1]
                    WHERE
                        [t].[ParentID] = [t1].[ParentID]
                ) as [Count_1]
            FROM
                [Parent] [t] WITH (NoLock)
            WHERE
                EXISTS(
                    SELECT
                        *
                    FROM
                        [Child] [t2]
                    WHERE
                        [t].[ParentID] = [t2].[ParentID]
                )
        ) [p] ON [c_1].[ParentID] = [p].[ParentID]
OPTION (RECOMPILE, FAST 10, MAX_GRANT_PERCENT=25)
```

## Database Specific Hint Extensions

The extension methods above are common and can be used to generate SQL for all database providers. You will be responsible for generated SQL as LinqToDB will generate SQL based on what you pass as parameters. Besides, LinqToDB implements database specific hint extensions. These extensions are designed specially for specific providers in the type-safe way and are “provider friendly” (which means you can use different specific database hints applied for the same LINQ query and they will not conflict).

### C#

```c#
var q =
(
    from p in db.Parent.TableID("pr")
        .AsMySql()
            .NoBatchedKeyAccessHint()
            .IndexHint("PK_Parent")
    from c in db.Child.TableID("ch")
        .AsMySql()
            .UseIndexHint("IX_ChildIndex")
        .AsOracle()
            .FullHint()
            .HashHint()
        .AsSqlCe()
            .WithNoLock()
        .AsSQLite()
            .NotIndexedHint()
        .AsSqlServer()
            .WithNoLock()
            .WithNoWait()
    join t in db.Patient.TableID("pt")
        .AsSqlServer()
            .JoinLoopHint()
    on c.ParentID equals t.PersonID
    select t
)
.QueryName("qb")
.AsAccess()
    .WithOwnerAccessOption()
.AsMySql()
    .MaxExecutionTimeHint(1000)
    .BatchedKeyAccessHint(Sql.TableSpec("ch"))
.AsOracle()
    .ParallelHint(2)
    .NoUnnestHint("qb")
.AsPostgreSQL()
    .ForShareHint(Sql.TableAlias("pt"))
.AsSqlServer()
    .WithReadUncommittedInScope()
    .OptionRecompile()
    .OptionTableHint(Sql.TableAlias("pr"), SqlServerHints.Table.ReadUncommitted)
    .OptionNoPerformanceSpool()
;
```

### SQL

#### Access

```sql
SELECT
    [t].[PersonID],
    [t].[Diagnosis]
FROM
    (
        SELECT
            [c_1].[ParentID]
        FROM
            [Parent] [p],
            [Child] [c_1]
    ) [t1]
        INNER JOIN [Patient] [t] ON ([t1].[ParentID] = [t].[PersonID])
WITH OWNERACCESS OPTION
```

#### MySql

```sql
SELECT /*+ QB_NAME(qb) NO_BKA(t1.p@qb) INDEX(t1.p@qb PK_Parent) MAX_EXECUTION_TIME(1000) BKA(t1.c_1@qb) */
    `t`.`PersonID`,
    `t`.`Diagnosis`
FROM
    (
        SELECT
            `c_1`.`ParentID`
        FROM
            `Parent` `p`,
            `Child` `c_1` USE INDEX(IX_ChildIndex)
    ) `t1`
        INNER JOIN `Patient` `t` ON `t1`.`ParentID` = `t`.`PersonID`
```

#### Oracle

```sql
SELECT /*+ QB_NAME(qb) FULL(t1.c_1@qb) HASH(t1.c_1@qb) PARALLEL(2) NO_UNNEST(qb) */
    t."PersonID",
    t."Diagnosis"
FROM
    (
        SELECT
            c_1."ParentID"
        FROM
            "Parent" p,
            "Child" c_1
    ) t1
        INNER JOIN "Patient" t ON t1."ParentID" = t."PersonID"
```

#### PostgreSQL

```sql
SELECT /* qb */
    t."PersonID",
    t."Diagnosis"
FROM
    (
        SELECT
            c_1."ParentID"
        FROM
            "Parent" p,
            "Child" c_1
    ) t1
        INNER JOIN "Patient" t ON t1."ParentID" = t."PersonID"
FOR SHARE OF t
```

#### SqlCe

```sql
SELECT /* qb */
    [t].[PersonID],
    [t].[Diagnosis]
FROM
    (
        SELECT
            [c_1].[ParentID]
        FROM
            [Parent] [p],
            [Child] [c_1] WITH (NoLock)
    ) [t1]
        INNER JOIN [Patient] [t] ON [t1].[ParentID] = [t].[PersonID]
```

#### SQLite

```sql
SELECT /* qb */
    [t].[PersonID],
    [t].[Diagnosis]
FROM
    (
        SELECT
            [c_1].[ParentID]
        FROM
            [Parent] [p],
            [Child] [c_1] NOT INDEXED
    ) [t1]
        INNER JOIN [Patient] [t] ON [t1].[ParentID] = [t].[PersonID]
```

#### SqlServer 2005

```sql
SELECT /* qb */
    [t].[PersonID],
    [t].[Diagnosis]
FROM
    (
        SELECT
            [c_1].[ParentID]
        FROM
            [Parent] [p] WITH (ReadUncommitted),
            [Child] [c_1] WITH (NoLock, NoWait, ReadUncommitted)
    ) [t1]
        INNER LOOP JOIN [Patient] [t] WITH (ReadUncommitted) ON [t1].[ParentID] = [t].[PersonID]
OPTION (RECOMPILE)
```

#### SqlServer 2008

```sql
SELECT /* qb */
    [t].[PersonID],
    [t].[Diagnosis]
FROM
    (
        SELECT
            [c_1].[ParentID]
        FROM
            [Parent] [p] WITH (ReadUncommitted),
            [Child] [c_1] WITH (NoLock, NoWait, ReadUncommitted)
    ) [t1]
        INNER LOOP JOIN [Patient] [t] WITH (ReadUncommitted) ON [t1].[ParentID] = [t].[PersonID]
OPTION (RECOMPILE, TABLE HINT(p, ReadUncommitted))
```

#### SqlServer 2019

```sql
SELECT /* qb */
    [t].[PersonID],
    [t].[Diagnosis]
FROM
    (
        SELECT
            [c_1].[ParentID]
        FROM
            [Parent] [p] WITH (ReadUncommitted),
            [Child] [c_1] WITH (NoLock, NoWait, ReadUncommitted)
    ) [t1]
        INNER LOOP JOIN [Patient] [t] WITH (ReadUncommitted) ON [t1].[ParentID] = [t].[PersonID]
OPTION (RECOMPILE, TABLE HINT(p, ReadUncommitted), NO_PERFORMANCE_SPOOL)
```
