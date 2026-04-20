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
| `added` | any | any | **None.** Additive public-API expansion (new types, methods, enum members, etc.) is always allowed, in every milestone and every namespace. The baselines-refresh note covers the follow-up work. |
| `modified` or `removed` | Equals `LinqToDB.Internal` or starts with `LinqToDB.Internal.` | any | None. Internal surface is not a stability contract. |
| `modified` or `removed` | Anywhere else | major-release (per step 2) | Emit one informational note listing the non-`Internal.*` namespaces that had **modified/removed** symbols, so the human reviewer is aware of the breaking surface changes. |
| `modified` or `removed` | Anywhere else | not a major release | Emit a **BLK finding** per change. Title: `Public API modified or removed outside LinqToDB.Internal.*`. Body includes the symbol, file, line, and wording from [`code-design.md`](code-design.md) → **Public API is a contract**: *"Types, method signatures, and observable SQL are a stability contract. This change needs explicit justification and a major-release milestone."* **But first check the SQL AST exception below** — if the symbol is a SQL AST type in `LinqToDB.SqlQuery`, the correct finding is a namespace-placement fix, not a milestone gate. |

### SQL AST namespace exception

Before emitting a BLK per the last row above, check whether the modified / removed symbol is a SQL AST type (query-tree building block used only during query construction, translation, and rendering) that happens to live under the public `LinqToDB.SqlQuery` namespace rather than `LinqToDB.Internal.SqlQuery` where it belongs. See [`code-design.md`](code-design.md) → **SQL AST types live in `LinqToDB.Internal.SqlQuery`** for the underlying rule.

For those cases, emit a BLK whose **Fix** is *"Move `<TypeName>` to `LinqToDB.Internal.SqlQuery` as part of this PR — the signature change then becomes AST-internal and no longer trips ApiCompat."* — not the milestone-gate wording above. The legacy classes currently under `LinqToDB.SqlQuery` include `SqlFrameClause`, `SqlExtendedFunction`, `SqlSearchCondition`, `SqlFrameBoundary`, and similar AST nodes; some carry a `// TODO: v7 - move to internal namespace` marker. When in doubt, check whether the type is referenced only from query-building / translation / SQL-rendering code — if yes, it's AST and belongs internal.

### Notes

- The baselines-refresh note is deduplicated — at most one appears in the review regardless of how many changes triggered it.
- Additive changes (`change: "added"`) never produce findings — they are expected in minor and major releases alike. Only modifications and removals are gated by the major-release rule.
- When a PR has both `Internal.*` changes and non-`Internal.*` changes under a major-release milestone, only the single deduplicated note plus (if any modifications/removals exist) the one informational note appear — no BLK findings.
- `/verify-review` reapplies this classification from scratch against the current `api_changes`. It does not try to reconcile against the prior classification.
