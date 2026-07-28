# Linq to DB extensions for NHibernate<!-- omit in toc -->

`linq2db.NHibernate` lets you run [Linq to DB](https://github.com/linq2db/linq2db) (linq2db) queries and
commands against an existing NHibernate `ISession` — reusing NHibernate's mapping metadata, its open
connection, and its transaction. Keep NHibernate for what it does well (identity map, change tracking,
entity lifecycle) and reach for linq2db when you need set-based SQL that NHibernate's LINQ provider can't
express.

- [Features](#features)
- [What is not supported](#what-is-not-supported)
- [How to use](#how-to-use)
- [Why use it?](#why-use-it)
- [Supported databases](#supported-databases)
- [Help! It doesn't work](#help-it-doesnt-work)

## Features

Over your existing NHibernate-mapped entities, linq2db adds:

- Set-based `UPDATE` / `DELETE` — no per-row loading, no round-trips
- Upserts (`InsertOrUpdate`) and `INSERT … SELECT` (server-side copy)
- [Window / analytic functions](https://linq2db.github.io/articles/sql/Window-Functions-(Analytic-Functions).html)
- [Recursive CTEs](https://linq2db.github.io/articles/sql/CTE.html)
- [MERGE](https://linq2db.github.io/articles/sql/merge/Merge-API-Description.html)
- Fast [BulkCopy](https://linq2db.github.io/articles/sql/Bulk-Copy.html) of millions of rows
- Arbitrary joins across entities that have no mapped association
- Table hints, temporary tables, cross-database queries, and a large ANSI-SQL surface

…while carrying your NHibernate context across:

- **Change tracking** — entities a linq2db query materializes join the session's first-level cache, so later
  edits persist on flush (on by default; see [`AsReadOnly()`](#read-only-queries) to opt out)
- **Session filters** — NHibernate `<filter>`s enabled on the session are applied to linq2db queries
- **Transactions** — linq2db commands run inside the session's active NHibernate transaction
- **Stateless sessions** — query through `IStatelessSession` too
- **Custom types** — single-column `IUserType` conversions are applied to linq2db queries as well
- **Inheritance** — a table-per-hierarchy subclass is restricted to its own discriminator values, so a linq2db
  query for it never returns its siblings' rows; table-per-concrete-class subclasses read from their own table
- **Components** — a `<component>`'s properties are mapped as columns of the owning entity, so they can be
  selected and filtered on like any other

## What is not supported

Read this first — it is the shortest way to know whether your mappings fit.

linq2db reads each entity from **one table**, taking the column and association metadata from NHibernate. A
mapping that cannot be expressed that way is handled in one of two ways:

- **Rejected with an explanation** — when going ahead would quietly lose data (a column that would simply be
  missing from the query and read as `null`). The exception names the type, the member and the reason.
- **Left unmapped** — when the member is not data of its own (an association). The rest of the entity keeps
  working, and using that member fails with linq2db's own *"The LINQ expression … could not be converted to
  SQL"*.

### Inheritance

| Mapping | |
|---|---|
| Table-per-hierarchy (discriminator) | Supported. A subclass is restricted to its own discriminator values, so it never returns a sibling's rows |
| Table-per-concrete-class (`<union-subclass>`) — concrete subclasses | Supported. Each reads from its own table |
| Table-per-concrete-class — the **root** | Not queryable: NHibernate reads it as a union over the subclass tables and it has no table of its own. Query a concrete subclass |
| Table-per-subclass (`<joined-subclass>`) | **Rejected.** Its columns are split across the base and subclass tables. Query its base class instead |

### Custom types (`IUserType`)

Applied when the user type maps to a **single column**. Otherwise:

- a **multi-column** user type (including any `ICompositeUserType`) is **rejected** — it has no single value to
  convert. Register a linq2db converter for it yourself with
  `LinqToDBForNHibernateTools.AddMappingSchema(sessionFactory, mappingSchema)`;
- a user type that inspects the session, or casts the reader/command to a provider-specific type, is not
  supported.

### Components

A `<component>`'s properties are mapped as columns of the owning entity. A sub-property that is an association,
or that spans several columns (a nested component), is **rejected**.

### Associations

Navigable when the foreign key is mapped as a scalar property on the referencing side:

- **many-to-one** — the source must map the foreign-key column as a property (it may be named differently from
  the target's key property). A reference mapped only as the navigation is not navigable;
- **one-to-many** — the child must map the foreign-key column as a property. A unidirectional collection whose
  child exposes no such property is not navigable;
- **many-to-many** — supported whether or not the junction table is mapped as an entity of its own.

### Everything else

- `ToLinqToDB()` on a native query from an `IStatelessSession` — use `statelessSession.GetTable<T>()` instead.
- Session filter conditions resolve unqualified columns against a single table, so they may not carry correctly
  into join queries; per-entity `<filter>` overrides fall back to the filter's default condition.
- One integration per process: this package and `linq2db.EntityFrameworkCore` both install process-wide query
  hooks, so they cannot be used in the same process.

## How to use

No setup call is required — the integration initializes itself on first use.

### Query over a session

`session.GetTable<T>()` returns a linq2db `ITable<T>` that builds and runs SQL over the session's connection:

```cs
using LinqToDB;
using LinqToDB.NHibernate;

var uk = session.GetTable<Customer>()
    .Where(c => c.Country == "UK")
    .OrderBy(c => c.CompanyName)
    .ToList();
```

Or take a linq2db data context explicitly:

```cs
using var db = session.CreateLinqToDbConnection();
var names = db.GetTable<Customer>().Select(c => c.CompanyName).ToList();
```

### Route a native NHibernate query through linq2db

Call `ToLinqToDB()` on a native `session.Query<T>()` to continue with linq2db extensions:

```cs
var names = session.Query<Customer>()
    .Where(c => c.Country == "UK")
    .ToLinqToDB()                 // hand off to linq2db
    .Select(c => c.CompanyName)
    .ToList();
```

### Set-based DML

```cs
// bulk UPDATE — no entities loaded
session.GetTable<Customer>()
    .Where(c => c.Country == "UK")
    .Set(c => c.City, "London")
    .Update();

// bulk DELETE
session.GetTable<Customer>().Where(c => c.IsObsolete).Delete();

// upsert
session.GetTable<Customer>().InsertOrUpdate(
    () => new Customer { CustomerId = "ACME", CompanyName = "Acme" },
    c  => new Customer { CompanyName = "Acme (updated)" });

// INSERT … SELECT — a server-side copy, no rows pulled to the client
session.GetTable<Customer>()
    .Where(c => c.Country == "UK")
    .Insert(session.GetTable<Customer>(), c => new Customer
    {
        CustomerId  = "C" + c.CustomerId,
        CompanyName = c.CompanyName,
        Country     = "Copy",
    });
```

### Window functions and recursive CTEs

```cs
// ROW_NUMBER() OVER (ORDER BY CustomerId)
var ranked = session.GetTable<Customer>()
    .Select(c => new { c.CustomerId, Rn = Sql.Window.RowNumber(f => f.OrderBy(c.CustomerId)) })
    .OrderBy(x => x.Rn)
    .ToList();

// recursive CTE walking a self-referencing tree
using var db = session.CreateLinqToDbContext();
var tree = db.GetCte<OrgUnit>(self =>
    db.GetTable<OrgUnit>().Where(o => o.ParentId == null)
        .Concat(
            from o   in db.GetTable<OrgUnit>()
            from par in self.InnerJoin(par => par.Id == o.ParentId)
            select o));
```

### Read-only queries

Query results are attached to the session's change tracker by default. Mark a query `AsReadOnly()` to leave
its entities detached:

```cs
// tracked — the entity joins the session
var tracked = session.GetTable<Customer>().First(c => c.CustomerId == "ACME");

// not tracked
var readOnly = session.GetTable<Customer>().AsReadOnly().First(c => c.CustomerId == "ACME");
```

Tracking can also be turned off globally with `LinqToDBForNHibernateTools.EnableChangeTracker = false;`.

### Configuring linq2db

Register options once against the session factory and they apply to every linq2db context created for its
sessions — interceptors, extra mapping schemas, or any other linq2db option:

```cs
LinqToDBForNHibernateTools.AddOptions(sessionFactory, o => o
    .UseInterceptor(new MyCommandInterceptor())
    .UseMappingSchema(myMappings));
```

### Stateless sessions

```cs
using var stateless = sessionFactory.OpenStatelessSession();
var customers = stateless.GetTable<Customer>().Where(c => c.Country == "UK").ToList();
```

### NHibernate filters

Filters enabled on the session are honored by linq2db queries:

```cs
session.EnableFilter("softDelete");
var visible = session.GetTable<Document>().ToList();   // soft-deleted rows excluded
```

### Async

linq2db's async methods carry a `LinqToDB` suffix to avoid colliding with NHibernate's own async LINQ
extensions; the `…NH` variants run through NHibernate:

```cs
var a = await session.Query<Customer>().Where(c => c.Country == "UK").ToListAsyncLinqToDB();
var b = await session.Query<Customer>().Where(c => c.Country == "UK").ToListAsyncNH();
```

## Why use it?

- Use advanced, set-based SQL — bulk `UPDATE`/`DELETE`, `MERGE`, upserts, window functions, CTEs, `BulkCopy` —
  while keeping NHibernate's identity map and change tracking for the rest of your application.
- Adopt linq2db incrementally, one query at a time, without rewriting your NHibernate mappings.

## Supported databases

Verified against SQL Server, PostgreSQL, MySQL / MariaDB, Oracle, Firebird, and SQLite. Any database
supported by both linq2db and your NHibernate dialect should work.

## Help! It doesn't work!

If you hit an issue, please check the [existing issues](https://github.com/linq2db/linq2db/issues) and, if
it's new, open one.
