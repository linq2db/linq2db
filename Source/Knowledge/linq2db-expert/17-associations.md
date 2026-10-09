<!-- Generated from: Source/Skills/linq2db/docs/associations.md -->

# Associations And Eager Loading

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](01-skill.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - define relationships with `[Association]` or fluent `.Association(...)`
> - query through association members
> - load associated entities with `LoadWith` / `ThenLoad`
> - choose eager-loading strategy markers
> - avoid treating associations as lazy-loaded navigation properties

Associations are reusable relationship metadata. They help LinqToDB translate relationship access
inside queries and can be used by explicit eager loading. They do not create lazy-loaded navigation
properties and they do not automatically populate members on materialized records.

---

## Required namespaces

```csharp
using LinqToDB;
using LinqToDB.Mapping;
```

Async materialization still requires:

```csharp
using LinqToDB.Async;
```

---

## Choosing the API

| Need | Use |
|---|---|
| Relationship metadata on entity classes | `[Association]` |
| Relationship metadata in centralized fluent mapping | `FluentMappingBuilder().Entity<T>().Association(...)` |
| Relationship metadata from a property mapping chain | `.Property(...).Association(...)` |
| One query with an ad-hoc relationship | explicit LINQ `join` / `GroupJoin` |
| Load related objects into materialized entities | `LoadWith` / `ThenLoad` |
| Guard against accidental collection loading in projections | `UseImplicitCollectionLoading(ImplicitCollectionLoading.Throw)` |
| Tune eager-loading query shape | `WithKeyedLoadStrategy`, `WithSeparateLoadStrategy`, `WithUnionLoadStrategy`, or `UseDefaultEagerLoadingStrategy(...)` |

Use associations when relationship metadata is reused across queries or eager-loading paths. Use
explicit joins when the relationship is local to one query and the join shape should be visible.

---

## Association metadata is not lazy loading

Wrong:

```csharp
var orders = await db.GetTable<Order>().ToListAsync();

foreach (var order in orders)
{
    // wrong assumption: association members are not lazy-loaded after materialization
    Console.WriteLine(order.Customer.Name);
}
```

Correct:

```csharp
var orders = await db.GetTable<Order>()
    .LoadWith(o => o.Customer)
    .ToListAsync();
```

If you only need related values in a projection, a normal query expression is often clearer:

```csharp
var rows = await
    (from o in db.GetTable<Order>()
     select new
     {
         o.Id,
         CustomerName = o.Customer.Name
     })
    .ToListAsync();
```

Association access in the query expression is translated into SQL. Association members on
materialized entities are populated only when explicitly loaded.

---

## Attribute mapping

Use `ThisKey` for member names on the current entity and `OtherKey` for member names on the target
entity. Both values are comma-separated when the relationship has a composite key.

```csharp
[Table("Orders")]
public sealed class Order
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column]
    public int CustomerId { get; set; }

    [Association(ThisKey = nameof(CustomerId), OtherKey = nameof(Customer.Id), CanBeNull = false)]
    public Customer Customer { get; set; } = null!;
}

[Table("Customers")]
public sealed class Customer
{
    [PrimaryKey]
    public int Id { get; set; }

    [Association(ThisKey = nameof(Id), OtherKey = nameof(Order.CustomerId))]
    public IEnumerable<Order> Orders { get; set; } = null!;
}
```

For a reference association, `CanBeNull = false` generates an inner join and `CanBeNull = true`
generates an outer join. When nullable reference type metadata is enabled, LinqToDB can derive the
default for reference associations from nullability; explicit `CanBeNull` still wins. For
collection associations the default is effectively nullable/outer-load friendly.

---

## Fluent mapping

Use fluent associations when mapping should stay outside entity classes.

```csharp
static MappingSchema BuildMapping()
{
    return new FluentMappingBuilder()
        .Entity<Order>()
            .HasTableName("Orders")
            .HasPrimaryKey(o => o.Id)
            .Association(o => o.Customer, o => o.CustomerId, c => c.Id, canBeNull: false)
        .Entity<Customer>()
            .HasTableName("Customers")
            .HasPrimaryKey(c => c.Id)
            .Association(c => c.Orders, c => c.Id, o => o.CustomerId)
        .Build()
        .MappingSchema;
}
```

Create and reuse a custom `MappingSchema`; do not rebuild it per operation. See
[`mapping.md`](08-mapping.md) for mapping schema lifetime rules.

---

## Predicate and query-expression associations

Key-based associations are the normal case. Use predicate or query-expression associations only
when key metadata cannot describe the relationship.

Predicate association:

```csharp
public sealed class Order
{
    [Column]
    public int CustomerId { get; set; }

    [Association(ExpressionPredicate = nameof(CustomerPredicate), CanBeNull = false)]
    public Customer Customer { get; set; } = null!;

    public static Expression<Func<Order, Customer, bool>> CustomerPredicate()
    {
        return (order, customer) => order.CustomerId == customer.Id && customer.IsActive;
    }
}
```

Query-expression association:

```csharp
public sealed class Customer
{
    [PrimaryKey]
    public int Id { get; set; }

    [Association(QueryExpressionMethod = nameof(RecentOrdersQuery))]
    public IEnumerable<Order> RecentOrders { get; set; } = null!;

    public static Expression<Func<Customer, IDataContext, IQueryable<Order>>> RecentOrdersQuery()
    {
        var recentFrom = DateTime.Today.AddDays(-30);

        return (customer, db) => db.GetTable<Order>()
            .Where(o => o.CustomerId == customer.Id && o.CreatedAt >= recentFrom);
    }
}
```

When `QueryExpression` / `QueryExpressionMethod` is used, key properties such as `ThisKey` and
`OtherKey` are ignored for that association. Keep query-expression associations small and
translatable; the expression becomes part of LinqToDB query translation.

---

## Eager loading with `LoadWith`

`LoadWith` marks associations that should be loaded for each materialized root record. This is a
deferred, composable query directive; it takes effect when the query is executed.

```csharp
var orders = await db.GetTable<Order>()
    .Where(o => o.CustomerId == customerId)
    .LoadWith(o => o.Customer)
    .LoadWith(o => o.Items)
    .ToListAsync();
```

`LoadWith` can require multiple SQL queries to load all requested associations.

Use the overload with `loadFunc` to filter or shape the association load query:

```csharp
var orders = await db.GetTable<Order>()
    .LoadWith(o => o.Items,
        q => q.Where(i => !i.IsCancelled).OrderBy(i => i.SortOrder))
    .ToListAsync();
```

When the underlying source is not a LinqToDB query, `LoadWith` is a passthrough and the eager-load
directive is ignored.

---

## Nested eager loading with `ThenLoad`

Use `ThenLoad` after `LoadWith` to load the next association level.

```csharp
var orders = await db.GetTable<Order>()
    .LoadWith(o => o.Items)
        .ThenLoad(i => i.Product)
            .ThenLoad(p => p.Category)
    .LoadWith(o => o.Customer)
    .ToListAsync();
```

The selector passed to `ThenLoad` receives the previously loaded association type. After a
collection association, the selector receives the collection element type.

Inline dot-path selectors are also supported:

```csharp
var orders = await db.GetTable<Order>()
    .LoadWith(o => o.Items[0].Product.Category)
    .ToListAsync();
```

The indexer or `First()` call used to step through a collection in the selector does not limit the
loaded rows. It only describes the association path. Use `loadFunc` if you need to filter the
loaded collection.

---

## Storage and custom association setters

`AssociationAttribute.Storage` names the property or field that receives the loaded association
value. When `Storage` is not specified, LinqToDB uses the association member itself.

Use `Storage` when the public association member should expose a calculated or wrapped value but
`LoadWith` should assign to a backing member.

For more advanced assignment, `AssociationSetterExpression` or `AssociationSetterExpressionMethod`
can provide a setter expression. Verify exact signatures in `docs/api.md` / XML-doc before using
custom setters; most application mappings do not need them.

---

## Eager-loading strategies

Eager-loading strategy controls how LinqToDB executes collection eager-loading preamble queries.

Per-query markers:

```csharp
var orders = await db.GetTable<Order>()
    .LoadWith(o => o.Items)
    .WithKeyedLoadStrategy()
    .ToListAsync();
```

Available markers:

| Marker | Meaning |
|---|---|
| `WithSeparateLoadStrategy()` | Dedicated pre-query per child association; equivalent to `EagerLoadingStrategy.Default`. |
| `WithKeyedLoadStrategy()` | Buffer root results, extract distinct parent keys, and load child rows with key-based batch query. Useful when parent entities are wide. |
| `WithUnionLoadStrategy()` | Combine multiple child association preamble queries with `CTE + UNION ALL` when possible; falls back when provider/query shape cannot support it. |

Per-query markers apply to the root query and propagate to contained child collections. They
override `LinqOptions.DefaultEagerLoadingStrategy`. If more than one marker is applied to the same
query, the outermost/last-applied marker wins.

Global default:

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery);
```

Use global defaults only when the choice is appropriate for the application. For one query, prefer
the per-query marker because it keeps the behavior local.

---

## Guarding implicit collection loading

`ImplicitCollectionLoading.Throw` rejects a query that projects a collection association without an
explicit eager-load request.

```csharp
var options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseImplicitCollectionLoading(ImplicitCollectionLoading.Throw);
```

With this option, make collection loading explicit:

```csharp
var customers = await db.GetTable<Customer>()
    .LoadWith(c => c.Orders)
    .ToListAsync();
```

The guard is bypassed for the explicitly requested collection only. A root
`WithUnionLoadStrategy`, `WithKeyedLoadStrategy`, or `WithSeparateLoadStrategy` marker opts the
whole query into eager loading.

---

## Common mistakes

| Mistake | Correct route |
|---|---|
| Expecting association members to lazy-load after `ToList()` | Use `LoadWith` / `ThenLoad`, or project related values inside the query |
| Using `LoadWith` when only a few related scalar values are needed | Use a projection; do not materialize full associated entities unnecessarily |
| Omitting `CanBeNull` on a required reference and getting outer-join semantics | Set `CanBeNull = false` in `[Association]` or fluent mapping |
| Using `Items[0]` or `First()` in a `LoadWith` path expecting it to load one row | Treat it only as path syntax; use `loadFunc` to filter rows |
| Returning an in-memory `IQueryable` and expecting `LoadWith` to work | `LoadWith` is ignored for non-LinqToDB query sources |
| Rebuilding fluent association mapping per request | Build and reuse `MappingSchema` |
| Inventing EF Core-style `Include` / lazy-loading behavior | Use LinqToDB `LoadWith` / `ThenLoad` and explicit query composition |

---

## API lookup anchors

When exact signatures are needed, search [`api.md`](04-api-discovery-and-extract.md) and then XML-doc for:

- `AssociationAttribute`
- `EntityMappingBuilder.Association`
- `PropertyMappingBuilder.Association`
- `LoadWith`
- `ThenLoad`
- `LoadWithAsTable`
- `ILoadWithQueryable`
- `EagerLoadingStrategy`
- `WithKeyedLoadStrategy`
- `WithSeparateLoadStrategy`
- `WithUnionLoadStrategy`
- `ImplicitCollectionLoading`
- `UseImplicitCollectionLoading`
- `UseDefaultEagerLoadingStrategy`
