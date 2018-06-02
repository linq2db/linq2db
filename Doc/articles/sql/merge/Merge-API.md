---
uid: Merge
---
# Merge API

This API available since linq2db 1.9.0. It superseeds previous version of API with very limited functionality. For migration from old API check link below.

## Supported Databases

- [Microsoft SQL Server](xref:Merge-API-Background#microsoft-sql-server-2008)
- [IBM DB2](xref:Merge-API-Background#ibm-db2)
- [Firebird](xref:Merge-API-Background#firebird)
- [Oracle Database](xref:Merge-API-Background#oracle-database)
- [Sybase/SAP ASE](xref:Merge-API-Background#sybasesap-ase)
- [IBM Informix](xref:Merge-API-Background#ibm-informix)
- [SAP HANA 2](xref:Merge-API-Background#sap-hana-2)

## Related Pages

- [Background](xref:Merge-API-Background)
- [API Description](xref:Merge-API-Description)
- [Migration from old API guide](xref:Merge-API-Migration)

## Introduction

Merge is an atomic operation to update table (target) content using other table (source). Merge API provides methods to build Merge command and execute it. Command could have following elements (availability depends on database engine, see [[support table|Merge-API-:-Background-Information-and-Providers-Support]] for more details):

- target. Required element. Could be a table or updateable view
- source. Required element. Could be a table, query or client-side collection
- match/on rule. Optional element. Defines rule to match target and source records. By default we match target and source by primary key columns
- ordered list of operations to perform for each match. At least one operation required
- operation condition. Optional element. Specify additional condition for operation execution.

## Merge Operations

Merge operations could be splitted into three groups:

- Matched operations. Operations, executed for records, present in *both* target and source according to match rule.
- Not matched operations. Operations, executed for records, present only in *source* according to match rule.
- Not matched by source. Operations, executed for records, present only in *target* according to match rule.

Each group of operations work with their own set of source and target records and could contain more than one operation. In this case each operation must have operation condition except last one, which could omit it and be applied to all remaining records. Operations within group must be ordered properly.

### Example

You want to do following: update status of all orders in `AwaitingConfirmation` status to `Confirmed` and delete all orders with `amount` equal to `0`. Your merge operation will look like:

```cs
db.Orders

    // start merge command
    .Merge()

    // use the same table for source
    .UsingTarget()

    // match on primary key columns
    .OnTargetKey()

    // first delete all records with 0 amount
    // we also can use source in condition because they reference the same record in our case
    .DeleteWhenMatchedAnd((target, source) => target.amount == 0)

    // for records, not handled by previous command, update records in AwaitingConfirmation status
    .UpdateWhenMatchedAnd(
        (target, source) => target.status == Status.AwaitingConfirmation,
        (target, source) => new Order() { status = Status.Confirmed })

    // send merge command to database
    .Merge();
```

In example above, `delete` and `update` operations belong to the same match group so their order is important. If you will put `Update` before `Delete` your merge command will do something else: it will update all orders in AwaitingConfirmation status and for **remaining** orders will remove those with 0 amount. After merge execution you could receive confirmed orders with 0 amount in `Orders` table.

### Matched operations

Because those operations executed for records, present in both target and source, they have access to both records. There are two operations in this group (plus one non-standard operation for Oracle):

- `Update` operation. This operation allows to update target record fields.
- `Delete` operation. This operation allows to delete target record.
- `Update Then Delete` operation. This is Oracle-only operation, which updates target record and then delete **updated** records (usually using delete predicate).

### Not matched operations

Those operations executed for records, present only in source table, so they could access only target table properties.
This group contains only one operation - `Insert` operation, which adds new record to target table.

### Not matched by source operations

This is SQL Server-only extension, that allows to perform operations for records, present only in target table. This group contains same operations as Matched group with one distinction - operations could access only target record:

- `Update By Source` operation. Allows to update target table record.
- `Delete By Source` operation. Allows to delete target table record.
