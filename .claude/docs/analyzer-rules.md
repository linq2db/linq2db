# Working with analyzer rules (Roslyn / Meziantou)

linq2db enforces a large analyzer set (Meziantou.Analyzer + Roslyn CA/IDE) configured in the root `.editorconfig`. Rules to fix or re-evaluate accumulate during release prep (`/release-verify` flips newly-shipped rules to `error` and walks the failures) and land as cleanup issues (e.g. #5532). This doc covers how the config is organized and how to change a rule safely.

## `.editorconfig` organization

The `[*.{cs,vb}]` section has three relevant regions:

- **Catch-up catalog** — every enforced rule as `severity = error`, with deferred / disabled ones sitting inline at `none` plus a justification comment. New analyzer-package rules are auto-added here as `error` and walked during `/release-verify`.
- **"inactive diagnostics (explicitly configured)"** — older rules permanently `none`, grouped by reason headers (`# too aggressive`, `# Lots of false positives`, `# slow`, …).
- **`[Tests/**]` override** — rules relaxed to `none` for test code only (e.g. MA0111, MA0150, MA0186), so source can enforce a rule without forcing test code to comply.

To **enable / fix** a rule: set `= error` (and move it out of the inactive section into the catalog if it lived there); if test code shouldn't be forced to comply, add `<rule> = none` to `[Tests/**]`.

To **disable** a rule: keep `none` and replace any deferral marker (e.g. `(see #5532)`) with a permanent justification — match the terse style of the neighbouring disables.

### Rules blocked on #2502 (mapping-attribute decoupling)

Several Meziantou rules can't be cleanly enabled because the mapping attributes (`ColumnAttribute`, `AssociationAttribute`, `ExpressionMethodAttribute`, …) double as fluent-mapping DTOs — they carry members typed `Expression` / `LambdaExpression` / `Delegate` / `IValueConverter` / nullable-enum set via `FluentMappingBuilder`, never as attribute literals. **MA0170** ("Type Cannot Be Used In An Attribute Parameter") is disabled for exactly this reason, with a "revisit after #2502" marker. Issue #2502 ("Decouple model attributes from model metadata", open) tracks migrating the internal mapping representation off these attributes onto dedicated metadata types; the rule disables they force should ease once it lands. When touching mapping attributes or this config, check whether #2502 has progressed.

## Verifying a rule change — full-solution build, always

Verify an enable/fix with a **full-solution** Release build, never a single project:

    dotnet build linq2db.slnx -c Release

- Dependent projects (e.g. `LinqToDB.EntityFrameworkCore`) have their own sites the core project doesn't.
- **A failing core project hides its dependents' sites.** MSBuild skips compiling the dependents of a project that errored, so their violations don't appear until core is clean — fixing core then surfaces a fresh batch. (In #5532, core's 79 MA0186 sites masked EF.Core's 3 until core built green.)
- A changed `.editorconfig` severity invalidates the Roslyn analyzer cache for **every** project, so an *incremental* full-solution build re-analyzes everything — a clean rebuild isn't required (an incremental build was accepted as authoritative in #5532).
- Analyzers run in **Release only** (see `CLAUDE.md`). Debug/Testing builds surface no analyzer diagnostics, but they *do* compile `#if DEBUG` code — useful for compile-checking a debug-only fix the Release build can't see.

Disabling a rule needs no build: severity stays `none`, so build state is unaffected (only the comment changes).

## Enumerating a rule's sites

Flip the rule to `= error` and build — the build fails listing every site. Use `error`, **not** `warning`: with `TreatWarningsAsErrors` + `--verbosity quiet`, errors are the reliable signal and the build stops cleanly. Dedupe by `file:line` (each site repeats once per TFM). For speed, enumerate in the core project first; the full-solution verify build catches the dependent-project remainder.

## Bulk-applying a mechanical fix

For a uniform rule with many sites (e.g. MA0186, 82 sites), apply the analyzer's own code fix instead of hand-editing:

    dotnet format <project.csproj> analyzers --diagnostics <ID>

**Gotcha:** `dotnet format` inserts the fix with *minimal per-file qualification* — `[System.Diagnostics.CodeAnalysis.NotNullWhen(true)]`, or `[Diagnostics.CodeAnalysis.…]` when only `using System;` is present, or the short form when the namespace is already imported. linq2db's convention is the **short form + `using`**, so normalize afterward: replace the qualified attribute with the short form and add a sorted `using` (create a new System group when the file has none — `dotnet_separate_import_directive_groups = true`). Preserve CRLF + BOM (`end_of_line = crlf`). Run `dotnet format` on each project that has sites (the full-solution verify build reveals dependent-project sites you must also fix).
