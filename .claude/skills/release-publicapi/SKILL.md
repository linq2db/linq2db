---
name: release-publicapi
description: PublicAPI.Shipped / PublicAPI.Unshipped reconciliation for the release-prep workflow. Runs a Release build to surface any pending RS0016/RS0017 diagnostics (stop-and-ask-user when present — they belong in IDE quick-fix, not in this script), then moves every project's PublicAPI.Unshipped.txt content into PublicAPI.Shipped.txt across all per-TFM and flat layouts. Distinct from `/api-baselines` (which handles `CompatibilitySuppressions.xml` — a different tool, different files). Invoked by `/release` step 2 or directly when running release prep.
---

# /release-publicapi

## What this skill is (and isn't)

**Is:** the per-release PublicAPI shipped/unshipped reconciliation. For every `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` pair under `Source/`, moves Unshipped entries into Shipped (sorted union), then truncates Unshipped to just the `#nullable enable` directive. Runs across ~36 file pairs spanning multi-TFM (per-`net{TFM}/` subfolder) and single-TFM (flat) layouts.

**Isn't:**

- Not [`/api-baselines`](../api-baselines/SKILL.md) — that skill handles `CompatibilitySuppressions.xml` (Microsoft.DotNet.ApiCompat). PublicAPI.*.txt is a different system (Microsoft.CodeAnalysis.PublicApiAnalyzers) tracking the declared public surface. Both run during a release prep, but on independent files.
- Not a Roslyn codefix substitute. If the Release build emits any **RS0016** (symbol not in declared API) or **RS0017** (symbol in declared API but not in code), the skill stops and tells the user to fix those via IDE quick-fix first. Re-running a build against the IDE-fixed Unshipped is the same path normal development uses — the script does not reimplement Roslyn codefixes.

## When to run

- During release prep as task 2 (called by `/release` orchestrator), **after** task 6 (`/release-verify`) has produced a clean build. Task 6's build addresses any RS0016/RS0017 diagnostics that step 1 here would normally surface, so when invoked post-task-6 the build step (1) is **skip-able** — go straight to step 3 (plan).
- Manually when the user wants to roll Unshipped into Shipped outside of a release (rare).

## Required reading

- [`.claude/docs/code-design.md`](../../docs/code-design.md) → **Never hand-edit API baseline files** still applies to `CompatibilitySuppressions.xml`. PublicAPI.*.txt is **not** under that rule — it's editable by the analyzer's codefixes and by this skill's apply step.
- [`.claude/docs/agent-rules.md`](../../docs/agent-rules.md) → **Git commit rules** (each commit is its own user request).

## Procedure

### 1. Run a Release build (skip when post-task-6)

**Skip this step** when invoked from `/release` after task 6 (`/release-verify`) has already run — the verification build there has already cleared RS0016/RS0017, and re-building here just burns minutes. Go straight to step 3.

For standalone (non-release-prep) invocations, or when explicitly re-validating mid-flow:

```
pwsh -NoProfile -File .claude/scripts/release-publicapi-reconcile.ps1 -Action build -Version <ver>
```

The script runs `dotnet build linq2db.slnx -c Release` and captures stdout+stderr to `.build/.claude/release-<ver>-publicapi-raw.txt`. Returns a status with exit code + line count.

If the build itself fails (compile errors unrelated to PublicAPI), stop and surface those — they need fixing before reconciliation makes sense.

### 2. Discover RS0016 / RS0017

```
pwsh -NoProfile -File .claude/scripts/release-publicapi-reconcile.ps1 -Action discover -Version <ver>
```

Parses the raw build log for RS0016 (missing-from-declared-API) and RS0017 (in-declared-API-but-not-in-code) diagnostics. Groups by source file + project.

**If any RS-diagnostics are present:**
- Surface them to the user as a numbered list — file:line, severity code, symbol text.
- Tell the user: "fix these via IDE quick-fix (Visual Studio: place cursor on the symbol → Quick Actions → 'Add to public API' or 'Remove from public API'), then re-run from step 1." The script does **not** auto-fix.
- Stop. The user must clear RS0016/RS0017 before the move step.

**If zero:** proceed to step 3.

### 3. Compute the move plan

```
pwsh -NoProfile -File .claude/scripts/release-publicapi-reconcile.ps1 -Action plan -Version <ver>
```

Walks every `PublicAPI.Shipped.txt` + sibling `PublicAPI.Unshipped.txt` pair under `Source/`. Per pair, computes:

- `newShipped = sorted(dedup(union(currentShipped-entries, currentUnshipped-entries)))` (the `#nullable enable` directive is preserved as the first line)
- `newUnshipped = "#nullable enable\n"` (just the directive — Unshipped is reset to empty)

Writes the plan as JSON to `.build/.claude/release-<ver>-publicapi-plan.json`. The plan lists every file pair with `noOp: true|false`, line counts, and per-file before/after.

### 4. Review the diff

```
pwsh -NoProfile -File .claude/scripts/release-publicapi-reconcile.ps1 -Action diff -Version <ver>
```

Prints a per-file unified diff (only the changed files). Surface this to the user.

**Deletion check:** if any line is being removed from `Shipped` (which only happens when Unshipped contains a tombstone like `*REMOVED*` — uncommon), flag it for separate scrutiny per `code-design.md` → API removals outside `LinqToDB.Internal.*` need explicit user sign-off.

### 5. Apply

```
pwsh -NoProfile -File .claude/scripts/release-publicapi-reconcile.ps1 -Action apply -Version <ver>
```

Reads the plan file and writes both files per project/TFM. UTF-8 encoding mirrors each file's existing BOM state (71 of the 72 PublicAPI files in `Source/**` currently start with a UTF-8 BOM, so most output keeps the BOM); line endings are normalized to LF (the analyzer is whitespace-tolerant; CRLF inputs would be rewritten as LF, but no PublicAPI file in the repo uses CRLF today).

### 6. Verify

Re-run step 1 (build) and step 2 (discover) to confirm zero RS0016/RS0017 after the move. Surface the result.

### 7. Update state + commit

1. Tell `/release` orchestrator: task 2 → `done` via `release-state.ps1 -Action update -Version <ver> -TaskId 2 -Status done -Annotation "<N files updated>"`.
2. Stage the modified files: `git add Source/**/PublicAPI.{Shipped,Unshipped}.txt` (or per-project paths from the plan).
3. Confirm commit message + commit. Suggested message:
   ```
   Release <ver>: PublicAPI reconciliation

   Move Unshipped entries to Shipped across <N> PublicAPI file pairs
   (multi-TFM and flat layouts). Verified zero RS0016/RS0017 post-apply.
   ```
4. Push on user confirmation (push semantics rule in `/release` Conventions).

## Don'ts

- Do **not** try to auto-apply RS0016 codefixes. They live in Roslyn. The script's contract is: surface them, wait for user IDE-fix, re-discover.
- Do **not** edit `PublicAPI.*.txt` files individually with `Edit` calls during a reconciliation — that's what the script's `apply` action is for. ~72 files in one batched write avoids burning permission surface.
- Do **not** confuse with `/api-baselines`. Different file family, different generator, different policy check. Both run during release prep but on independent paths.
- Do **not** skip the verify step (6). A clean post-apply build is the only proof the move didn't break the analyzer's view of the declared API.
