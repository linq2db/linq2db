#### Changelog
<details> 
<summary>Show</summary>
<p>

- replaced `Insert*With` API with `Insert*(Iqueryable<cte>)`
- [pgsql] specificed pgsql support for designer APIs
- [pgsql] added `string conflictConstraint` parameter overload for `ON CONFLICT` clause
- [pgsql] added `RETURNING` APIs (both materialized and as subquery)
- corrected return type for `*WithOutput` APIs
- corrected type parameters naming
- [pgsql] added missing `IsertOrNothing*Output` APIs
- [mysql/mariadb] specified support by mariadb and/or mysql and added missing APIs
- added naming conventions
- separate method groups visually and format APIs
- [pgsql] added missing enumerable source overrides
- [mssql] add `InsertWithOutputInto()`
- [access] add Access support info
- [db2] add DB2 LUW support info, add `InsertInto*` API to support insert into updatable CTE/Query
- [db2] add DB2 z/OS support info
- [db2] add DB2 iSeries support info (to not miss required APIs)
- [firebird] add Firebird insert support info
- [informix] add Informix insert support info
- [mysql/mariadb] add MySql/MariaDB `REPLACE` statement support
- refactor document structure
- merge InsertOrNothing APIs into InsertOrAction
- merge UPSERT APIs into InsertOrAction
- [oracle] update oracle support info
- [saphana] update sap hana support info
- [sqlce] update sqlce support info
- [sql server] update sql server support info
- [sybase ase] add ASE support info

</p></details>

Here we review API only for `INSERT`, `INSERT OR UPDATE`, `INSERT ON CONFLICT`-like statements. Following statements are not covered by this design:
- `SELECT INTO <new_table>` statements
- data output to/from file

Note that some of mentioned APIs already exist and we just need to improve generated SQL.

#### References
- MS Access: [INSERT INTO](https://docs.microsoft.com/en-us/office/client-developer/access/desktop-database-reference/insert-into-statement-microsoft-access-sql)
- DB2 LUW: [INSERT](https://www.ibm.com/support/knowledgecenter/SSEPGG_11.5.0/com.ibm.db2.luw.sql.ref.doc/doc/r0000970.html?pos=2)
- DB2 z/OS: [INSERT](https://www.ibm.com/support/knowledgecenter/SSEPEK_12.0.0/sqlref/src/tpc/db2z_sql_insert.html)
- DB2 iSeries: [INSERT](https://www.ibm.com/support/knowledgecenter/ssw_ibm_i_74/db2/rbafzbackup.htm)
- Firebird: [INSERT + UPDATE OR INSERT](https://firebirdsql.org/en/reference-manuals/) (see 3.0 russian pdf for latest docs)
- Informix: [INSERT](https://www.ibm.com/support/knowledgecenter/SSGU8G_14.1.0/com.ibm.sqls.doc/ids_sqs_0861.htm)
- MariaDB: [INSERT](https://mariadb.com/kb/en/insert/), [REPLACE](https://mariadb.com/kb/en/replace/)
- MySQL: [INSERT](https://dev.mysql.com/doc/refman/8.0/en/insert.html), [REPLACE](https://dev.mysql.com/doc/refman/8.0/en/replace.html)
- Oracle: [INSERT](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/INSERT.html#GUID-903F8043-0254-4EE9-ACC1-CB8AC0AF3423)
- PostgreSQL: [INSERT](https://www.postgresql.org/docs/13/sql-insert.html)
- SAP HANA: [INSERT](https://help.sap.com/viewer/4fe29514fd584807ac9f2a04f6754767/2.0.03/en-US/20f7f70975191014a76da70c9181720e.html), [UPSERT](https://help.sap.com/viewer/4fe29514fd584807ac9f2a04f6754767/2.0.03/en-US/20fc06a7751910149892c0d09be21a38.html)
- SQL CE: [INSERT](https://docs.microsoft.com/en-us/previous-versions/sql/sql-server-2005/ms174633(v=sql.90))
- SQLite: [INSERT](https://sqlite.org/lang_insert.html), [UPSERT clause](https://sqlite.org/lang_upsert.html)
- MS SQL: [INSERT](https://docs.microsoft.com/en-us/sql/t-sql/statements/insert-transact-sql?view=sql-server-ver15), [OUTPUT clause](https://docs.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql?view=sql-server-ver15)
- Sybase ASE: [INSERT](https://help.sap.com/viewer/e0d4539d39c34f52ae9ef822c2060077/16.0.3.9/en-US/ab31940ebc2b1014a8dda7c8ca59cf89.html)

#### Naming conventions and other notes

##### Type parameters
- `TTarget`: type of insert target table mapping
- `TSource`: type of insert source record (CTE, query or enumerable collection)
- `TOutput`: type of output clause record

##### Method naming components
- `Insert*`  prefix: performs `INSERT` operation into table or view (`ITable<T>`)
- `InsertInto*`  prefix: performs `INSERT` operation into select query or CTE (name?) (`IQueryable<T>`)
- `InsertOrAction*` prefix: performs `INSERT ... ON CONFLICT <CONFLICT_BEHAVIOR>` operation
- `InsertOrUpdate*` prefix: performs `INSERT ... ON CONFLICT UPDATE` operation
- `*WithOutput` suffix: return data from `OUTPUT` clause
- `*AsOutput` suffix: return operation with `OUTPUT` clause as selectable subquery
- `*WithOutputInto` suffix: insert data from `OUTPUT` clause directly into another table

##### CTE support
`IQueryable` data source could be subquery or CTE and not all databases support both of them with insert. Below we document support per-database (using latest version of DB).

|  Database   |       Subquery     |        CTE         |
|-------------|--------------------|--------------------|
| Access      | :heavy_check_mark: |         :x:        |
| DB2 LUW     | :heavy_check_mark: | :heavy_check_mark: |
| DB2 z/OS    | :heavy_check_mark: | :heavy_check_mark: |
| DB2 iSeries | :heavy_check_mark: | :heavy_check_mark: |
| Firebird    | :heavy_check_mark: | :heavy_check_mark: |
| Informix    | :heavy_check_mark: |         :x:        |
| MariaDB     | :heavy_check_mark: | :heavy_check_mark: |
| MySQL       | :heavy_check_mark: | :heavy_check_mark: |
| Oracle      | :heavy_check_mark: | :heavy_check_mark: |
| PostgreSQL  | :heavy_check_mark: | :heavy_check_mark: |
| SAP HANA    | :heavy_check_mark: | :heavy_check_mark: |
| SQL CE      | :heavy_check_mark: |         :x:        |
| SQLite      | :heavy_check_mark: | :heavy_check_mark: |
| SQL Server  | :heavy_check_mark: | :heavy_check_mark: |
| SYBASE ASE  | :heavy_check_mark: |         :x:        |

## Proposed APIs

### INSERT operation

We already have some APIs in this section implemented, so we need to add new methods and improve SQL generation for existing

<details>
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query
- by result: output into another table

```cs
// Access
// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// Firebird
// Informix
// MariaDB
// MySQL
// Oracle
// PostgreSQL
// SAP HANA
// SQL CE
// SQLite
// SQL Server
// Sybase ASE
int Insert<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter);

// DB2 LUW
// Oracle
// SQL Server
int InsertInto<TTarget>(
    this IQueryable<TTarget> target,
    Expression<Func<TTarget>> setter);

// Access
// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// Firebird
// Informix
// MariaDB
// MySQL
// Oracle
// PostgreSQL
// SAP HANA
// SQL CE
// SQLite
// SQL Server
// Sybase ASE
int Insert<TSource, TTarget>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter);

// DB2 LUW
// Oracle
// SQL Server
int InsertInto<TSource, TTarget>(
    this IQueryable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter);

// this API needs additional design if we decide to implement it
// Informix: supports procedures and functions
// SQL Server: supports procedures, functions and rawsql
// also for SQL Server other supported combinations of output/target APIs with EXECUTE source allowed
int Insert<TSource, TTarget>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    string procedureOrFunctionName, // orSql?
    bool procedure, // or function
    ?[] parameters, // we use various types for parameters, not important for this document
    // note: would be nice to isolate this junk already in some custom struct (ObjectName)
    string? schema = null,
    string? database = null,
    string? server = null);

// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// MariaDB
// MySQL
// Oracle (emulation through UNION ALL FROM DUAL query)
// PostgreSQL
// SAP HANA (emulation through UNION ALL FROM DUMMY)
// SQL CE (emulation through UNION ALL)
// SQLite
// SQL Server
int Insert<TSource, TTarget>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter);

// DB2 LUW
// Oracle (emulation through UNION ALL FROM DUAL query)
// SQL Server
int InsertInto<TSource, TTarget>(
    this IEnumerable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter);

/** WithOutput **/

// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// Firebird
// MariaDB
// PostgreSQL
// SQL Server
IEnumerable<TOutput> InsertWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// SQL Server
IEnumerable<TOutput> InsertIntoWithOutput<TTarget, TOutput>(
    this IQueryable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// Firebird
// MariaDB
// PostgreSQL
// SQL Server
IEnumerable<TOutput> InsertWithOutput<TSource, TTarget, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// SQL Server
IEnumerable<TOutput> InsertIntoWithOutput<TSource, TTarget, TOutput>(
    this IQueryable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// MariaDB
// PostgreSQL
// SQL Server
IEnumerable<TOutput> InsertWithOutput<TSource, TTarget, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// SQL Server
IEnumerable<TOutput> InsertIntoWithOutput<TSource, TTarget, TOutput>(
    this IEnumerable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// PostgreSQL
// SQL Server (only as source for another insert)
IQueryable<TOutput> InsertAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// SQL Server (only as source for another insert)
IQueryable<TOutput> InsertIntoAsOutput<TTarget, TOutput>(
    this IQueryable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// PostgreSQL
// SQL Server (only as source for another insert)
IQueryable<TOutput> InsertAsOutput<TSource, TTarget, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// SQL Server (only as source for another insert)
IQueryable<TOutput> InsertIntoAsOutput<TSource, TTarget, TOutput>(
    this IQueryable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// DB2 z/OS
// DB2 iSeries
// PostgreSQL
// SQL Server (only as source for another insert)
IQueryable<TOutput> InsertAsOutput<TSource, TTarget, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// DB2 LUW
// SQL Server (only as source for another insert)
IQueryable<TOutput> InsertIntoAsOutput<TSource, TTarget, TOutput>(
    this IEnumerable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource, TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** Into **/

// SQL Server
int InsertWithOutput<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    ITable<TTarget> outputTable);

// SQL Server
int InsertWithOutput<TTarget,TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    ITable<TOutput> outputTable,
    Expression<Func<TTarget, TOutput>> outputSelector);

// SQL Server
int InsertWithOutputInto<TSource,TTarget>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TTarget> outputTable);

// SQL Server
int InsertWithOutputInto<TSource,TTarget,TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TOutput> outputTable,
    Expression<Func<TTarget, TOutput>> outputSelector);

// SQL Server
int InsertIntoWithOutputInto<TSource,TTarget>(
    this IQueryable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TTarget> outputTable);

// SQL Server
int InsertIntoWithOutputInto<TSource,TTarget,TOutput>(
    this IQueryable<TSource> source,
    IQueryable<TTarget> target,
    Expression<Func<TSource,TTarget>> setter,
    ITable<TOutput> outputTable,
    Expression<Func<TTarget, TOutput>> outputSelector);

```
</p></details>

### INSERT OR UPDATE operation

This operation defines insert or update operation with separate setters for insert and update.

##### INSERT OR UPDATE: basic

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// MariaDB
// MySQL
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// MariaDB
// MySQL
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// MariaDB
// MySQL
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

/** WithOutput **/

// MariaDB
// PostgreSQL
//
TOutput? InsertOrUpdateWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// MariaDB
// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// MariaDB
// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

##### INSERT OR UPDATE: with extra condition on update

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrUpdateWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

##### INSERT OR UPDATE: with explicit conflict columns

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrUpdateWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutputT, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

##### INSERT OR UPDATE: with conflict on constraint

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// PostgreSQL
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// PostgreSQL
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrUpdateWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

##### INSERT ON UPDATE: with explicit conflict columns and extra condition for update

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrUpdateWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

##### INSERT OR UPDATE: with conflict on constraint and extra condition on update

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrUpdateWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTargetTTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target, Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source, ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this Enumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

##### INSERT OR UPDATE: with explicit conflict columns and conflict condition

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

// PostgreSQL
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrUpdateWithOutput<TTarget>(
    this ITable<TTarget, TOutput> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

##### INSERT OR UPDATE: with explicit conflict columns and conflict condition and extra condition on update

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

// PostgreSQL
// SQLite
int InsertOrUpdate<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrUpdateWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrUpdateWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrUpdateAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, TTarget>> update,
    Expression<Func<TSource, TTarget/*old*/, TTarget/*new*/, bool>> updateCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p>
</details>

### INSERT WITH CONFLICT ACTION

This operation defines how insert conflicts should be handled (except update, covered above).

I've included UPSERT and InsertOrNothing API as InsertOrAction with corresponding action for now instead of separate APIs.

<details> 
<summary>Conflict Actions</summary>
<p>

```cs
// defines insert conflict action
enum ConflictAction
{
    // DB-specific replace strategy used (remove conflicting record(s) and insert new)
    //
    // MariaDB (REPLACE statement)
    // MySQL (REPLACE statement)
    // SQLite
    Replace,

    // Performs update on conflict (UPSERT)
    // compared to InsertOrUpdate API, it doesn't have separate UPDATE setters
    //
    // Firebird
    // SAP HANA
    Update,

    // Rollback current statement/transaction
    //
    // SQLite
    Rollback,

    // Aborts current statement and rollback statement changes
    //
    // SQLite
    Abort,

    // Aborts current statement but don't rollback changes, already made by current statement
    //
    // SQLite
    Fail,

    // Do nothing (ignores changes request) to current record and continue statement execution
    // similar to Ignore and we use two fields because:
    // - sqlite has two actions with different functionality and logic
    // - different dbs use different naming: NOTHING vs IGNORE and it could became confusing
    //
    // SQLite (ON CONFLICT DO NOTHING)
    // PostgreSQL (ON CONFLICT DO NOTHING)
    Nothing,

    // Do nothing (ignores changes request) to current record and continue statement execution
    // Also see Nothing
    //
    // MariaDB (INSERT IGNORE)
    // MySQL (INSERT IGNORE)
    // SQLite (INSERT OR IGNORE)
    Ignore,
}
```

</p></details>

##### INSERT WITH CONFLICT ACTION: basic (by primary key and/or other db-specific default conflict conditions)

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// Firebird
// MariaDB
// MySQL
// PostgreSQL
// SAP HANA
// SQLite
int InsertOrAction<TTarget>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter);

// MariaDB
// MySQL
// PostgreSQL
// SAP HANA
// SQLite
int InsertOrAction<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TSource, TTarget>> setter);

// MariaDB
// MySQL
// PostgreSQL
// SQLite
int InsertOrAction<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TSource, TTarget>> setter);

/** WithOutput **/

// MariaDB
// PostgreSQL
TOutput? InsertOrActionWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// Firebird
TOutput? InsertOrActionWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget /* old */, TTarget /* new */, TOutput>> outputSelector);

// MariaDB
// PostgreSQL
IEnumerable<TOutput> InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// MariaDB
// PostgreSQL
IEnumerable<TOutput> InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p></details>

##### INSERT WITH CONFLICT ACTION: with explicit conflict columns

<details>
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// Firebird
// PostgreSQL
// SQLite
int InsertOrAction<TTarget>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns);

// PostgreSQL
// SQLite
int InsertOrAction<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns);

// PostgreSQL
// SQLite
int InsertOrAction<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrActionWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, TOutput>> outputSelector);

// Firebird
TOutput? InsertOrActionWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget /* old */, TTarget /* new */, TOutput>> outputSelector);

// PostgreSQL
TOutput? InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
TOutput? InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p></details>

##### INSERT WITH CONFLICT ACTION: with conflict constraint

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
int InsertOrAction<TTarget>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint);

// PostgreSQL
int InsertOrAction<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint);

// PostgreSQL
int InsertOrAction<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrActionWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    string conflictConstraint,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p></details>

##### INSERT WITH CONFLICT ACTION: with explicit conflict columns with conflict condition

<details> 
<summary>Show</summary>
<p>

Variations:
- by source: single record source
- by source: cte/query data source
- by source: enumerable source
- by result: number of rows affected
- by result: output
- by result: output as query

```cs
// PostgreSQL
// SQLite
int InsertOrAction<TTarget>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition);

// PostgreSQL
// SQLite
int InsertOrAction<TTarget, TSource>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition);

// PostgreSQL
// SQLite
int InsertOrAction<TTarget, TSource>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition);

/** WithOutput **/

// PostgreSQL
TOutput? InsertOrActionWithOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IEnumerable<TOutput> InsertOrActionWithOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

/** AsOutput **/

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TOutput>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IQueryable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);

// PostgreSQL
IQueryable<TOutput> InsertOrActionAsOutput<TTarget, TSource, TOutput>(
    this IEnumerable<TSource> source,
    ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget>> indexColumns,
    Expression<Func<TTarget, bool>> conflictCondition,
    Expression<Func<TTarget, TOutput>> outputSelector);
```
</p></details>

##### INSERT WITH CONFLICT ACTION: with conflict condition

<details> 
<summary>Show</summary>
<p>

```cs
// SAP HANA
int InsertOrAction<TTarget>(
    this ITable<TTarget> target,
    ConflictAction action,
    Expression<Func<TTarget>> setter,
    Expression<Func<TTarget, bool>> conflictCondition);
```

</p></details>