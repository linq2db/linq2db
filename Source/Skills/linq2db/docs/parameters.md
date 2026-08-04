# Parameters

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`SKILL.md`](../SKILL.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

Use this guide when constructing a typed parameter, reading a stored-procedure output parameter,
or deciding whether a value should be sent to the database as a bound parameter or embedded as a
SQL literal.

## `DataParameter`

Typed factory methods (`DataParameter.Int32(name, value)`, `.VarChar(...)`, `.Guid(...)`, etc.) are
the most direct way to construct a parameter when the target `DataType` is known.

`DataParameter.Create(name, value)` infers `DataType` from the value's C# type, but its defaults do
**not** always match the same-named typed factory:

| Value type | `DataParameter.Create` picks | Not |
|---|---|---|
| `char` / `string` | `DataType.NChar` / `NVarChar` | `Char` / `VarChar` |
| `byte[]` / `Binary` | `DataType.VarBinary` | - |
| `DateTime` | `DataType.DateTime2` | `DateTime` |

`DataType`, `DbType`, `Size`, `Precision`, `Scale` are settable properties on `DataParameter`.

## Output and input-output parameters

```csharp
var input  = DataParameter.Int32("input", 1);
var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
var result = db.ExecuteProc("ExecuteProcIntParameters", input, output);
// output.Value is only populated here, after the call
```

`Direction = ParameterDirection.Output` (or `InputOutput`) marks a parameter for the database to
write back to. The initial `Value` is typically `null`; `DataType` must be set explicitly since
there is no input value to infer it from.

**Deferred-execution gotcha:** `QueryProc<T>`/`QueryProcAsync<T>` return a lazily-enumerated
sequence - `output.Value` stays `null` immediately after the call and is only populated once the
result is actually enumerated (`.ToList()` / `await ... .ToListAsync()`). `ExecuteProc`/
`ExecuteProcAsync` execute immediately, so `output.Value` is available right after the call
returns. Reading an output parameter from a `QueryProc` call before enumerating the result reads a
stale/unset value, not a query failure.

## Forcing a parameter or a literal for a specific value

| API | Scope | Effect |
|---|---|---|
| `.InlineParameters()` (LINQ method on `IQueryable<T>`) | whole query | Deferred, composable - forces parameterizable values in the query to be embedded as SQL literals. |
| `Sql.ToSql(value)` | one expression | Forces server-side evaluation **and** inlines the value as a literal. |
| `Sql.AsSql(value)` | one expression | Forces server-side evaluation; does **not** force inlining - the value may still be sent as a parameter. |
| `Sql.Parameter(value)` | one value | Forces generation of a real bound parameter. |
| `Sql.Constant(value)` | one value | Forces the value to be embedded as a SQL literal, not a parameter. |

`Sql.Parameter`/`Sql.Constant` are the value-level counterparts to `InlineParameters`/`ToSql`:
pick the value, not the whole query, as the scope of control.

```csharp
where q.ParentID == Sql.Parameter(someId)   // real bound parameter - same query plan across values
where q.ParentID == Sql.Constant(someId)    // literal in the SQL text - a new value produces a new query/cache entry
```

## Non-translatable expressions that do not reference query data

A subexpression that cannot be translated to SQL, but also does not reference any query data
(a table column), can be evaluated on the client and sent to SQL as a parameter - this is
intentional, not an error. A subexpression that cannot be translated **and** references query data
throws instead. See [`agent-antipatterns.md`](agent-antipatterns.md) anti-pattern #4 for the full
rule and examples; this guide does not repeat it.

## Do not invent automatic parameterization/inlining rules

Beyond `InlineParameters`/`ToSql`/`AsSql`/`Sql.Parameter`/`Sql.Constant` and the client-evaluation
rule above, whether linq2db decides to parameterize or inline a given C# value automatically is an
internal translator heuristic that package docs do not expose as a stable, documented contract. If
asked why a specific value appears as a literal or a parameter outside of these APIs, say that the
package docs do not confirm the exact rule - do not invent explanations such as "local variables are
always inlined" or "only captured fields are parameterized."

## Common Mistakes

### Reading an output parameter before enumerating a deferred `QueryProc` result

Wrong:

```csharp
var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
var persons = db.QueryProc<Person>("MyProc", output);
if (output.Value != null) { /* ... */ }   // still null - persons has not been enumerated yet
```

Correct:

```csharp
var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
var persons = db.QueryProc<Person>("MyProc", output).ToList();
if (output.Value != null) { /* ... */ }   // populated after enumeration
```

### Assuming `DataParameter.Create` matches the same-named typed factory

Wrong: using `DataParameter.Create("name", someChar)` and expecting `DataType.Char` to match a
`CHAR` column - `Create` picks `NChar` for `char`/`string` values.

Correct: use the explicit typed factory (`DataParameter.Char(...)`) when the target column type
must match exactly, or pass `DataType` explicitly.

## API Lookup Anchors

Search `docs/api.md` for:

- `DataParameter`
- `ParameterDirection`
- `InlineParameters`
- `Sql.ToSql`
- `Sql.AsSql`
- `Sql.Parameter`
- `Sql.Constant`
