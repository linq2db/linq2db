# Linq to DB extensions for NHibernate<!-- omit in toc -->

`linq2db.NHibernate` lets you run [Linq to DB](https://github.com/linq2db/linq2db) (linq2db) queries and
commands against an existing NHibernate `ISession` — reusing NHibernate's mapping metadata, its open
connection, and its transaction. Keep NHibernate for what it does well (identity map, change tracking,
entity lifecycle) and reach for linq2db when you need set-based SQL that NHibernate's LINQ provider can't
express.

- [Features](#features)
- [How to use](#how-to-use)
- [Why use it?](#why-use-it)
- [Supported databases](#supported-databases)
- [Known limitations](#known-limitations)
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

## Known limitations

- `ToLinqToDB()` on a native query from an `IStatelessSession` is not supported — use
  `statelessSession.GetTable<T>()` instead.
- NHibernate value conversions (`IUserType`) are not translated to linq2db value converters.
- Session filter conditions resolve unqualified columns against a single table, so they may not carry
  correctly into join queries; per-entity `<filter>` overrides fall back to the filter's default condition.
- Associations are exposed to linq2db only when the foreign key is mapped as a scalar property on the
  referencing side:
  - **many-to-one** — the source entity must map the foreign-key column as a property (it may be named
    differently from the target's key property); a reference mapped only as the navigation is not navigable
    from a linq2db query;
  - **one-to-many** — the child entity must map the foreign-key column as a property; a unidirectional
    collection whose child exposes no such property is not navigable from a linq2db query;
  - **many-to-many** — the junction table must be mapped as its own entity so linq2db can query through it.

## Help! It doesn't work!

If you hit an issue, please check the [existing issues](https://github.com/linq2db/linq2db/issues) and, if
it's new, open one.
