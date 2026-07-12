# linq2db Skill Black-Box Test Scenarios

This repository-local file lists repeatable black-box questions for testing the NuGet-shipped
linq2db skill documentation.

Do not upload this file to Custom GPT Knowledge and do not allow the tested agent to read it. The
tested agent should receive only one question at a time plus the black-box test prompt from
`.agents/black-box-docs-test-prompt.md`.

Each question should be sent as a fresh test question, preferably prefixed with `q:` so the test
agent resets conversation-local conclusions.

Passing means the agent:

- answers the practical question;
- grounds LinqToDB-specific API names, receivers, scopes, overloads, and fallbacks in package docs
  or XML-doc;
- explains which package docs/XML-doc entries it inspected;
- labels documentation gaps instead of inventing unsupported LinqToDB behavior.

## Architecture And API Discovery

- q: What are the mandatory package-local files an AI agent should read before writing linq2db code?
- q: How should an agent find the exact overload for a linq2db API if markdown only mentions the API family?
- q: Can application code use APIs from `LinqToDB.Internal.*` if they appear in XML documentation?
- q: How can I tell whether a linq2db method executes immediately or only composes a query?
- q: I found conflicting package markdown and XML-doc for an overload. Which source should I trust for the exact signature?
- q: How do I decide whether to use online documentation or package-local documentation for an installed linq2db version?

## Provider Setup And Configuration

- q: I installed only the `linq2db` NuGet package and called `UsePostgreSQL`. What else do I need?
- q: How should I configure `DataOptions` for repeated short-lived database operations?
- q: How do I configure SQL Server provider options without rebuilding configuration per request?
- q: How can I enable SQL tracing or logging using package-confirmed APIs?
- q: How do I add a retry policy to linq2db configuration?
- q: How should I choose provider configuration when the database provider is selected at runtime?
- q: What is the difference between configuring `DataOptions` and creating a `DataConnection`?

## Mapping

- q: How do I map a class to a table using attributes?
- q: How do I configure mapping fluently with `MappingSchema`?
- q: Should I create a new `MappingSchema` for every request?
- q: How do I specify string length and decimal precision for generated table DDL?
- q: How do I map a property to a differently named database column?
- q: How do I configure mapping for anonymous temporary table row types?
- q: What should I do if a mapping feature is not covered by markdown docs?

## CRUD And DML

- q: How do I insert one entity asynchronously?
- q: How do I insert rows from one query into another table?
- q: How do I update rows using a set-based update expression?
- q: How do I delete rows without materializing them first?
- q: How do I perform an upsert with linq2db?
- q: How do I use `Merge` to update matched rows and insert missing rows?
- q: How do I bulk insert an in-memory collection?
- q: Which namespace is required for async query and DML methods?

## Temporary Tables And CTE

- q: How do I create and use a temporary table safely?
- q: I have rows in memory. What is the recommended way to create and populate a temporary table?
- q: I have an `IQueryable` source. How do I create a temporary table from it?
- q: How do I create an empty temporary table and then configure its mapping?
- q: Can I use a temporary table created from one `DataConnection` on another connection?
- q: How do I create a temporary table with anonymous row types and explicit string length?
- q: How do I write a recursive CTE with linq2db?
- q: How should an agent decide whether a CTE scenario is documented or needs raw XML-doc confirmation?

## Hints

- q: How do I add the `FINAL` hint in ClickHouse?
- q: How do I add `NOLOCK` to all tables already present in a SQL Server query?
- q: How do I add SQL Server `OPTION (RECOMPILE)` to a query?
- q: How do I add SQL Server `MAXRECURSION` to a recursive query?
- q: How do I add a PostgreSQL `MATERIALIZED` CTE hint?
- q: How do I add an Oracle `PARALLEL` hint?
- q: How do I add a MySQL index hint to a table?
- q: How do I combine several SQL Server typed hints after calling the provider marker?
- q: When should an answer use `TableHint` or `QueryHint` instead of a typed provider helper?
- q: How do I reference generated SQL table aliases or table names inside a hint format string?

## Custom SQL And Translatable Methods

- q: How do I map a custom C# method to a SQL expression?
- q: How do I call a SQL function from a LINQ query using package-confirmed APIs?
- q: How do I use raw SQL as a query source while preserving parameters?
- q: How do I execute a raw SQL update command with parameters?
- q: How do I inspect generated SQL and parameter values for a LINQ query?
- q: How do I control where linq2db places a generated alias in a `FromSql` query?
- q: How do I decide whether a method is translated to SQL or evaluated after materialization?
- q: How do I add provider-specific SQL syntax when no typed linq2db API exists?
- q: How do I avoid inventing a custom SQL API when the docs only show XML-doc discovery?

## Interceptors

- q: How do I register an interceptor with linq2db configuration?
- q: Which interceptor should I use to inspect or modify executed commands?
- q: How do I intercept connection lifecycle events?
- q: How do I intercept query expression processing?
- q: How do I add an exception interceptor?
- q: Are interceptors a first-choice API for provider-specific SQL syntax, or a fallback?

## Associations And Eager Loading

- q: How do I map a required many-to-one association from `Order` to `Customer`?
- q: How do I map a one-to-many association from `Customer` to `Orders` using fluent mapping?
- q: Does linq2db lazy-load association properties after `ToListAsync()`?
- q: How do I load `Order.Items.Product.Category` with `LoadWith` / `ThenLoad`?
- q: How do I filter rows loaded for a collection association?
- q: What does `CanBeNull = false` change for an association?
- q: When should I use a predicate association instead of `ThisKey` / `OtherKey`?
- q: How do I use `WithKeyedLoadStrategy()` for a query with eager-loaded collections?
- q: What happens if I use `Items[0]` or `First()` inside a `LoadWith` path?
- q: How do I prevent accidental implicit collection loading in projections?

## Anti-Patterns And Troubleshooting

- q: Why is filtering after `ToListAsync()` a linq2db anti-pattern?
- q: Why is sharing one `DataConnection` concurrently unsafe?
- q: Why can recreating `MappingSchema` per request hurt performance?
- q: What should I check if a translation fails for a LINQ expression?
- q: How should I answer if package docs expose an API but do not explain which database tuning strategy to choose?
- q: How should I distinguish general SQL advice from package-confirmed linq2db API usage?

## Provider Capabilities

- q: How do I check whether a provider supports native bulk copy?
- q: How do I check whether a provider supports `MERGE`?
- q: How do I check whether a provider supports recursive CTEs?
- q: How do I check provider-specific differences for `OUTPUT` or `RETURNING`?
- q: What should an agent do if a capability matrix does not cover a provider-specific edge case?

## Coverage Boundary

- q: Are stored procedures covered in depth by package-local AI docs?
- q: Are window functions a current documentation priority?
- q: What should an agent do for an uncovered topic that still has public XML-doc APIs?
- q: Does this skill cover extension packages such as `LinqToDB.EntityFrameworkCore`, `LinqToDB.Remote.*`, or `LinqToDB.CLI`?
- q: What should an agent do if the user asks about `CompiledQuery` and there is no dedicated guide?
- q: What should an agent do if the user asks about metrics or `ActivityService`?
- q: What should an agent do if the user asks about `UpdateOptimistic` or `OptimisticLockPropertyAttribute`?
- q: How should an agent report a documentation failure found during black-box testing?
