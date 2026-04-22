# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is linq2db

LINQ to DB is a lightweight, type-safe ORM and LINQ provider for .NET. It translates LINQ expressions into SQL — no change tracking, no heavy abstraction. Think "one step above Dapper" with compiler-checked queries.

Repository: https://github.com/linq2db/linq2db — use this when resolving issue/PR numbers via `gh` (e.g. `gh pr view <n> --repo linq2db/linq2db`).

## Build Commands

```bash
# Build the full solution
dotnet build linq2db.slnx

# Build just the core library
dotnet build Source/LinqToDB/LinqToDB.csproj

# Build in Release (enables Roslyn analyzers and banned API checks)
dotnet build linq2db.slnx -c Release

# Quick single-TFM build for development (fastest iteration)
dotnet build linq2db.slnx -c Testing
# Testing config targets only net10.0 with DEBUG defines
```

## Running Tests

Test runner, config, patterns, and debugging are documented in [.claude/docs/testing.md](.claude/docs/testing.md). Read it before writing or modifying tests.

## Solution Structure

| Solution | Purpose |
|---|---|
| `linq2db.slnx` | Full solution |
| `linq2db.playground.slnf` | Lightweight — for working on specific tests without full load |
| `linq2db.Benchmarks.slnf` | Benchmarks only |

## Architecture

Core query pipeline, directory layout, and companion projects are documented in [.claude/docs/architecture.md](.claude/docs/architecture.md). Read it before working on anything under `Source/LinqToDB/`.

## Codebase design

Design invariants that define what linq2db *is* as a library — public-API contract, cross-cutting internals, SQL AST namespace placement, intentional column-aligned formatting — live in [.claude/docs/code-design.md](.claude/docs/code-design.md). Read it before changing anything under `Source/LinqToDB/` that touches namespaces, public types, or AST nodes.

## Code Conventions

- **Indentation**: Tabs (not spaces) for C#/VB. Spaces for F#, YAML, shell scripts, markdown.
- **C# version**: 14 (`LangVersion` in Directory.Build.props)
- **Nullable**: Enabled globally
- **Warnings as errors**: `TreatWarningsAsErrors` is true. No compilation warnings allowed.
- **Analyzers**: Run during Release builds only (`RunAnalyzersDuringBuild`). Banned API list in `Build/BannedSymbols.txt`.
- **XML documentation**: Required on new public classes, properties, and methods.
- **Target frameworks**: `net462`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. Use conditional compilation (`#if`) for TFM-specific code. Feature flags like `SUPPORTS_COMPOSITE_FORMAT`, `SUPPORTS_SPAN`, `ADO_ASYNC` are defined in `Directory.Build.props`.
- **Code style**: Match existing code. The project uses column-aligned formatting intentionally — do not "fix" alignment spacing.
- **.NET SDK**: 10.0 (see `global.json`)

## Versioning

All versions live in `Directory.Build.props`:

- `<Version>` — main product version, applied to every project in the solution
- `<EF3Version>`, `<EF8Version>`, `<EF9Version>`, `<EF10Version>` — per-EF-major versions used by the `LinqToDB.EntityFrameworkCore.EFx` packages

User-triggered version bumps are handled by the `/version-bump` skill (`.claude/skills/version-bump/SKILL.md`).

## Branch Conventions

- `master` — main development branch
- `release` — latest released version
- Bugfix branches: `issue/<issue_id>-<kebab-slug>` (e.g. `issue/1234-fix-cte-column-aliases`)
- Feature branches: `feature/<issue_id_or_feature_name>-<kebab-slug>` (e.g. `feature/5501-duckdb-provider`)

The `<kebab-slug>` is 2–5 lowercase, hyphen-separated words derived from the task goal so the branch name is legible at a glance.

See `Creating a new branch` in [.claude/docs/agent-rules.md](.claude/docs/agent-rules.md) for the branching workflow.

## Claude Code setup

`.claude/` layout, settings precedence, and skill discovery are documented in [.claude/docs/claude-setup.md](.claude/docs/claude-setup.md).

@.claude/docs/agent-rules.md
