---
name: dogfood-analyzer
description: Dogfood a shipped linq2db analyzer/code-fix rule against the real test corpus — build it as a package, attach it to Tests/Linq, run it in report mode (no crash / no false positives / no missed sites) and code-fix mode (compiles / trivia preserved / every convertible site rewritten / skipped sites justified). Use when the user says "dogfood-analyzer", "/dogfood-analyzer", "dogfood the analyzer", or "run the analyzer over the tests". Read-only on the primary clone; all work happens in a throwaway worktree and is never committed.
---

# dogfood-analyzer

Validates a **user-facing** `linq2db.Analyzers` rule against real, varied, clustered code — the coverage unit fixtures (one diagnostic each) can't give. Canonical procedure, rationale, and every gotcha live in [`authoring-analyzers.md`](../../docs/authoring-analyzers.md) → **Dogfooding a rule against the real corpus**; this skill drives it and reports findings. It caught the #5703 `BatchFixer` under-application (6/266 applied) that 45 green unit tests missed, and quantified a return-type-decline cluster into an opt-in option.

## When to run

After adding or changing an analyzer rule (the final step of `/create-analyzer`), or on explicit request. The analyzer projects must exist on the target branch (merged, or checked out in a worktree). Draft/unmerged is fine — dogfood the branch.

## Inputs (resolve first; ask only if underivable)

- **`<rule-id>`** — e.g. `L2DB1001`.
- **consumer project** — default `Tests/Linq` (the richest window/analytic corpus; `AnalyticTests.cs` alone exercises nearly every shape). Another project only if the rule targets APIs used elsewhere.
- **has-code-fix** — does the rule ship a `CodeFixProvider`? (governs whether step 4 runs).
- **legacy-usage oracle** — a `Grep` regex for the API the rule flags (e.g. `Sql\.Ext\.(RowNumber|Rank|…|PercentileDisc)` reaching `.Over()`…`.ToValue()`), used in step 3 to detect missed sites. Derive from the rule's detection anchor; confirm with the user if ambiguous.

## Steps

Everything runs in a **throwaway worktree** and is **never committed** — the code-fix pass mutates tracked test source and can regenerate SQL baselines. Confirm worktree creation per [`worktree.md`](../../docs/worktree.md).

### 1. Scratch worktree

`git worktree add --detach ../<clone>.dogfood origin/<analyzer-branch>` (base on the *remote* ref so it includes the latest pushed commits). Work there.

### 2. Pack + attach the analyzer

- **Pack:** `dotnet pack <worktree>/Source/LinqToDB.Analyzers.CodeFixes/LinqToDB.Analyzers.CodeFixes.csproj -c Release -p:VersionSuffix=-dogfood<n>`. The leading `-` is required — `PackageVersion` is `$(Version)$(VersionSuffix)` concatenated (a bare `dogfood1` yields the invalid `6.4.0dogfood1`). Output lands in `<worktree>/.build/package/release/`.
- **Purge the extraction cache** so a re-pack of the same version isn't served stale: remove `~/.nuget/packages/linq2db.analyzers` (PowerShell tool).
- **Local feed** — add to the worktree `nuget.config`: `<add key="linq2db-testing" value=".build\package\release\" />` and a `packageSourceMapping` `<package pattern="linq2db.Analyzers" />` → that feed.
- **Reference from the consumer csproj** (the repo uses Central Package Management, so use `VersionOverride`): `<PackageReference Include="linq2db.Analyzers" VersionOverride="6.4.0-dogfood<n>" />`. If `dotnet format` later reports 0 diagnostics despite report mode finding sites, fall back to a direct `<Analyzer Include>` of the two built DLLs (`.build/bin/LinqToDB.Analyzers.CodeFixes/Release/{LinqToDB.Analyzers,LinqToDB.Analyzers.CodeFixes}.dll`) — `dotnet format` does not reliably load a `developmentDependency` package analyzer.

### 3. Report mode (crash / false positives / missed sites)

- Scratch worktree `Tests/<consumer>/.editorconfig`: `[*.cs]` → `dotnet_analyzer_diagnostic.severity = none` **and** `dotnet_diagnostic.<ID>.severity = warning` — silences every *other* analyzer so only the rule under test surfaces.
- Build with analyzers forced (they're off outside Release): `dotnet build <consumer>.csproj -c Testing -t:Rebuild --no-dependencies -p:RunAnalyzersDuringBuild=true -p:TreatWarningsAsErrors=false` (do a plain `-c Testing` build first to warm dependencies). Redirect to a log.
- Parse the `warning <ID>` lines → unique `file(line,col)` set. **Reconcile against the oracle grep** over the consumer: oracle-only = **missed site**; analyzer-only that isn't a real target = **false positive**; a build/analyzer crash or unhandled exception = **top-priority finding**. The analyzer reports at the chain-start line, so line-broken chains (`Sql.Ext\n.Lag(...)`) may under-count in the oracle — verify those by eye before calling them a discrepancy.

### 4. Code-fix mode (compiles / trivia / all-convertible-applied / skips justified)

Skip if the rule has no code fix. `dotnet format` traps (each cost a cycle on #5703):

- **Native back-slash project path**, via the **PowerShell tool** (not Git-Bash): `dotnet format 'C:\…\<consumer>.csproj' analyzers --diagnostics <ID> --severity info`. A forward-slash path fails to match MSBuild's `FilePath` → every project is "referenced" and skipped → *"Formatted 0 of 0 files"*.
- **Single-TFM the consumer** for this step (scratch `<TargetFramework>net10.0` + empty `<TargetFrameworks>`) — a multi-TFM project has every TFM-instance skipped as "referenced".
- **Drop the `= none` bulk** from the step-3 `.editorconfig` (leave only the per-ID `warning`) — the bulk `none` makes `dotnet format` skip loading the analyzer.
- `--severity info` is required for an `Info`-default rule (else the fix no-ops).

Then judge:
- **Rebuild** the fixed corpus (`-c Testing --no-dependencies`) → must compile (unless an opt-in option intentionally introduces type errors — see below).
- **`git diff`** → comments/formatting intact, no swallowed tokens/`;`, no over-collapsed multi-line chains.
- **Re-run report mode** → the **remaining** diagnostics are the skipped sites. Each must be a genuine no-fix decline (return-type divergence, non-constant modifier, unrecognized/non-convertible shape), **not** a convertible site the fix wrongly skipped. **A partial subset is a red flag, not an acceptable outcome** — if convertible sites are skipped, that's a code-fix defect (the #5703 case was `WellKnownFixAllProviders.BatchFixer` dropping physically-clustered edits; fix = a custom `DocumentBasedFixAllProvider`, see `authoring-analyzers.md`). Prove the fix with a **≥3-adjacent-occurrences** unit test (the Roslyn SDK's Fix-All leg).

### 5. Bailout clusters → analyzer-option candidate

When the surviving declines concentrate on **one** cause (e.g. all return-type-divergence), that's a signal to propose a **configurable, default-off** opt-in that overrides that specific bail (as #5703 added `linq2db.<ID>.apply_fix_on_return_type_mismatch`). **Quantify it** — "N of M declines are cause X" — in the report; don't silently accept the bailouts. If such an option exists, dogfood it too: enable it in the scratch `.editorconfig`, re-run step 4, and report how many more sites convert and **what compile errors (if any) it introduces** (on #5703, ~116 more converted, only 2 hard `CS1929` — the rest were inferred-context divergences that compile).

### 6. Report + teardown

One structured report: report-mode reconciliation (sites / false positives / missed / crash), code-fix outcome (converted / total, compiles?, trivia intact?), skipped-site classification, and any option candidate with counts. Frame "found nothing / blocked" as a valid outcome. Then remove the worktree (confirm per `worktree.md`); verify the primary clone `git status` shows nothing from the dogfood.

## Don'ts

- Never commit or push anything from the dogfood worktree (mutated test source + regenerated baselines are all scratch).
- Don't treat a partial code-fix apply as success — drive **every** convertible rewrite or root-cause why not.
- Don't skip the report-mode reconciliation — a green code-fix pass doesn't prove the analyzer flags the right set.
