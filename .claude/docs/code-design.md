## Codebase design invariants

Design-level facts about what linq2db *is* as a library. These are invariants agents should preserve and reviewers should enforce — they describe the product, not the review workflow. Operational rules for how agents *act* on the codebase live in [`agent-rules.md`](agent-rules.md) → Agent Guardrails.

### Public API is a contract

Types, method signatures, and observable generated SQL in non-`Internal.*` namespaces under `Source/LinqToDB/` are a stability contract for downstream consumers. Don't modify them without a clear, explicit reason — new major-release milestone, documented breaking change, or a namespace-placement fix (see below).

The ApiCompat baseline files at `Source/**/CompatibilitySuppressions.xml` are the enforcement mechanism. Any surface change — add, modify, or remove — requires regenerating them (e.g. `dotnet pack -p:ApiCompatGenerateSuppressionFile=true`, or run the `api-baselines` skill).

### Cross-cutting internals are shared

The SQL AST (`Source/LinqToDB/SqlQuery/` and `Source/LinqToDB/Internal/SqlQuery/`), the `IDataProvider` interface, and the translator interfaces under `Source/LinqToDB/Linq/Translation/` are consumed by every database provider in the repo. A fix scoped to one provider or one test shouldn't reshape them — the blast radius is the whole product. When a local task seems to need a cross-cutting change, surface the question explicitly rather than making the change silently.

### SQL AST types live in `LinqToDB.Internal.SqlQuery`

The SQL query tree building blocks — `SqlFrameClause`, `SqlKeepClause`, `SqlExtendedFunction`, `SqlFrameBoundary`, `SqlSearchCondition`, and every other AST node used only during query construction, translation, and rendering — are library-internal. They are not part of the stable public surface. New AST types MUST go in the `LinqToDB.Internal.SqlQuery` namespace.

A handful of legacy AST classes still live under the public `LinqToDB.SqlQuery` namespace; some carry a `// TODO: v7 - move to internal namespace` marker acknowledging the debt. When a PR modifies one of those legacy AST types' signatures and ApiCompat flags it as a breaking public-API change, the correct fix is to **move the class to `LinqToDB.Internal.SqlQuery` in the same PR** — that repays the pre-existing design debt and removes the apparent public-API breakage in one step. Do **not** target a major-release milestone for what is really just internal AST evolution, and do not add the signature change to the suppression baseline as if it were an intentional public-API break.

This rule refines the `/review-pr` classification in [`api-surface-classification.md`](api-surface-classification.md): a `modified` or `removed` entry whose symbol is in `LinqToDB.SqlQuery` and targets a SQL AST type should be reviewed as a namespace-placement finding (fix: move to `Internal.SqlQuery`), not as a milestone-gated public-API-break BLK.

### Column-aligned formatting is intentional

Large blocks of the codebase use column-aligned formatting — property declarations line their `{ get; }` up at the same column, constructor parameters line their defaults up at the same column, constant declarations line their `=` up at the same column. This is deliberate house style, not accidental. Preserve it when editing; match the existing alignment of surrounding code rather than reformatting it to a narrower width.

Formatting is only worth flagging when it is clearly broken — three or more consecutive blank lines, mixed tabs and spaces that cause visible misalignment, indentation that doesn't match the enclosing scope. The positive alignment style is never the bug.
