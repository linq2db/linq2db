---
uid: Merge-API-Background
---
# Merge API Background Information

Merge API uses `MERGE INTO` command defined by `SQL:2003` standard with updates in `SQL:2008`. Additionally we support some non-standard extensions to this command. See specific database engine support information below.
Later we plan to extend providers support by adding support for `UPSERT`-like commands.

## Basic syntax (SQL:2003)

```sql
MERGE INTO <target_table> [[AS] <alias>]
      USING <source_data_set> [[AS] <alias>]
      ON <match_condition>
      -* one or both cases could be specified
      WHEN MATCHED THEN <update_operation>
      WHEN NOT MATCHED THEN <insert_operation>

<update_operation> := UPDATE SET <column> = <value> [, <column> = <value>]
<insert_operation> := INSERT (<column_list>) VALUES(<values_list>)
```

## Advanced syntax (SQL:2008 extensions)

### Multiple `MATCH` cases

It is possible to perform different operations for records, matched by ON match condition by specifying extra conditions on `WHEN` statement:

```sql
WHEN [NOT] MATCHED [AND <extra_condition>] THEN <match_specific_operation>
```

### `DELETE` operation

`DELETE` operation could be used for `WHEN MATCHED` case.

```sql
WHEN MATCHED [AND <extra condition>] THEN DELETE
```

## Links

* [MERGE on wikibooks](https://en.wikibooks.org/wiki/Structured_Query_Language/MERGE)
* [SQL grammar](https://jakewheat.github.io/sql-overview/) see SQL:2003 and SQL:2011 (sic! grammars)

## Supported Databases

- [Merge API Background Information](#merge-api-background-information)
  - [Basic syntax (SQL:2003)](#basic-syntax-sql2003)
  - [Advanced syntax (SQL:2008 extensions)](#advanced-syntax-sql2008-extensions)
    - [Multiple `MATCH` cases](#multiple-match-cases)
    - [`DELETE` operation](#delete-operation)
  - [Links](#links)
  - [Supported Databases](#supported-databases)
  - [General considerations](#general-considerations)
  - [Microsoft SQL Server 2008+](#microsoft-sql-server-2008)
  - [IBM DB2](#ibm-db2)
  - [Firebird](#firebird)
  - [Oracle Database](#oracle-database)
  - [Sybase/SAP ASE](#sybasesap-ase)
  - [IBM Informix](#ibm-informix)
  - [SAP HANA 2](#sap-hana-2)
  - [PostgreSQL](#postgresql)

## General considerations

Not all data types supported or have limited support for some providers right now if you use client-side source. Usually it will be binary types. Check notes for specific provider below.

## Microsoft SQL Server 2008+

Microsoft SQL Server supports Merge API starting from SQL Server 2008 release.
It supports all features from `SQL:2008` standard and adds support for two new operations, not available for other providers:

* Update by source operation
* Delete by source operation

Those two operations allow to update or delete target record when no matching record found in source. Of course it means that only target record available in context of those two operations.

Limitations:

* operation of each type can be used only once in merge command even with different predicates
* only up to three operations supported in single command

Other notes:

* identity insert enabled for insert operation

Links:

* [MERGE INTO command](https://learn.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql)

## IBM DB2

Note: merge implementation was tested only on DB2 LUW.

DB2 supports all features from `SQL:2008` standard.

Limitations:

* doesn't support associations (joins) in match predicate

Links:

* [MERGE INTO DB2 z/OS 12](https://www.ibm.com/docs/en/db2-for-zos/12?topic=statements-merge)
* [MERGE INTO DB2 iSeries 7.3](https://www.ibm.com/docs/en/i/7.3?topic=statements-merge)
* [MERGE INTO DB2 LUW 11.1](https://www.ibm.com/docs/en/db2/11.1?topic=statements-merge)

## Firebird

Firebird 2.1-2.5 supports all features from `SQL:2003` standard.
Firebird 3.0 supports all features from `SQL:2008` standard.

Limitations:

* update of fields, used in match condition could lead to unexpected results in Firebird 2.5
* very small double values in client-side source could fail
* BLOB and TIMESTAMP mapped to TimeSpan will not work with client-side source if null values mixed with non-null values

Links:

* [Firebird 2.5 MERGE INTO](https://www.firebirdsql.org/file/documentation/html/en/refdocs/fblangref25/firebird-25-language-reference.html#fblangref25-dml-merge)
* [Firebird 3.0 MERGE INTO](https://firebirdsql.org/file/documentation/chunk/en/refdocs/fblangref30/fblangref30-dml-merge.html)
* [Firebird 4.0 MERGE INTO](https://firebirdsql.org/file/documentation/chunk/en/refdocs/fblangref40/fblangref40-dml-merge.html)

## Oracle Database

Oracle supports `SQL:2003` features and operation conditions from `SQL:2008`.

Instead of independent `Delete` operation it supports delete condition for `Update` operation, which will be applied only to updated records and work with updated values.
To support this behavior, merge API supports `Update Then Delete` operation, that works only for Oracle. You also can use regular `Update` operation, but not `Delete`. For `Delete` operation you can use `UpdateWithDelete' with the same condition for update and delete.

Limitations:

* Only two operations per command supported, where one of them should be `Insert` and second should be `Update` or `UpdateWithDelete`
* `Delete` operation not supported
* Associations in `Insert' setters not supported
* fields, used in match condition, cannot be updated
* command with empty enumerable source will not send command to database and return 0 immediately
* mixing nulls and non-null values for binary column for client-side source doesn't work

Links:

* [MERGE INTO](https://docs.oracle.com/en/database/oracle/oracle-database/12.2/sqlrf/MERGE.html)

## Sybase/SAP ASE

ASE supports all features from `SQL:2008` standard

Limitations:

* it is hard to name it just a limitation * server could crash on some merge queries
* associations in match condition not supported (undocumented)
* returned number of affected records could be (and usually is) more than expected
* Merge only with `Delete` operations doesn't work (undocumented)
* Some combinations of operations rise error with text that doesn't make any sense (undocumented): "`MERGE is not allowed because different MERGE actions are referenced in the same WHEN [NOT] MATCHED clause`", which is not true, because other commands with same set of operations just work
* command with empty enumerable source will not send command to database and return 0 immediately

Other notes:

* identity insert enabled for insert operation

Links:

* [MERGE INTO ASE 15.7](https://infocenter.sybase.com/help/index.jsp?topic=/com.sybase.infocenter.dc36272.1570/html/commands/commands84.htm)
* [MERGE INTO ASE 16](https://help.sap.com/viewer/4c45f8d627434bb19e10dd0abbb757b0/16.0.0.0/en-US/ab389f37bc2b10149bb5c3bafec694a1.html)

## IBM Informix

Informix supports all features from `SQL:2003` standard and `Delete` operation from `SQL:2008`.

Limitations:

* associations not supported
* BYTE type (C# byte[] binary type) in client-side source leads to unexpected results for unknown reason

Other notes:

* for enumerable source it could be required to specify database types on columns that contain `null` values if provider cannot infer them properly

Links:

* [MERGE INTO](https://www.ibm.com/docs/en/informix-servers/12.10?topic=statements-merge-statement)

## SAP HANA 2

SAP HANA 2 supports all features from `SQL:2003` standard.

Limitations:

* `Update` operation must be first if both `Update` and `Insert` operations used in command
* associations in `Insert` operation not supported
* command with empty enumerable source will not send command to database and return 0 immediately

Links:

* [MERGE INTO](https://help.sap.com/docs/SAP_HANA_PLATFORM/4fe29514fd584807ac9f2a04f6754767/3226201f95764a57810dd256c9524d56.html?version=2.0.00)

## PostgreSQL

PostgreSQL supports all features from `SQL:2008` standard starting from version 15.

Limitations:

* nothing substantial

Links:

* [MERGE INTO](https://www.postgresql.org/docs/current/sql-merge.html)
