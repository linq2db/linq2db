# LinqToDB Skill

Use this skill when a task involves LinqToDB queries, mapping, configuration, provider setup,
schema APIs, SQL generation, DML, interceptors, or provider-specific features.

This file is a compatibility entry point for agent skill systems. For this installed package
version, the mandatory package entry point is [`AGENT_GUIDE.md`](AGENT_GUIDE.md).

## Required Reading

Before using public LinqToDB APIs:

1. Read [`AGENT_GUIDE.md`](AGENT_GUIDE.md).
2. Read the task-specific guide under [`docs`](docs).
3. Use [`docs/api.md`](docs/api.md) for API discovery rules.
4. Use `lib/<TFM>/linq2db.xml` for exact current-version public API names, signatures, overloads,
   parameters, return types, remarks, and AI-Tags.

Do not use GitHub, online docs, blog posts, memory, or examples from another package version as
the primary source for API shape.

## API Discovery Rule

Missing from markdown does not mean missing from LinqToDB.

For API-level questions, especially provider-specific APIs, hints, SQL extensions, configuration,
and DML/query extensions:

1. Read the relevant markdown guide for concepts and boundaries.
2. Search XML documentation for the exact API surface.
3. Prefer typed or provider-specific APIs found in XML-doc.
4. Use generic string-based APIs (`QueryHint`, `TableHint`, `Sql.Expression`, raw SQL, etc.) only
   as fallbacks when no typed API exists or the typed API does not cover the requested case.

If a generated agent-friendly API extract is shipped with the package in addition to
`linq2db.xml`, use it as a search aid, but keep XML-doc as the canonical exact API source.

