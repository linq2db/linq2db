---
name: create-analyzer
description: Scaffold a new user-facing Roslyn analyzer (and optional code fix) rule in the linq2db.Analyzers package. Adds the DiagnosticAnalyzer to Source/LinqToDB.Analyzers, an optional CodeFixProvider to Source/LinqToDB.Analyzers.CodeFixes, an AnalyzerReleases.Unshipped.md entry with the next L2DB1xxx id, and a Tests/Tests.Analyzers fixture skeleton. Use when the user says "create-analyzer", "/create-analyzer", "add an analyzer rule", or "new linq2db analyzer".
---

# create-analyzer

Scaffolds a new rule in the `linq2db.Analyzers` package. The package's structure, conventions, and gotchas are the source of truth in [`.agents/docs/authoring-analyzers.md`](../../docs/authoring-analyzers.md) — read it first; this skill only wires up the boilerplate consistently.

## When to run

When the user asks to add a new user-facing analyzer/code-fix rule. If the `Source/LinqToDB.Analyzers` project doesn't exist yet, this is the first rule — follow `authoring-analyzers.md` § "Project layout" to create the two projects + test project before scaffolding the rule.

## Steps

1. **Pick the id.** Scan `Source/LinqToDB.Analyzers/AnalyzerReleases.{Shipped,Unshipped}.md` for the highest `L2DB1xxx` in use; the new id is the next free number. User-facing analyzer-package rules use the short **`L2DB1xxx`** space (first is `L2DB1001`); the internal `CodeGenerators` build-time analyzer keeps its own separate **`LINQ2DB0xxx`** space (e.g. `LINQ2DB0001`) — don't continue that prefix for user-facing rules. Confirm the id, title, category, and default severity with the user (Info for "prefer the new API" style rules while the old API is still supported).

2. **Analyzer.** Add a `sealed class <Name>Analyzer : DiagnosticAnalyzer` to `Source/LinqToDB.Analyzers` — model it on `WindowFunctionApiAnalyzer`: `public const string DiagnosticId`, an `internal static DiagnosticDescriptor Rule` (with `helpLinkUri` to the wiki page), `EnableConcurrentExecution()` + `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`, symbols resolved once in `RegisterCompilationStartAction`, and the narrowest `RegisterOperationAction`/`RegisterSyntaxNodeAction` with a cheap gate first. Honor the performance checklist. Use `ImmutableArray.Create(...)` (not `[...]`) and MA-clean code (Roslyn 4.8 + Meziantou — see the doc's gotchas).

3. **Code fix (optional).** If the rule is auto-fixable, add a `[ExportCodeFixProvider][Shared] sealed class <Name>CodeFixProvider : CodeFixProvider` to `Source/LinqToDB.Analyzers.CodeFixes`, referencing the analyzer's `DiagnosticId`. Follow the trivia-preservation and code-fix-correctness checklists (placeholder + `NormalizeWhitespace` + splice originals; expression-tree safety; return-type guard). Register no fix (return without `RegisterCodeFix`) for cases with no safe mechanical rewrite — the diagnostic still reports.
   - **Custom Fix-All, not `WellKnownFixAllProviders.BatchFixer`.** `BatchFixer` computes each fix against the original tree and drops physically-clustered edits (multiple flagged sites in one `select new { … }`) — invisible to single-fix tests, but "Fix all"/`dotnet format` then converts only a fraction. Derive a `DocumentBasedFixAllProvider` that rewrites every flagged node in one `ReplaceNodes` pass, and add a **≥3-adjacent-occurrences** regression test (the testing SDK verifies the Fix-All leg). See `authoring-analyzers.md` → *Code-fix correctness checklist* and `WindowFunctionApiCodeFixProvider.WindowChainFixAllProvider`.
   - **Capability gate (when the rule targets a type/API that a version range may lack).** The package ships **no linq2db dependency / version floor**, so it can sit next to an older linq2db. Gate on **symbol presence, not a version** via `Compilation.GetTypeByMetadataName("<FullMetadataName>")` (the package version is invisible to an analyzer; assembly version is unreliable). Gate both directions: the analyzer bails when the *legacy* anchor type is absent; when a code fix rewrites to a *newer* API a too-old linq2db may lack, the fix must **also** `return null` when the rewrite-target type is absent, or it rewrites compiling code into a call to a non-existent member. See `authoring-analyzers.md` § "Capability gate".

4. **Release tracking.** Add the rule to `AnalyzerReleases.Unshipped.md` (`Rule ID | Category | Severity | Notes`).

5. **Tests.** Add a `Tests/Tests.Analyzers` fixture using the `AnalyzerVerifier`/`CodeFixVerifier` shims: positive + negative detection, and (if a fix) per-case before/after, a trivia battery, and an expression-tree case. Run via `dotnet test Tests/Tests.Analyzers -c Debug`; verify a Release build of the projects is clean (RS + Meziantou rules).

6. **Docs.** Note the follow-up: a wiki page for the new id (the `helpLinkUri` target) and a `readme.md` row, done post-merge per the defer-docs rule. Document any code-fix `.editorconfig` option in `readme.md` (and the wiki), including what it does when enabled and any errors it can introduce.

7. **Dogfood.** After the unit fixtures pass, run [`/dogfood-analyzer <id>`](../dogfood-analyzer/SKILL.md) over `Tests/Linq` (the richest real corpus) to validate report mode (no crash / no false positives / no missed sites) and, if there's a code fix, code-fix mode (compiles / trivia preserved / **every** convertible site applied / skips justified). Resolve findings before proposing the PR — dogfooding catches defects unit fixtures miss (the `BatchFixer` under-application on #5703), and a same-cause bailout cluster is a signal to add a default-off opt-in option (step 3).

Do **not** commit — commits require an explicit user request (`.agents/docs/agent-rules.md` → Git commit rules).
