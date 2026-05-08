---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# New scaffolding framework and CLI tool (replace T4)

## Context
The existing T4-based code generation required a T4 host (Visual Studio or Rider), was difficult to use from the command line, and could not be extended without forking the T4 templates. The templates had accumulated years of per-provider workarounds and lacked a stable API for customization.

## Decision
Commit `e1dadf1` (2022, #3098) introduced a new scaffolding framework backed by a dotnet CLI tool (linq2db.cli). The framework uses a programmatic API (C# classes) for model customization rather than T4 text macros. A new linq2db.Tools package exposes the scaffolding API for embedding in build pipelines. The T4 templates were retained but marked as legacy.

## Why
A CLI-first approach enables headless scaffolding in CI and removes the T4 host requirement. The programmatic API allows schema customization via C# code, which integrates with IDE tooling (completion, refactoring) and is testable.

## Consequences
- Source/LinqToDB.CLI/ added as a new project.
- T4 templates remained in Source/LinqToDB.Templates/ but new development focuses on the CLI tool.
- The scaffolding framework was further developed and migrated to the main repo in subsequent commits.

## Sources
- Commit `e1dadf1` -- New scaffolding framework / CLI tool (#3098) (MaceWindu, 2022)
