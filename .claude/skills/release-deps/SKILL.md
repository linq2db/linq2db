---
name: release-deps
description: NuGet dependency update for the release-prep workflow. Discovers every <PackageVersion> in Directory.Packages.props plus all <PackageReference VersionOverride="..."> sites across the repo, queries nuget.org for available versions, applies the project's version policy (shipping packages pin runtime libs to their initial .NET version; test-only references float to latest; analyzers may use prereleases), and renders a single table the user picks updates from. Per-package custom rules + release-notes URLs accrue in `.claude/docs/release/nuget-package-notes.md`. Invoked by `/release` step 1 or directly when running release prep.
---

# /release-deps

## What this skill is (and isn't)

**Is:** the dependency-update phase of release prep. One discovery pass produces a table of every package with `current`, `latest-release`, `latest-prerelease`, `proposed`, and any `policy:` block reason. User picks updates with a batch selection. For each selected package without a recorded rule, the skill walks per-package to capture release-notes URL + any custom rule. Apply is a batched `Edit` across `Directory.Packages.props` + per-csproj `VersionOverride` sites.

**Isn't:**

- Not for normal-development dependency updates. Those go through PRs targeted at the affected component.
- Not for adding new packages — adding a new `<PackageVersion>` is a code change unrelated to release prep.
- Not for the `linq2db.t4models` self-reference — that gets bumped post-release by [`release-postpublish`](../release-postpublish/SKILL.md) step 4 to the just-tagged version, not by this skill.

## When to run

- During release prep as task 1 (called by `/release` orchestrator).
- Manually when the user wants a one-off dependency-update pass outside a release (rare).

## Required reading

- [`.claude/docs/release/nuget-package-notes.md`](../../docs/release/nuget-package-notes.md) — accrued per-package rules; consulted before rendering the table.
- [`.claude/docs/release/branch-and-pr.md`](../../docs/release/branch-and-pr.md) — branch + PR conventions.
- [`.claude/docs/ci-tests.md`](../../docs/ci-tests.md) — for the post-apply `/azp run test-all` trigger.

## Procedure

### 1. Discover

```
pwsh -NoProfile -File .claude/scripts/release-deps-discover.ps1 -Action discover -Version <ver>
```

The script:

- Parses `<PackageVersion>` entries in `Directory.Packages.props`. Resolves `$(PropertyName)` references using the file's `<PropertyGroup>` properties.
- Greps `VersionOverride="..."` across `.csproj` / `.props` (rediscovered each run — locations may shift).
- Identifies **shipping** vs **test-only** packages: a package is *shipping* if any `Source/**/*.csproj` has a `<PackageReference Include="<id>" />` for it.
- Queries `https://api.nuget.org/v3-flatcontainer/<id>/index.json` for each unique package (parallel fan-out, cached at `.build/.claude/release-<ver>-deps-cache/<id>.json`).
- Applies policy:
  - **runtime-pin** (shipping + Id matches `^(System\.|Microsoft\.Extensions\.|Microsoft\.AspNetCore\.|Microsoft\.Bcl\.)` + current is an initial `.0` version): keep current. Surface latest-release as a *blocked* update with reason `policy:runtime-pin`.
  - **shipping-prerelease**: if a candidate target version is prerelease, mark the row blocked unless user explicitly overrides.
  - **vulnerable**: not auto-detected by the flatcontainer endpoint. Capture this as a first-run TODO if a vulnerability surfaces — for now, the user manually flags it.
- Emits a plan JSON at `.build/.claude/release-<ver>-deps-plan.json`.

Conditional `<PackageVersion>` entries (TFM-bracketed via `Condition="..."`) are surfaced as separate rows with the condition shown, since they need per-condition reasoning.

### 2. Render the table

The agent reads the plan and renders one numbered row per package. Per [R2-C] the table includes packages with available updates blocked by policy — visible so the user can override.

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
- `Rule` (✓ / —): does `nuget-package-notes.md` have a custom rule recorded for this package?
- `Notes` (✓ / ?): is a release-notes URL recorded?
- `Reason`: populated only for blocked rows.

### 3. First gate — selection

Ask the user once:

> _"Pick rows to update by number (e.g. `1,3,5-9`), `all stable`, `all`, or `none`. To override a policy-blocked row, name it explicitly (`4`). Shipping-package prereleases are prohibited by default — naming a row whose proposed is prerelease will surface a separate confirmation."_

If the user names any prerelease-proposed shipping row, surface an explicit warning before continuing — per user rule, prerelease shipping packages create downstream consumer issues.

### 4. Second gate — per-package walk for selected packages without a rule

For each selected row where `Rule: —`, walk one at a time. For each:

1. Show release notes between current and target version. If `notes-url-known` is `✓`, fetch the URL via WebFetch (or open it in browser for user reading). Else ask user: _"where are release notes for `<package>`?"_ — record the URL in `nuget-package-notes.md` on confirmation.
2. Ask if any custom update rule applies (e.g. co-bump with another package, post-update codegen rerun, downgrade-allowed-for-compat). On non-trivial answer:
   - Append a structured entry to `nuget-package-notes.md` (per its schema header).
   - Tell the user: 📌 _"Reload session to pick up new nuget-package-notes entries before continuing."_ — and stop until they reload.
3. Confirm target version (default: the row's `Proposed`).

For rows where `Rule: ✓`, the recorded rule may dictate target version or co-bumps directly — apply automatically and skip the walk.

### 5. Apply

Edit each affected file with the new versions:

- `Directory.Packages.props`: one batched `Edit` call covering every updated `<PackageVersion>` line (single Edit avoids per-line permission prompts — same pattern as `/version-bump`).
- Each `VersionOverride` csproj: one Edit per file. Group by file in a single message.
- Tests/Tests.T4.Nugets/Directory.Packages.props if any of its packages are in scope: separate Edit.

Show the full diff. Gate on user confirmation before any edits land.

### 6. Commit

After apply:

1. Update state: `release-state.ps1 -Action update -Version <ver> -TaskId 1 -Status done -Annotation "<N updates, M skipped, K blocked>"`.
2. Stage the modified files. Commit message format:
   ```
   Release <ver>: dependency updates

   - <Package> <old> -> <new>
   - <Package> <old> -> <new>
   - ...

   Skipped (user choice): <ids or "none">
   Blocked by policy: <ids or "none">
   ```
3. Push **on explicit user confirmation** (push semantics rule from `/release` Conventions).

### 7. Trigger CI test-all

After push, post `/azp run test-all` on the prep PR via `.claude/scripts/azp-run.ps1`. This kicks off the full provider matrix to catch regressions while other prep work continues.

Record `state.deps.lastCiTrigger = <iso-timestamp>` via:

```
pwsh -NoProfile -File .claude/scripts/release-state.ps1 -Action update -Version <ver> -TaskId 1.ci -Status done
```

(If `1.ci` isn't a known sub-task ID, the orchestrator's state schema allows adding ad-hoc sub-task tracking — alternative: store the timestamp in the task's `annotation` field.)

## Don'ts

- Do **not** silently update a shipping-package `Microsoft.Extensions.*` / `System.*` past its pinned-version policy. The pin exists to keep downstream consumers free of transitive constraints — see [issue #3953](https://github.com/linq2db/linq2db/issues/3953) referenced in `Directory.Packages.props`.
- Do **not** auto-include prerelease versions in shipping packages. Always warn + require explicit re-confirmation.
- Do **not** edit `linq2db.t4models` from this skill. That's owned by `/release-postpublish` step 4 (it gets bumped to the just-released version *after* tagging).
- Do **not** batch multiple unrelated package updates into one review — they're unrelated work and each has its own release-notes / risk profile. Per `agent-rules.md` → **Do not batch code-change reviews**.
- Do **not** push without an explicit user request. The CI trigger in step 7 follows the explicit-push confirmation from step 6.
