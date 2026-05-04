---
area: TESTS-VB
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 0/0
coverage_tier_2: 4/4
---

# TESTS-VB

VB.NET test helpers verifying that linq2db compiles and produces correct LINQ-to-SQL translation from VB query syntax. Not an NUnit fixture project itself — all functions are standalone helpers called by the C# test suite in `Tests/Linq/`.

## Subsystems

**VB query-syntax helpers** (`VisualBasicCommon.vb`, `CompilerServices.vb`): Four functions exercising VB `From … Where … Select` syntax against `ITestDataContext` / `NorthwindDB`. Covers local-variable capture (`ParamenterName`), compound boolean conditions with VB bitwise-`Or` (`SearchCondition1`), navigation-property `.Count` access (`SearchCondition2`), and VB date-literal syntax `#11/14/1997#` (`SearchCondition3` / `SearchCondition4`). `CompilerServices.vb` specifically exercises `CompareString` — the equality comparison that the VB compiler emits for `p.FirstName = "John"`, which the LINQ translator must handle without confusing it with assignment.

**Issue-regression helpers** (`Tests.vb`): Three variants of a `GROUP BY` with navigation-property key scenario (issue #649): VB `Group By … Into … Select` syntax (`Issue649Test1`), inline aggregation in a `Group By` clause (`Issue649Test2`), and the equivalent fluent-API form (`Issue649Test3`). `Issue2746Test` exercises nullable `CType` coercion (`w.ChildID.Value = CType(SelectedValue, Integer)`) — a VB-specific cast form the expression-tree translator must recognise.

## Key types

| Type / Module | File | Role |
|---|---|---|
| `VisualBasicCommon` | `VisualBasicCommon.vb` | VB query-syntax smoke tests against standard test models |
| `CompilerServices` | `CompilerServices.vb` | Tests VB compiler-emitted `CompareString` equality path |
| `VBTests` | `Tests.vb` | Issue-regression helpers; defines `Activity649` / `Person649` entities inline |
| `Activity649`, `Person649` | `Tests.vb` | Inline entity pair with `[Association]` used only in issue-649 repros |

## Files (Tier 1 / Tier 2)

Tier 1: (none designated)

| File | Tier | Notes |
|---|---|---|
| `Tests/VisualBasic/Tests.VisualBasic.vbproj` | 2 | Targets VB 16.9; refs `Tests.Model` only; inherits TFMs from `Directory.Build.props` |
| `Tests/VisualBasic/CompilerServices.vb` | 2 | `CompareString` helper — VB string-equality compiler emission |
| `Tests/VisualBasic/Tests.vb` | 2 | Issue-649 GROUP BY variants + issue-2746 nullable CType repro |
| `Tests/VisualBasic/VisualBasicCommon.vb` | 2 | Four query-syntax helpers including VB date literal `#…#` |

All 4 files read in full (100% Tier-2 coverage).

## Inbound / outbound dependencies

- **Inbound**: C# test methods in `Tests/Linq/` call these VB module functions directly (interop via `Public` VB modules compiled to a referenced assembly).
- **Outbound → TESTS-MODEL**: imports `Tests.Model` for `ITestDataContext`, `NorthwindDB`, `Person`, `Parent`, `LinqDataTypes`, `GrandChild1`.
- **Outbound → EXPR-TRANS**: VB query syntax compiles to `SelectMany` / `GroupBy` / `Where` method calls plus VB-runtime helpers (`CompareString`, VB `Or` operator). The LINQ-to-SQL translator in [EXPR-TRANS](../EXPR-TRANS/INDEX.md) must handle these without special-casing.
- No dependency on NUnit, `TestBase`, or `[DataSources]` — this project is purely a helper library.

## Known issues / debt

- `ParamenterName` in `VisualBasicCommon.vb:5` contains a typo (`Paramenter` vs `Parameter`) — cosmetic, not functional.
- The project reference chain (`Tests.VisualBasic` → `Tests.Model`) skips `Tests.Base`, so VB tests cannot directly use `DataSourcesAttribute` or `TestBase`. All VB helpers must be invoked from C#.

## See also

- [TESTS-MODEL](../TESTS-MODEL/INDEX.md) — entity types consumed here
- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — translator that processes VB-emitted expression trees

<details><summary>Coverage</summary>

Tier 1: 0 files designated (area has no pinned Tier-1 files).
Tier 2: 4/4 visited (100%).

Read (this run):
- `Tests/VisualBasic/Tests.VisualBasic.vbproj` — project file, VB 16.9, refs Tests.Model
- `Tests/VisualBasic/CompilerServices.vb` — CompareString helper
- `Tests/VisualBasic/Tests.vb` — Issue649 GROUP BY repros + Issue2746 CType
- `Tests/VisualBasic/VisualBasicCommon.vb` — query-syntax + date-literal helpers

Skipped: none.
</details>
