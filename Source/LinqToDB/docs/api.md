# API Discovery

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - find the exact LinqToDB API for the installed package version
> - decide whether a typed API exists before using a generic fallback
> - inspect overloads, return types, XML-doc remarks, or AI-Tags
> - answer provider-specific API questions without inventing methods

---

## Core Rule

Markdown guides explain concepts, constraints, and common mistakes. They are not a complete public
API listing.

The package XML documentation is the exact current-version public API surface for members that have
XML comments:

```text
lib/<TFM>/linq2db.xml
```

Missing from markdown does not mean missing from LinqToDB. Do not conclude that an API is
unavailable until XML-doc has been searched.

If this package also ships a generated markdown API extract, use it as a search aid. The canonical
API shape is still the XML-doc member entry.

---

## Discovery Workflow

Use this workflow for any API-level question:

1. Read the relevant task guide in `docs/*.md`.
2. Search XML-doc for likely type names, method names, provider namespaces, SQL keywords, and
   AI-Tags.
3. Verify the exact member signature, receiver type, parameters, return type, remarks, and tags.
4. Prefer typed and provider-specific APIs found in XML-doc.
5. Use generic string-based APIs only when no typed API exists or when the typed API does not cover
   the requested case.
6. If markdown and XML-doc disagree, prefer XML-doc for exact API shape and mention the discrepancy.

Do not invent APIs, overloads, options, provider flags, provider capabilities, XML-doc remarks, or
AI-Tags.

---

## Search Strategy

Search broadly first, then narrow by member signature.

| Task | Search for |
|---|---|
| Provider-specific API | Provider namespace, provider marker (`AsSqlServer`, `AsClickHouse`, etc.), provider `*Hints`/`*Tools`/extension type names |
| SQL hints | SQL hint text, `Group=Hints`, `HintType=...`, provider `*Hints` type |
| Configuration | `DataOptions`, `UseXxx`, option name, provider name |
| Mapping | Attribute name, fluent method name, `MappingSchema`, `Configuration`, `DataType`, `DbType` |
| DML/query extension | Operation name, extension method name, `Group=DML`, `Group=Merge`, receiver type |
| Async API | Method name plus `Async`; remember async query/DML methods often require `using LinqToDB.Async;` |

When searching XML-doc, include both the user-facing SQL/database term and likely LinqToDB naming.
Provider-specific helpers do not always map one-to-one to SQL spelling.

---

## Fallbacks

Generic APIs are valid tools, but they are not proof that a typed API is absent.

Before recommending these APIs, search XML-doc for a typed or provider-specific member:

- `QueryHint(...)`
- `TableHint(...)`
- `Sql.Expression`
- `Sql.Extension`
- `Sql.Table`
- raw SQL command text
- SQL text rewriting through interceptors

Use them when the typed API is missing, too narrow, or intentionally not available for the
requested provider feature.

---

## AI-Tags

Some XML-doc remarks contain `AI-Tags`. Use them as machine-readable classification, not as a
replacement for the full member documentation.

For hints:

- `HintType` tells where the hint is applied (`Table`, `TablesInScope`, `Join`, `Query`, etc.).
- The concrete SQL hint text is documented in the member XML summary, usually inside `<c>...</c>`.
- The receiver type matters. A table hint helper and a query/scope hint helper can have similar
  names but different extension receiver types.

---

## Related Documentation

- [`AGENT_GUIDE.md`](../AGENT_GUIDE.md) - mandatory package entry point for agents.
- [`docs/architecture.md`](architecture.md) - translation pipeline and entry points.
- [`docs/agent-antipatterns.md`](agent-antipatterns.md) - common mistakes and quick symptom index.
- [`docs/hints.md`](hints.md) - query, table, join, subquery, provider-specific, and MERGE hints.
- [`docs/provider-setup.md`](provider-setup.md) - provider packages and setup.

