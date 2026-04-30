# LinqToDB Mapping

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> **You are here if** you need to:
> - map CLR classes, records, or interfaces to database tables and columns
> - choose between convention-based mapping, attributes, and fluent mapping
> - configure `MappingSchema` or attach it to `DataOptions`
> - create tables from mapped types or temporary table types
> - reason about primary keys, identities, nullability, column names, lengths, precision, or scale

Mapping metadata tells LinqToDB how CLR types correspond to database objects.
It affects query translation, DML column lists, schema creation, temporary tables, and value conversion.

---

> **Agent guidance:**
> - Use convention-based mapping for simple read/query types when database names match CLR names.
> - Use explicit attributes or fluent mapping when table names, column names, keys, identities, nullability, or schema generation matter.
> - Do not create a `MappingSchema` unless you need fluent mapping, custom conversions, or additional metadata readers.
> - If a custom `MappingSchema` is needed, create it once at application startup and reuse it through `DataOptions.UseMappingSchema(...)`.
> - Do not create a new custom `MappingSchema` per `DataConnection`, per request, or per operation.
> - For any LinqToDB API or option that generates a `CREATE TABLE` statement, specify `Length`, `Precision`, and `Scale` for provider-sensitive columns (`string`, `decimal`, etc.).
> - If a length or precision value is an agent assumption, put a `TODO` comment on the same line as the mapping.

---

## Pattern quick-reference

| Scenario | Pattern |
|---|---|
| Database names match CLR names | Convention mapping |
| Need table/column names in the type | Mapping attributes |
| Cannot modify entity types | Fluent mapping with a shared custom `MappingSchema` |
| Need reusable custom mapping configuration | `MappingSchema` + `DataOptions.UseMappingSchema(...)` |
| Need DDL or temporary tables | Explicit column sizes and numeric precision |
| Need associations | `[Association]` or fluent `.Association(...)`; see future association guide and XML-doc |

---

## 1. Convention-based mapping

When no mapping attributes are applied, LinqToDB maps public scalar members by convention.

Default conventions:

| Concept | Convention |
|---|---|
| Table name | CLR type name; for interfaces, a leading `I` is stripped (`IProduct` -> `Product`) |
| Schema / database / server | Not specified |
| Column name | Member name |
| Included members | Public instance properties and fields whose CLR type is scalar |
| Nullability | Inferred from CLR nullability and mapping metadata |

Example:

```csharp
public sealed class Product
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = null!;
    public decimal Price    { get; set; }
    public bool    IsActive { get; set; }
}

using var db = new DataConnection(options);

var active = db.GetTable<Product>()
    .Where(p => p.IsActive)
    .ToList();
```

Use conventions only when they match the database schema and no DDL-sensitive metadata is needed.

---

## 2. Attribute mapping

Use attributes when mapping belongs with the entity type.

```csharp
using LinqToDB.Mapping;

[Table("Products")]
public sealed class Product
{
    [PrimaryKey, Identity]
    [Column("ProductID")]
    public int Id { get; set; }

    [Column(Length = 200), NotNull]
    public string Name { get; set; } = null!;

    [Column(Precision = 18, Scale = 2)]
    public decimal Price { get; set; }

    [Column]
    public bool IsActive { get; set; }
}
```

`[Table]` changes column inclusion behavior:

| Entity mapping | Column inclusion |
|---|---|
| No `[Table]` attribute | Public scalar members are included by convention |
| `[Table]` | Explicit mode: only `[Column]`, `[PrimaryKey]`, `[Identity]`, and `[ColumnAlias]` members are columns |
| `[Table(IsColumnAttributeRequired = false)]` | Keeps convention-based column inclusion while still specifying table metadata |

If you add `[Table]` only to set the table name and still expect public scalar members to be mapped,
set `IsColumnAttributeRequired = false`.

```csharp
[Table("Products", IsColumnAttributeRequired = false)]
public sealed class Product
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

## 3. Fluent mapping

Use fluent mapping when entity classes cannot carry LinqToDB attributes or when mapping is
environment-specific.

```csharp
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

public sealed class Category
{
    public int                  Id       { get; set; }
    public string               Name     { get; set; } = null!;
    public IEnumerable<Product> Products { get; set; } = null!;
}

public sealed class Product
{
    public int      Id         { get; set; }
    public int      CategoryId { get; set; }
    public string   Name       { get; set; } = null!;
    public decimal  Price      { get; set; }
    public bool     IsActive   { get; set; }
    public Category Category   { get; set; } = null!;
}

static readonly MappingSchema Mapping = BuildMapping();

static readonly DataOptions Options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMappingSchema(Mapping);

static MappingSchema BuildMapping()
{
    return new FluentMappingBuilder()
        .Entity<Category>()
            .HasTableName("Categories")
            .IsColumnRequired()
            .HasPrimaryKey(c => c.Id)
            .HasIdentity(c => c.Id)
            .Property(c => c.Id)
                .HasColumnName("CategoryID")
                .IsColumn()                         // optional as `Property(...)` maps the member as a column by default
            .Property(c => c.Name)
                .HasColumnName("Name")              // optional if the column name matches the member name
                .HasLength(100)
                .IsNotNull()
                .IsColumn()                         // optional as `Property(...)` maps the member as a column by default
            .Association(c => c.Products, c => c.Id, p => p.CategoryId)
        .Entity<Product>()
            .HasTableName("Products")
            .IsColumnRequired()
            .HasPrimaryKey(p => p.Id)
            .HasIdentity(p => p.Id)
            .Property(p => p.Id)
                .HasColumnName("ProductID")
                .IsColumn()                         // optional as `Property(...)` maps the member as a column by default
            .Property(p => p.CategoryId)
                .HasColumnName("CategoryID")
                .IsColumn()                         // optional as `Property(...)` maps the member as a column by default
            .Property(p => p.Name)
                .HasLength(200)
                .IsNotNull()
            .Property(p => p.Price)
                .HasPrecision(18)
                .HasScale(2)
            .Property(p => p.IsActive)
                .IsColumn()                         // optional as `Property(...)` maps the member as a column by default
            .Association(p => p.Category, p => p.CategoryId, c => c.Id, canBeNull: false)
        .Build()
        .MappingSchema;
}

using var db = new DataConnection(Options);
```

Fluent mapping is chain-oriented. `PropertyMappingBuilder` exposes `.Property(...)`,
`.Association(...)`, `.Entity(...)`, and `.Build()`, so a mapping can stay in one chain when that
is clearer. Split it into local variables when the mapping becomes too long to read.

`Build()` registers the fluent metadata with the `MappingSchema`.
If the builder is created inside a framework callback that calls `Build()` for you, follow that
framework's XML-doc or setup guide; otherwise call `Build()` explicitly.

---

## 4. MappingSchema lifetime

You do not need to create a `MappingSchema` for ordinary convention-based or attribute-based mapping.
Create a custom `MappingSchema` only when you need fluent mapping, custom conversions, or extra metadata.

When you do create one, remember that `MappingSchema` holds mapping metadata and internal expression caches.
Recreating it per operation bypasses those caches and can cause serious performance degradation.

Wrong:

```csharp
using var db = new DataConnection(
    new DataOptions()
        .UseSqlServer(connectionString)
        .UseMappingSchema(new MappingSchema())); // wrong: custom schema recreated per operation
```

Correct:

```csharp
static readonly MappingSchema Mapping = BuildMapping();

static readonly DataOptions Options = new DataOptions()
    .UseSqlServer(connectionString)
    .UseMappingSchema(Mapping);

using var db = new DataConnection(Options);
```

Use `.UseAdditionalMappingSchema(...)` only when you intentionally layer an extra schema on top
of a base configuration. Create every participating custom schema once.

---

## 5. DDL-sensitive column metadata

Mapping metadata is used not only for queries, but also for APIs that generate `CREATE TABLE`
statements:

- `CreateTable`
- `CreateTempTable`
- APIs that create a table because of `TableOptions` flags such as `CreateIfNotExists` or `IsTemporary`

For those APIs, provider-sensitive CLR types must be bounded explicitly.

```csharp
[Table("Products")]
public sealed class Product
{
    [PrimaryKey, Identity]
    public int Id { get; set; }

    [Column(Length = 200), NotNull]
    public string Name { get; set; } = null!; // TODO: Confirm max product name length. 200 is an AI agent assumption.

    [Column(Precision = 18, Scale = 2)]
    public decimal Price { get; set; }
}
```

Do not write an unconstrained schema entity like this when DDL is involved:

```csharp
[Column]
public string Name { get; set; } = null!; // wrong for schema creation: provider default length is implicit
```

If exact limits are not provided by the task, choose a bounded value guided by field semantics and
add a same-line `TODO` explaining that the value is an AI agent assumption.

---

## 6. Column and member selection

Use these tools to control which members are mapped:

| Need | Attribute | Fluent mapping |
|---|---|---|
| Include a member as a column | `[Column]` | `.Property(p => p.X).IsColumn()` |
| Exclude a member | `[NotColumn]` | `.Property(p => p.X).IsNotColumn()` or `.Ignore(p => p.X)` |
| Set column name | `[Column("db_name")]` | `.HasColumnName("db_name")` |
| Mark primary key | `[PrimaryKey]` | `.HasPrimaryKey(p => p.Id)` |
| Mark identity | `[Identity]` | `.HasIdentity(p => p.Id)` |
| Skip implicit insert/update | `[Column(SkipOnInsert = true)]` / `[Column(SkipOnUpdate = true)]` | `.HasSkipOnInsert()` / `.HasSkipOnUpdate()` |

`SkipOnInsert` and `SkipOnUpdate` affect implicit full-entity `Insert` / `Update` operations.
Explicit column assignments in fluent DML APIs still use the values the caller provides.

---

## 7. Associations are mapping metadata, not lazy loading

Associations describe relationships that LinqToDB can translate in queries.
They do not enable EF-style lazy loading, identity maps, change tracking, or `SaveChanges()`.

Use associations when relationship metadata should be reusable in query expressions or eager loading.
Use explicit joins when the relationship is local to one query or when the join shape is clearer.

`LoadWith` / `ThenLoadWith` are explicit eager-loading directives. They are not implicit navigation loading.

---

## 8. Common mistakes

| Mistake | Correct action |
|---|---|
| Creating `MappingSchema` when attributes or conventions are enough | Do not create a custom schema |
| Creating custom `MappingSchema` in each repository method | Build it once and attach it to shared `DataOptions` |
| Adding `[Table]` and forgetting `[Column]` on members | Add `[Column]` to mapped members or set `IsColumnAttributeRequired = false` |
| Assuming conventions include private fields or complex objects | Map only supported scalar members or configure explicit mapping |
| Using unconstrained `string` / `decimal` in DDL scenarios | Specify `Length`, `Precision`, and `Scale` |
| Treating associations as lazy-loaded navigation properties | Use explicit query composition or `LoadWith` |
| Using online docs as primary source | Use package-local docs and XML-doc for the installed package version |

---

## Related documentation

- [`docs/architecture.md`](architecture.md) - mapping model and query translation overview.
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) - `MappingSchema` lifetime and unconstrained column anti-patterns.
- [`docs/query-temp-tables.md`](query-temp-tables.md) - temporary table mapping and DDL-sensitive schema rules.
- [`docs/configuration.md`](configuration.md) - attaching `MappingSchema` to `DataOptions`.
- `MappingSchema`, `TableAttribute`, `ColumnAttribute`, `FluentMappingBuilder` - XML documentation for full API details.
