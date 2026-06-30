---
name: profile-analyzers
description: Measure Roslyn analyzer build-time cost across the linq2db solution. Runs a dedicated rebuild with `/reportanalyzer`, parses the detailed log, and produces top-N rankings (analyzer x project, analyzer totals, project totals). Use when the user asks to profile analyzers, says "analyzers got slow", "which analyzers are slowest", "why is the build slow", or as part of `/release-deps` after an analyzer-package bump to spot regressions.
---

# /profile-analyzers

## What this skill is (and isn't)

**Is:** the analyzer-perf-profiling phase. One full Release rebuild with `-p:ReportAnalyzer=true -v:detailed` produces a per-project per-analyzer time table; the parser ranks (analyzer x project) pairs, total per-analyzer time, and total per-project time. Used to decide whether an analyzer-package bump regressed the codebase's build time.

**Isn't:**
- Not the verification build. The verification build (`dotnet build linq2db.slnx -c Release`) only checks that compilation passes; this skill is on top of that, with extra MSBuild flags to surface the analyzer report. Wall-clock cost: 10-25 min on `linq2db.slnx`.
- Not for CI. CI already runs the full matrix; this is local diagnostic work.
- Not auto-triggered. Always explicit user request — including from `/release-deps`, the orchestrator must ask before invoking.

## When to run

User-invoked. Good triggers:

- `analyzers got slow`
- `which analyzers are slowest`
- `profile the build`
- `why is the build slow`
- explicit `/profile-analyzers`
- after `/release-deps` bumped any analyzer package (Meziantou.Analyzer, NUnit.Analyzers, Microsoft.CodeAnalysis.*Analyzers, AsyncFixer, Lindhart.Analyser.MissingAwaitWarning, etc.) — `/release-deps` proposes invoking this skill at the end of the verification-build phase. The user still confirms.

Skip if the user only wants to measure one project — a plain `dotnet build <project.csproj> -p:ReportAnalyzer=true -p:UseSharedCompilation=false -v:detailed` gives the same report for that one project without the full rebuild.

## Non-obvious MSBuild flags

Roslyn emits the `/reportanalyzer` summary through `csc.exe`'s stdout. Four things interact to make the report invisible at defaults:

1. **MessageImportance.Low** — MSBuild logs each line from csc at Low importance, which `-v:normal` filters out. **Use `-v:detailed`**.
2. **VBCSCompiler (shared compilation)** can swallow the report (the output is returned in the compile response, not written to stdout MSBuild captures). **Use `-p:UseSharedCompilation=false`**.
3. **Incremental** skips `CoreCompile` for up-to-date projects and no report is emitted for them. **Use `-t:Rebuild`** to measure the whole solution.
4. **Enable the report itself:** `-p:RunAnalyzersDuringBuild=true -p:ReportAnalyzer=true`.

The provided scripts already pass all four.

## Required reading

- [`.agents/docs/agent-rules.md`](../../docs/agent-rules.md) → **Bash command rules**, **Running tests** (we're not running tests here, but the build-vs-test separation is the same).
- [`.agents/docs/release/nuget-package-notes.md`](../../docs/release/nuget-package-notes.md) → analyzer-package rules (when an analyzer regresses, the per-package note may capture the workaround).

## Procedure

### 1. Confirm the user wants the long run

Tell the user: "this is a full Release rebuild with `-v:detailed`, expected wall-clock 10-25 min on `linq2db.slnx`. Continue?" Wait for explicit yes. The skill never auto-launches.

### 2. Run the instrumented rebuild

```
pwsh -NoProfile -File .agents/scripts/analyzer-profile-build.ps1 -LogPath .build/.agents/analyzer-build.log
```

Defaults: `-SolutionPath linq2db.slnx`, `-Target Rebuild`. The script:
- Shuts down build servers (`dotnet build-server shutdown`) to force a fresh `csc.exe` run.
- Runs `dotnet build` with the four flags above.
- Writes the full log to `-LogPath`.
- Returns `{ logPath, exitCode, elapsedMs }`.

If the rebuild halts on a pre-existing analyzer error on an unrelated file, pass `-ExtraArgs '-p:TreatWarningsAsErrors=false'`. Then fix the underlying error in a separate change — do not commit `TreatWarningsAsErrors=false` to the repo.

### 3. Parse the log

```
pwsh -NoProfile -File .agents/scripts/analyzer-profile-report.ps1 -LogPath .build/.agents/analyzer-build.log -Top 10
```

Output: three pretty tables on stdout, plus a one-line diagnostics header (per-analyzer rows / project reports / distinct analyzers).

For programmatic consumption: `-AsJson` returns `{ slowestPairs, busiestAnalyzers, projectTotals, diagnostics }` to stdout (see `-Help`).

### 4. Present the three rankings

The agent reads the tables and presents to the user, flagging the dominant offender(s):

- **Top N slowest (analyzer x project) pairs** — where one analyzer is particularly painful in one project.
- **Top N busiest analyzers** — sum across projects; uniformly expensive analyzers rise.
- **Top N projects by total analyzer time** — which projects dominate the build cost.

### 5. (Optional) Compare against the previous baseline

Not implemented yet — there is no committed baseline file. When this skill is invoked from `/release-deps`, surface the report and ask the user to **eyeball-compare against memory** of the prior release's profile. A future iteration may persist a baseline JSON under `.agents/docs/release/analyzer-perf-baseline.json` and produce an automatic delta.

### 6. Disable rules (if user agrees)

Editing `.editorconfig` is a repo-wide change. Before touching it:

- Confirm with the user which rules to disable.
- Only propose disabling rules that are **(a) disproportionately expensive AND (b) not pulling their weight** (false-positive heavy, or redundant with another rule). Expensive-but-valuable rules stay.

When disabling, follow the existing convention in `.editorconfig` (numerically ordered under `###### Meziantou.Analyzers` and the corresponding sections for other analyzer families). Add a short reasoning suffix on the `severity = none` line:

```editorconfig
# CA2000: Dispose objects before losing scope
dotnet_diagnostic.CA2000.severity = none # disabled: slow (~791s/build)
```

Keep the reason terse (slow / noisy / redundant); the total-seconds figure from the report is the durable evidence. After disabling, offer to re-run steps 2-4 to measure the delta.

## Scripts

- `.agents/scripts/analyzer-profile-build.ps1` — shuts down the build servers, runs `dotnet build -t:Rebuild` with the four non-obvious flags, writes the log, returns `{ logPath, exitCode, elapsedMs }`.
- `.agents/scripts/analyzer-profile-report.ps1` — parses the log, emits the three rankings as pretty tables (or JSON via `-AsJson`).

Both follow [`.agents/docs/script-authoring.md`](../../docs/script-authoring.md) conventions.

## Don'ts

- Do **not** auto-run this skill — it's a 10-25 min build and dominates the session.
- Do **not** disable analyzer rules without explicit user approval; `.editorconfig` changes affect every project in the repo.
- Do **not** commit `TreatWarningsAsErrors=false` or any other temporary unblock flag.
- The report is **CPU time across analyzer executions**, not wall-clock. Analyzers run concurrently, so sum-of-times exceeds elapsed build time. The ranking is still valid for comparing rules against each other.
- Expect ~50-70 project reports (not every csproj reaches `CoreCompile` — reference-assembly projects, multi-target with skipped TFMs, etc.). Don't re-invoke the build trying to "get" the missing ones.

## Related

- [`/release-deps`](../release-deps/SKILL.md) — invokes this skill (with user confirmation) after an analyzer-package bump.
- [`/release`](../release/SKILL.md) → step 4 (Test matrix) — wall-clock cost of this skill is comparable to a single test-matrix track.
