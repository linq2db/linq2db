## Public-API surface classification

Used by `/review-pr` and `/verify-review` to turn the `api_changes` list produced by `code-reviewer` into review output (notes vs blocker findings).

### Inputs

- `api_changes` — list of `{namespace, symbol, change, file, line}` entries from `code-reviewer`. Only entries for files under `Source/*` are produced.
- PR milestone title (or `null`).
- List of files changed in the PR (for the suppressions-file check).

### Step 1. Suppressions-file update check

Compute once:

```
suppressions_updated = any file in the PR diff matches the glob
                       Source/**/CompatibilitySuppressions.xml
```

**Deletion gate.** Any `Source/**/CompatibilitySuppressions.xml` that is **deleted** by the PR must be emitted as a `BLK` finding (file-level). These files are the product's public-API baselines — deleting them unblocks `ApiCompat` without re-justifying the suppressions. The deletion is almost always a merge artefact. The finding should state the file path and recommend either (a) restoring the file from `origin/master` if the deletion was incidental, or (b) regenerating with `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` (the `api-baselines` skill) and explicitly confirming that zero suppressions are needed.

### Step 2. Major-release milestone check

A milestone counts as **major release** iff its title matches (case-insensitive):

```
^\d+\.0\.0(-(preview|rc)\.\d+)?$
```

Matches `6.0.0`, `6.0.0-preview.1`, `6.0.0-rc.3`, `6.0.0-RC.2`. The repo's milestone history uses exactly these forms — no `-beta`, no `-alpha`, no space-separated suffixes.

### Step 3. Per-change classification

First, if the `api_changes` list is non-empty, emit **one deduplicated review note**: *"API baselines need a refresh (run the `api-baselines` skill)."* Checkbox `[x]` if `suppressions_updated`, else `[ ]`. This fires regardless of change type or namespace — the suppressions file needs regenerating whenever the surface shifts.

Then, for each `api_changes` entry, decide whether to emit an **additional** finding based on the table below. Entries are classified by their `change` field (one of `"added"`, `"modified"`, `"removed"`).

| Change | Namespace | Milestone | Extra action |
|--------|-----------|-----------|--------------|
| `added` | Equals `LinqToDB.Internal` or starts with `LinqToDB.Internal.` | any | **None.** Additive internal surface never needs review. The baselines-refresh note covers the follow-up work. |
| `added` | Anywhere else (including `LinqToDB`, `LinqToDB.SqlQuery`, etc.) | any | **Run the additive-location sanity check** (step 4). Milestone-wise the change is always allowed — additive public surface is fine in any release — but the reviewer must verify that *public* is the intended visibility for this symbol, rather than `Internal.*` or `private`. |
| `modified` or `removed` | Equals `LinqToDB.Internal` or starts with `LinqToDB.Internal.` | any | None. Internal surface is not a stability contract. |
| `modified` or `removed` | Anywhere else | major-release (per step 2) | Emit one informational note listing the non-`Internal.*` namespaces that had **modified/removed** symbols, so the human reviewer is aware of the breaking surface changes. |
| `modified` or `removed` | Anywhere else | not a major release | Emit a **BLK finding** per change. Title: `Public API modified or removed outside LinqToDB.Internal.*`. Body includes the symbol, file, line, and wording from [`code-design.md`](code-design.md) → **Public API is a contract**: *"Types, method signatures, and observable SQL are a stability contract. This change needs explicit justification and a major-release milestone."* **But first check the SQL AST exception below** — if the symbol is a SQL AST type in `LinqToDB.SqlQuery`, the correct finding is a namespace-placement fix, not a milestone gate. |

### Step 4. Additive-location sanity check

For every `added` entry whose namespace is **not** `LinqToDB.Internal` / `LinqToDB.Internal.*`, evaluate whether the new surface is genuinely intended as public API or whether it leaked out by accident. Allowed ≠ appropriate — the milestone gate is satisfied, but public surface is still a long-term contract, so each newly-exposed symbol should be screened before it locks in.

Three outcomes per `added` entry:

1. **Public is intentional** — user-facing fluent builder, LINQ extension method, documented helper, configuration attribute, etc. No finding.
2. **Should be `LinqToDB.Internal.*`** — the symbol is an implementation building block whose only callers are inside the library itself. Emit a **MAJ** finding per change. Title: `New public API likely belongs in LinqToDB.Internal.*`. Fix: *"Move `<symbol>` to the corresponding `LinqToDB.Internal.*` namespace (or nest it inside an existing Internal type) so it doesn't become a permanent public contract."* Cross-reference [`code-design.md`](code-design.md) → **SQL AST types live in `LinqToDB.Internal.SqlQuery`** when the symbol matches that pattern specifically.
3. **Should be `private` / `internal` / `protected`** — the symbol has no external consumer and no conceptual reason to be visible outside its declaring type. Emit a **MAJ** finding per change. Title: `New public member appears to be an implementation detail`. Fix: *"Reduce visibility of `<symbol>` to `private` / `internal` / `protected` — nothing outside `<containing type>` looks like a legitimate caller."*

**Heuristics for outcome 2 ("should be `Internal.*`"):**

- Type name starts with `Sql` and represents a query-tree node, clause, boundary, or visitor — i.e. a SQL AST type (see SQL AST exception below).
- Type name ends with `Visitor`, `Translator`, `Converter`, `Builder` (non-fluent), `Optimizer`, `Rewriter`, `Walker` — pipeline machinery.
- Type is under a namespace like `Linq.Translation`, `SqlProvider`, `SqlQuery.Visitors`, `Expressions` — package paths that elsewhere contain only Internal surface.
- Type name contains `Info`, `Context`, `State`, `Descriptor`, `Accessor` **and** is only referenced from within the library.
- Method is named `Apply*`, `Visit*`, `Modify*`, `Rewrite*`, `Translate*`, `Build*` on a type that users don't construct directly.

**Heuristics for outcome 3 ("should be `private` / `internal`"):**

- Public constructor on a class instantiated only by the library itself (no user-level factory or `new` documented).
- Public field that looks like mutable implementation state (typical smell: `public readonly List<…>` on a type that has a public `Add` method intended for external callers — but also the inverse: a `public List<…> Foo` field on a class never mentioned in user docs).
- Public method whose signature references AST / translator / internal-only parameter types (e.g. `SqlExpression`, `ISqlExpressionTranslator`) — if the parameter type isn't part of the user-facing API, neither is the method.
- Nested helper class that only appears in the body of its outer type's methods.

**When uncertain**, emit a **SUG** finding instead of MAJ — worded as a question: *"`<symbol>` looks like an internal detail — is public visibility intentional?"* — so the author confirms rather than being asked to take action.

This check runs regardless of milestone. It is a design-quality gate, not a stability gate: the cost of relaxing visibility later is zero; the cost of reducing visibility later is a breaking change.

### SQL AST namespace exception

Before emitting a BLK per the last row above, check whether the modified / removed symbol is a SQL AST type (query-tree building block used only during query construction, translation, and rendering) that happens to live under the public `LinqToDB.SqlQuery` namespace rather than `LinqToDB.Internal.SqlQuery` where it belongs. See [`code-design.md`](code-design.md) → **SQL AST types live in `LinqToDB.Internal.SqlQuery`** for the underlying rule.

For those cases, emit a BLK whose **Fix** is *"Move `<TypeName>` to `LinqToDB.Internal.SqlQuery` as part of this PR — the signature change then becomes AST-internal and no longer trips ApiCompat."* — not the milestone-gate wording above. The legacy classes currently under `LinqToDB.SqlQuery` include `SqlFrameClause`, `SqlExtendedFunction`, `SqlSearchCondition`, `SqlFrameBoundary`, and similar AST nodes; some carry a `// TODO: v7 - move to internal namespace` marker. When in doubt, check whether the type is referenced only from query-building / translation / SQL-rendering code — if yes, it's AST and belongs internal.

### Notes

- The baselines-refresh note is deduplicated — at most one appears in the review regardless of how many changes triggered it.
- Additive changes (`change: "added"`) never produce milestone-gated BLK findings — they are always allowed surface-expansion. They *can* produce MAJ / SUG findings from the step-4 location sanity check when the new symbol looks misplaced (should be `Internal.*` or should be `private` / `internal`). Additive changes in namespaces starting with `LinqToDB.Internal` skip step 4 entirely.
- When a PR has both `Internal.*` changes and non-`Internal.*` changes under a major-release milestone, only the single deduplicated note plus (if any modifications/removals exist) the one informational note appear — no BLK findings. Step-4 findings on additive non-Internal entries are independent of the milestone and may still appear.
- `/verify-review` reapplies this classification from scratch against the current `api_changes`. It does not try to reconcile against the prior classification.
