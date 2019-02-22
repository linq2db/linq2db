---
uid: CTE
---
# Common Table Expression (CTE)

Common Table Expression (CTE) support introduced for supporting advanced SQL techniques in `LINQ To DB`.
See documentation for Transact SQL: [WITH common_table_expression](https://docs.microsoft.com/en-us/sql/t-sql/queries/with-common-table-expression-transact-sql?view=sql-server-2017)

## When it is useful

* Reusing same SQL part in complex query
* Recursive table processing

## Defining simple CTE

CTE in `LINQ To DB` is also `IQueryable`. Any `IQueryable` can be converted to CTE by extension method `AsCte("optional_name")`.

```cs
var employeeSubordinatesReport  =
   from e in db.Employee
   select new
   {
      e.EmployeeID,
      e.LastName,
      e.FirstName,
      NumberOfSubordinates = db.Employee
          .Where(e2 => e2.ReportsTo == e.ReportsTo)
          .Count(),
      e.ReportsTo
   };

var employeeSubordinatesReportCte = employeeSubordinatesReport
                                     .AsCte("EmployeeSubordinatesReport");
```

Variable `employeeSubordinatesReportCte` can be reused in other parts of linq query.

```cs
var result =
   from employee in employeeSubordinatesReportCte
   from manager in employeeSubordinatesReportCte
                      .LeftJoin(manager => employee.ReportsTo == manager.EmployeeID)
   select new
   {
      employee.LastName,
      employee.FirstName,
      employee.NumberOfSubordinates,
      ManagerLastName = manager.LastName,
      ManagerFirstName = manager.FirstName,
      ManagerNumberOfSubordinates = manager.NumberOfSubordinates
   };
```

You are not limited in defining as many CTEs as you need and they can reference each other. `LINQ To DB` will put them in correct order and generate SQL with one limitation - **there should be no circular references between CTEs**.

```sql
WITH [EmployeeSubordinatesReport]
(
   [ReportsTo],
   [EmployeeID],
   [LastName],
   [FirstName],
   [NumberOfSubordinates]
)
AS
(
   SELECT
      [t2].[ReportsTo],
      [t2].[EmployeeID],
      [t2].[LastName],
      [t2].[FirstName],
      (
         SELECT
            Count(*)
         FROM
            [Employees] [t1]
         WHERE
            [t1].[ReportsTo] IS NULL AND [t2].[ReportsTo] IS NULL OR
            [t1].[ReportsTo] = [t2].[ReportsTo]
      ) as [c1]
   FROM
      [Employees] [t2]
)

SELECT
   [t3].[LastName] as [LastName1],
   [t3].[FirstName] as [FirstName1],
   [t3].[NumberOfSubordinates],
   [manager].[LastName] as [LastName2],
   [manager].[FirstName] as [FirstName2],
   [manager].[NumberOfSubordinates] as [NumberOfSubordinates1]
FROM
   [EmployeeSubordinatesReport] [t3]
      LEFT JOIN [EmployeeSubordinatesReport] [manager]
           ON [t3].[ReportsTo] = [manager].[EmployeeID]
```

## Defining recursive CTE

> Recursive CTEs are special in the sense they are allowed to reference themselves! Because of this special ability, you can use recursive CTEs to solve problems other queries cannot. Recursive CTEs are really good at working with hierarchical data such as org charts for bill of materials. [Recursive CTEs Explained](https://www.essentialsql.com/recursive-ctes-explained/)

CTEs have limitations that are not handled by `LINQ To DB`, so you have to be aware of them before start of usage - [Guidelines for Defining and Using Recursive Common Table Expressions](https://docs.microsoft.com/en-us/sql/t-sql/queries/with-common-table-expression-transact-sql?view=sql-server-2017#guidelines-for-defining-and-using-recursive-common-table-expressions)

Since in C# language we can not use variable's reference in it's own initialization part, we have created function that helps in defining such queries `GetCte<TCteProjection>(cte => ...)`. `TCteProjection` is required generic parameter that is needed for resolving type of lambda parameter.
The following example shows how to define CTE for calculation of employee hierarchy level

```cs
// defining class for representing Recursive CTE
class EmployeeHierarchyCTE
{
   public int EmployeeID;
   public string LastName;
   public string FirstName;
   public int? ReportsTo;
   public int HierarchyLevel;
}

using (var db = new NorthwindDB(context))
{
   var employeeHierarchyCte = db.GetCte<EmployeeHierarchyCTE>(employeeHierarchy =>
   {
      return
         (
            from e in db.Employee
            where e.ReportsTo == null
            select new EmployeeHierarchyCTE
            {
               EmployeeID = e.EmployeeID,
               LastName = e.LastName,
               FirstName = e.FirstName,
               ReportsTo = e.ReportsTo,
               HierarchyLevel = 1
            }
         )
         .Concat
         (
            from e in db.Employee
            from eh in employeeHierarchy
                        .InnerJoin(eh => e.ReportsTo == eh.EmployeeID)
            select new EmployeeHierarchyCTE
            {
               EmployeeID = e.EmployeeID,
               LastName = e.LastName,
               FirstName = e.FirstName,
               ReportsTo = e.ReportsTo,
               HierarchyLevel = eh.HierarchyLevel + 1
            }
         );
   });

   var result =
      from eh in employeeHierarchyCte
      orderby eh.HierarchyLevel, eh.LastName, eh.FirstName
      select eh;

   var data = result.ToArray();
}
```

Resulting SQL:

```sql
WITH [employeeHierarchy]
(
   [EmployeeID],
   [LastName],
   [FirstName],
   [ReportsTo],
   [HierarchyLevel]
)
AS
(
   SELECT
      [t1].[EmployeeID],
      [t1].[LastName],
      [t1].[FirstName],
      [t1].[ReportsTo],
      1 as [c1]
   FROM
      [Employees] [t1]
   WHERE
      [t1].[ReportsTo] IS NULL
   UNION ALL
   SELECT
      [t2].[EmployeeID],
      [t2].[LastName],
      [t2].[FirstName],
      [t2].[ReportsTo],
      [eh].[HierarchyLevel] + 1 as [c1]
   FROM
      [Employees] [t2]
         INNER JOIN [employeeHierarchy] [eh] ON [t2].[ReportsTo] = [eh].[EmployeeID]
)

SELECT
   [t3].[EmployeeID] as [EmployeeID2],
   [t3].[LastName] as [LastName2],
   [t3].[FirstName] as [FirstName2],
   [t3].[ReportsTo] as [ReportsTo2],
   [t3].[HierarchyLevel]
FROM
   [employeeHierarchy] [t3]
ORDER BY
   [t3].[HierarchyLevel],
   [t3].[LastName],
   [t3].[FirstName]
```

## Database engines that support CTE

|Database Engine| Minimal version|
|----------|----------|
|[Firebird](https://firebirdsql.org/refdocs/langrefupd21-select.html#langrefupd21-select-cte)|2.1|
|[MS SQL](https://docs.microsoft.com/en-us/sql/t-sql/queries/with-common-table-expression-transact-sql?view=sql-server-2017)|2008|
|[MySQL](https://dev.mysql.com/doc/refman/8.0/en/with.html)|8.0.1|
|[Oracle](https://docs.oracle.com/database/121/SQLRF/statements_10002.htm#BABCGAAJ)|11g Release 2|
|[PostgreSQL](https://www.postgresql.org/docs/9.1/static/queries-with.html)|8.4|
|[SQLite](https://www.sqlite.org/lang_with.html)|3.8.3|
|[IBM DB2](https://www.ibm.com/support/knowledgecenter/en/SSEPEK_11.0.0/sqlref/src/tpc/db2z_sql_commontableexpression.html)| 8 |

## Known limitations

* Oracle and Firebird DML operations that use CTE is not completely implemented.
* _TBD_