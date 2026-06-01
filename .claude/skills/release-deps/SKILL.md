---
name: release-deps
description: NuGet dependency update for the release-prep workflow. Discovers every <PackageVersion> in Directory.Packages.props plus all <PackageReference VersionOverride="..."> sites across the repo, queries nuget.org for available versions, applies the project's version policy, walks per-package decisions, runs all predictive audits (analyzer rule catalogs, polyfill API diffs, fuget API surface diffs, drift checks), batch-applies all consequent edits. **Runs no verification build, no commit, no push, no CI trigger** — those are all owned by `/release-verify` (orchestrator task 6), which sees the cumulative state from every release-prep task and produces the single consolidated commit + push + CI trigger. Per-package custom rules + release-notes URLs accrue in `.claude/docs/release/nuget-package-notes.md`. Invoked by `/release` step 1 or directly when running release prep.
---

# /release-deps

## What this skill is (and isn't)

**Is:** the dependency-update phase of release prep. One discovery pass produces a table of every package with `current`, `latest-release`, `latest-prerelease`, `proposed`, and any `policy:` block reason. User picks updates per-package. Every audit that can be predicted from the package decisions runs **before** the apply (analyzer rule-catalog catch-up, polyfill API diff, fuget API surface diff, drift checks). All edits land in one batch. **No verification build inside this skill** — the verification build is owned by `/release` orchestrator and runs as a final gate **after all release-prep tasks** (deps + PublicAPI + milestone-check + test-matrix + release-notes + ad-hoc) so the build sees the cumulative state.

**Isn't:**

- Not for normal-development dependency updates. Those go through PRs targeted at the affected component.
- Not for adding new packages — adding a new `<PackageVersion>` is a code change unrelated to release prep.
- Not for the `linq2db.t4models` self-reference — that gets bumped post-release by [`release-postpublish`](../release-postpublish/SKILL.md) step 4 to the just-tagged version, not by this skill.

## When to run

- During release prep as task 1 (called by `/release` orchestrator).
- Manually when the user wants a one-off dependency-update pass outside a release (rare).

## Required reading

- [`.claude/docs/release/nuget-package-notes.md`](../../docs/release/nuget-package-notes.md) — accrued per-package rules; consulted before rendering the table and before each pre-build audit.
- [`.claude/docs/release/branch-and-pr.md`](../../docs/release/branch-and-pr.md) — branch + PR conventions.
- [`.claude/docs/ci-tests.md`](../../docs/ci-tests.md) — for the `/azp run test-all` trigger.

## Why this ordering

Every Release build of `linq2db.slnx` is a 10-25 minute cost. Earlier versions of this skill ran the build first and then reacted to its errors per-package — a Meziantou bump alone could trigger 3-4 build cycles (initial build → disable noisy new rules → re-build → fix one rule's errors → re-build → ...). The new ordering pushes every audit that can be done from package metadata alone to **before** the apply, batches all consequent file edits, and **does not run a verification build at all** — that is delegated up to `/release` orchestrator, which runs **one** verification build at the very end of release prep, after every other prep task (PublicAPI / milestone-check / test-matrix / release-notes / ad-hoc) has had its turn to mutate the tree. The build then sees the cumulative state in one shot.

If that final build fails on something none of our predictive audits caught, the fix is a follow-up commit on the same PR — but the common case becomes one build per release prep, not one per task.

## Procedure

### 1. Discover

```
pwsh -NoProfile -File .claude/scripts/release-deps-discover.ps1 -Action discover -Version <ver>
```

The script:

- Parses `<PackageVersion>` entries in `Directory.Packages.props`. Resolves `$(PropertyName)` references using the file's `<PropertyGroup>` properties.
- Greps `VersionOverride="..."` across `.csproj` / `.props` (rediscovered each run — locations may shift).
- Identifies **shipping** vs **test-only** packages: a package is *shipping* if any `Source/**/*.csproj` (excluding `Source/LinqToDB.LINQPad/`, `Source/LinqToDB.CLI/`, which are tooling not shipping libs) has a `<PackageReference Include="<id>" />` for it.
- Queries `https://api.nuget.org/v3/registration5-gz-semver2/<id>/index.json` for each unique package — returns **listed** versions only (skip unlisted — they may be removed/deprecated). Parallel fan-out, cached at `.build/.claude/release-<ver>-deps-cache/<id>.json`.
- Applies policy:
  - **runtime-pin** (shipping + Id matches `^(System\.|Microsoft\.Extensions\.|Microsoft\.AspNetCore\.|Microsoft\.Bcl\.)` + current is an initial `.0` version): keep current. Surface latest-release as a *blocked* update with reason `policy:runtime-pin`.
  - **shipping-prerelease**: if a candidate target version is prerelease, mark the row blocked unless user explicitly overrides.
  - **vulnerable**: not detected by this discovery pass — surfaced by step 4f's advisory / provenance audit instead.
- Emits a plan JSON at `.build/.claude/release-<ver>-deps-plan.json`.

Conditional `<PackageVersion>` entries (TFM-bracketed via `Condition="..."`) are surfaced as separate rows with the condition shown.

### 2. Render the table

The agent reads the plan and renders one numbered row per package. The table includes packages with available updates blocked by policy — visible so the user can override.

```
#  Package                          Current        Latest-Rel    Latest-Pre    Proposed   Rule  Notes  Reason
1  Npgsql                           9.0.3          9.0.4         10.0.0-rc.1   9.0.4       —    ?
2  Microsoft.Data.SqlClient         5.2.2          6.0.1         —             6.0.1       ✓    ✓
3  Oracle.ManagedDataAccess (OV)    23.6.1         23.7.0        —             23.7.0      —    ?
4  Microsoft.Extensions.Logging     8.0.0          9.0.5         —             —           —    —      policy:runtime-pin
5  SomeAnalyzer                     1.0.0          —             2.0.0-rc.1    2.0.0-rc.1  —    ?     analyzer-allowed-prerelease
...
```

Columns:
- `(OV)` after the Id marks a `VersionOverride` site (different apply mechanic).
- `Rule` (✓ / —): does `nuget-package-notes.md` have a custom rule recorded?
- `Notes` (✓ / ?): is a release-notes URL recorded?
- `Reason`: populated only for blocked rows.

### 3. Selection + per-package walk

Walk every actionable row with the user. Per-package category rules from `nuget-package-notes.md` may auto-resolve some rows without asking (e.g. `*LatestForNuget` properties stay pinned, `*Latest` properties auto-bump to latest X.0.x). For each row that needs a decision:

1. Show release notes between current and target version. If `notes-url-known` is `✓`, fetch the URL via WebFetch (or open it in browser for user reading). Else ask user: _"where are release notes for `<package>`?"_ — record the URL in `nuget-package-notes.md`.
2. Ask if any custom update rule applies (e.g. co-bump with another package, post-update codegen rerun, downgrade-allowed-for-compat, fuget API diff procedure, TFM-cap rule, vulnerability pin). On a non-trivial answer, append a structured entry to `nuget-package-notes.md` (per its schema header). Do **not** stop the walk for a session reload — the discovery script doesn't re-read this file mid-flow; defer the reload to after commit.
3. Confirm target version (default: the row's `Proposed`).

Multi-row packages (TFM-conditional + `VersionOverride` sites) get **one ask per package** — the decision applies to all of that package's rows. Same for packages whose conditional rows reference a shared `<PropertyGroup>` property.

Output of this step: a per-package decisions map with version targets + per-package audit flags (which packages need fuget API diff, which need polyfill review, etc.).

### 4. Pre-build audit phase

Run all audits that can be done from package metadata alone, **before** any file is edited. Each audit may queue additional file edits (`.editorconfig` lines, code dedups, etc.) that get batched into the apply step.

#### 4a. Analyzer rule-catalog catch-up

For every analyzer package being bumped (Meziantou.Analyzer, NUnit.Analyzers, Microsoft.CodeAnalysis.*Analyzers, AsyncFixer, Lindhart.Analyser.*, etc.):

1. Fetch the package's release-notes URL between `current` and `target` versions.
2. Extract the list of new rule IDs added in that version range (e.g. for Meziantou: `MAxxxx` entries; for NUnit.Analyzers: `NUnitxxxx`).
3. For each new rule, ask the user: **enable as error**, **enable as suggestion**, or **disable (severity = none)**. Record the choice as a queued `.editorconfig` edit (numerically ordered insert).
4. **First-time-this-package update only:** also catch up on missed older rules — diff the package's full rule catalog at `target` against `.editorconfig`; for each rule absent from `.editorconfig`, ask the user the same question. Apply existing exceptions (rules already explicitly enabled or disabled stay).

This replaces the old reactive "build → see what fires → ask per rule → re-build" cycle. Predictive answers from the changelog let the build pass on first try.

#### 4b. Polyfill new-APIs review

For every polyfill package being bumped (Meziantou.Polyfill, etc.):

1. Pull the README diff between `current` and `target` (or the package's per-version API list).
2. Extract the list of newly polyfilled APIs.
3. For each new API, search the linq2db codebase for our own polyfill or `#if`-conditional implementation of the same API. If found, propose **deletion** of the internal copy (queued code edit).
4. Always show the full new-APIs list to the user even when no internal duplicate exists — they may want to opt in to a new polyfill somewhere.

#### 4c. Fuget API-surface diffs

For every package flagged with the *Fuget API-diff procedure* in `nuget-package-notes.md`:

1. Fetch the API-surface diff between `current` and `target` from the user's fuget server (URL in [`external-repos.md`](../../docs/release/external-repos.md)).
2. Apply the package's diff exclusions (per-package list under `**API-diff exclusions:**` in `nuget-package-notes.md`) — strip those namespaces / types from the rendered diff.
3. Show the filtered diff to the user. They decide: absorb the API change (keep the bump), revert the bump (drop the package from the apply list), or add new exclusions (record in `nuget-package-notes.md`).

#### 4d. Cross-package drift checks

For every package with a recorded drift-alert rule (e.g. `linq2db4iSeries` mirrors the linq2db version it targets — alert when drift exists between its target version and the linq2db release we're shipping):

1. Read the drift-check predicate from the package's `nuget-package-notes.md` entry.
2. Compute the drift (e.g. compare provider-API surface between linq2db `<dep-target-version>` and the about-to-release linq2db version).
3. Surface to user. They decide: ship anyway, hold the release until the upstream catches up, or pin our consumer to a workable older version.

#### 4e. Other predictive checks

Any package-specific pre-check captured in `nuget-package-notes.md` (e.g. TFM-raised detection per *Lowest-supported-TFM detection*) runs here. Outputs are queued edits or user decisions, not file mutations.

#### 4f. Security-advisory / provenance check

A dependency bump is a supply-chain entry point — a compromised, typosquatted, or yanked-then-relisted version pulls hostile code into the shipped product (the LiteLLM infostealer incident is the canonical failure this guards against). For every package being bumped:

1. Check the **target version** for known vulnerabilities. NuGet surfaces GitHub Advisory data — run `dotnet list package --vulnerable --include-transitive` against the post-edit tree once the apply lands (or query the advisory feed for the target version during the walk). Any target version carrying a known advisory is a blocking user decision.
2. Watch for **provenance red flags** the discovery script can't see from version numbers alone: an unexpected package owner / author change between `current` and `target`, a version that was unlisted-then-relisted, or a sudden major expansion of the transitive dependency tree.
3. A flagged version is a **user decision**, not an auto-revert: pin to the last clean version, hold the bump, or accept with the justification recorded in `nuget-package-notes.md`.

This upgrades the "vulnerable: not detected by discovery" gap from step 1 into an explicit predictive audit. It does not replace the verification build — it runs from package metadata + the advisory feed, before the apply, like the other 4x audits.

### 5. Batch apply

Apply all queued edits in one batched pass, grouped by file:

- `Directory.Packages.props`: one batched edit covering every updated `<PackageVersion>` line + new `<PropertyGroup>` properties + condition cleanups + TFM-split additions.
- `Tests/Tests.T4.Nugets/Directory.Packages.props`: separate edit if any of its packages are in scope.
- Each `VersionOverride` csproj: one edit per file.
- `.editorconfig`: one edit covering every new rule severity line from step 4a (numerically ordered inserts).
- Any code edits queued from step 4b (polyfill dedups) or other audits.

Show the full diff as a single proposal. Gate on user confirmation before any edits land.

### 6. Mark deps task done — do **not** commit, do **not** push

After step 5's apply lands all queued edits, this skill's interactive work is complete. Update state:

```
pwsh -NoProfile -File .claude/scripts/release-state.ps1 -Action update -Version <ver> -TaskId 1 -Status done -Annotation "<N updates, M skipped, K blocked>"
```

**Do not commit.** The release-prep cycle uses **one** consolidated commit at the end (made by `/release-verify` task 6 after the build proves stable). All release-prep tasks — deps / PublicAPI / milestone-check / test-matrix / release-notes / ad-hoc — leave their changes on the working tree of `release-prep/<ver>` and let `/release-verify` stage everything together. Reasons:

- One commit means one push, one CI trigger, one PR diff to review. Each task committing separately would create commits whose individual states may not even build (e.g. deps lands an analyzer-rule-disabling .editorconfig but the build proves the disable was wrong; a separate verification commit then has to undo it).
- The orchestrator-level verification (`/release-verify`) sees the cumulative state and can adjust before commit. Separate task commits force the verification fix into a follow-up commit on top of broken intermediate commits.

**Do not push.** No deps push happens in this skill. Push is owned by `/release-verify`.

**Do not trigger CI.** No CI trigger here. CI fires once after `/release-verify` pushes the consolidated commit.

### 7. Hand back to `/release` orchestrator

The orchestrator records the deps task as `[x]` and proceeds to the next release-prep task. The worktree carries the deps changes uncommitted forward into the next task.

If a build regression escapes the predictive audits in step 4, `/release-verify` step 2's reactive walk handles it before commit. There's no need for a deps verification build inside this skill.

## Don'ts

- Do **not** silently update a shipping-package `Microsoft.Extensions.*` / `System.*` past its pinned-version policy. The pin exists to keep downstream consumers free of transitive constraints — see [issue #3953](https://github.com/linq2db/linq2db/issues/3953) referenced in `Directory.Packages.props`.
- Do **not** auto-include prerelease versions in shipping packages. Always warn + require explicit re-confirmation.
- Do **not** edit `linq2db.t4models` from this skill outside the override case where the user explicitly asks. The default ownership of that bump is `/release-postpublish` step 4.
- Do **not** batch multiple unrelated package updates into one review — they're unrelated work and each has its own release-notes / risk profile. Per `agent-rules.md` → **Do not batch code-change reviews**. Step 3's per-package walk and step 5's batched apply are the same logical change (one cohesive deps update), so the per-edit-confirmation rule does not split this single apply.
- Do **not** commit, push, or trigger CI from this skill. All of those are owned by `/release-verify` after the build proves the consolidated release-prep state is sound.
- Do **not** run a verification build inside this skill. Trust the predictive audits; the orchestrator's `/release-verify` step runs the only build of the release-prep cycle.
