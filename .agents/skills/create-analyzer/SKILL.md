---
name: create-analyzer
description: Scaffold a new user-facing Roslyn analyzer (and optional code fix) rule in the linq2db.Analyzers package. Adds the DiagnosticAnalyzer to Source/LinqToDB.Analyzers, an optional CodeFixProvider to Source/LinqToDB.Analyzers.CodeFixes, an AnalyzerReleases.Unshipped.md entry with the next LINQ2DB1xxx id, and a Tests/Tests.Analyzers fixture skeleton. Use when the user says "create-analyzer", "/create-analyzer", "add an analyzer rule", or "new linq2db analyzer".
---

# create-analyzer

Scaffolds a new rule in the `linq2db.Analyzers` package. The package's structure, conventions, and gotchas are the source of truth in [`.agents/docs/authoring-analyzers.md`](../../docs/authoring-analyzers.md) — read it first; this skill only wires up the boilerplate consistently.

## When to run

When the user asks to add a new user-facing analyzer/code-fix rule. If the `Source/LinqToDB.Analyzers` project doesn't exist yet, this is the first rule — follow `authoring-analyzers.md` § "Project layout" to create the two projects + test project before scaffolding the rule.

## Steps

1. **Pick the id.** Scan `Source/LinqToDB.Analyzers/AnalyzerReleases.{Shipped,Unshipped}.md` for the highest `LINQ2DB1xxx` in use; the new id is the next free number (user-facing rules are `1xxx`; `0xxx` is reserved for internal `CodeGenerators` rules). Confirm the id, title, category, and default severity with the user (Info for "prefer the new API" style rules while the old API is still supported).

2. **Analyzer.** Add a `sealed class <Name>Analyzer : DiagnosticAnalyzer` to `Source/LinqToDB.Analyzers` — model it on `WindowFunctionApiAnalyzer`: `public const string DiagnosticId`, an `internal static DiagnosticDescriptor Rule` (with `helpLinkUri` to the wiki page), `EnableConcurrentExecution()` + `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`, symbols resolved once in `RegisterCompilationStartAction`, and the narrowest `RegisterOperationAction`/`RegisterSyntaxNodeAction` with a cheap gate first. Honor the performance checklist. Use `ImmutableArray.Create(...)` (not `[...]`) and MA-clean code (Roslyn 4.8 + Meziantou — see the doc's gotchas).

3. **Code fix (optional).** If the rule is auto-fixable, add a `[ExportCodeFixProvider][Shared] sealed class <Name>CodeFixProvider : CodeFixProvider` to `Source/LinqToDB.Analyzers.CodeFixes`, referencing the analyzer's `DiagnosticId`. Follow the trivia-preservation and code-fix-correctness checklists (placeholder + `NormalizeWhitespace` + splice originals; expression-tree safety; return-type guard). Register no fix (return without `RegisterCodeFix`) for cases with no safe mechanical rewrite — the diagnostic still reports.

4. **Release tracking.** Add the rule to `AnalyzerReleases.Unshipped.md` (`Rule ID | Category | Severity | Notes`).

5. **Tests.** Add a `Tests/Tests.Analyzers` fixture using the `AnalyzerVerifier`/`CodeFixVerifier` shims: positive + negative detection, and (if a fix) per-case before/after, a trivia battery, and an expression-tree case. Run via `dotnet test Tests/Tests.Analyzers -c Debug`; verify a Release build of the projects is clean (RS + Meziantou rules).

6. **Docs.** Note the follow-up: a wiki page for the new id (the `helpLinkUri` target) and a `readme.md` row, done post-merge per the defer-docs rule.

Do **not** commit — commits require an explicit user request (`.agents/docs/agent-rules.md` → Git commit rules).
