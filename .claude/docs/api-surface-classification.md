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

For each `api_changes` entry:

| Namespace | Milestone | Action |
|-----------|-----------|--------|
| Equals `LinqToDB.Internal` or starts with `LinqToDB.Internal.` | any | Emit one **deduplicated** review note: *"API baselines need a refresh (run the `api-baselines` skill)."* Checkbox `[x]` if `suppressions_updated`, else `[ ]`. |
| Anywhere else | major-release (per step 2) | Same as above: emit the single deduplicated note. Additionally emit one informational note listing the non-`Internal.*` namespaces touched in the PR, so the human reviewer is aware of the surface expansion. |
| Anywhere else | not a major release | Emit a **BLK finding** per change. Title: `Public API change outside LinqToDB.Internal.*`. Body includes the symbol, file, line, and wording from `agent-rules.md` → Agent Guardrails: *"Public API, architecture, and behavior are contracts. This change needs explicit justification and a major-release milestone."* |

### Notes

- The baselines-refresh note is deduplicated — at most one appears in the review regardless of how many changes triggered it.
- When a PR has both `Internal.*` changes and non-`Internal.*` changes under a major-release milestone, only the single deduplicated note plus the one informational note appear — no BLK findings.
- `/verify-review` reapplies this classification from scratch against the current `api_changes`. It does not try to reconcile against the prior classification.
