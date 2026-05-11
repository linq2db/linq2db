---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
---

# IDE analyzers, ReportAnalyzer, and XML-doc generation gated to Release builds only

## Context

The repo defines four MSBuild configurations: `Testing`, `Debug`, `Release`, and `Azure`. Before PR #5516, `EnforceCodeStyleInBuild` (which enables IDExxxx style analyzers during build), `ReportAnalyzer` (per-analyzer cost profiling), and `GenerateDocumentationFile` (XML doc generation, also required for xml-doc validation) were either always enabled or not consistently gated. `RunAnalyzersDuringBuild` was already gated to `Release` only. The inconsistency meant that IDExxxx analyzers and XML-doc generation ran in `Testing` and `Azure` configurations, slowing the fast-iteration single-TFM build used in development and CI provider tests.

## Decision

Commit `81db2d711` (PR #5516, 2026-05-09) tightened all four properties to use a strict `== 'Release'` condition in `Directory.Build.props`:

- `RunAnalyzersDuringBuild` -- already `== 'Release'`; confirmed.
- `EnforceCodeStyleInBuild` -- changed to `== 'Release'`.
- `ReportAnalyzer` -- remains `false` unconditionally (debugging-only toggle).
- `GenerateDocumentationFile` -- changed to `== 'Release'`.

The `!= 'Debug'` form was explicitly rejected because it would still fire in `Testing` and `Azure` configurations, contrary to the intent.

## Why

`Testing` is the primary fast-iteration configuration (single TFM, used by most CI provider-matrix jobs). Running IDExxxx analyzers and XML-doc generation there added measurable overhead. `Release` is the only configuration where both analyzer correctness and full doc validation are required (NuGet packaging, ApiCompat baseline checks, banned-symbol enforcement).

## Consequences

- In `Debug`, `Testing`, and `Azure` builds: no IDExxxx style diagnostics from MSBuild, no XML doc file generated. Live IDE analysis (`RunAnalyzersDuringLiveAnalysis`) is unaffected and remains enabled.
- In `Release` builds: IDExxxx analyzers run, `GenerateDocumentationFile` is true, and XML-doc validation errors surface.
- Any new property added to `Directory.Build.props` that should be Release-only must use `Condition="'$(Configuration)' == 'Release'"` -- see `Directory.Build.props:110` as the canonical pattern.

## Sources

- Commit `81db2d711` -- Gate IDE analyzers, ReportAnalyzer, and xml-doc to Release builds only (#5516) (MaceWindu, 2026-05-09)
- PR #5516
- File anchors: `Directory.Build.props` (Code Analysis Setup property group, lines ~107-121)
