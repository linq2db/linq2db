# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is linq2db

LINQ to DB is a lightweight, type-safe ORM and LINQ provider for .NET. It translates LINQ expressions into SQL — no change tracking, no heavy abstraction. Think "one step above Dapper" with compiler-checked queries.

Repository: https://github.com/linq2db/linq2db — use this when resolving issue/PR numbers via `gh` (e.g. `gh pr view <n> --repo linq2db/linq2db`).

Pre-2011 history (older than this repo's initial commit) lives in [BLToolkit](https://github.com/igor-tkachev/bltoolkit), the project linq2db forked from. Consult [.claude/docs/predecessor-bltoolkit.md](.claude/docs/predecessor-bltoolkit.md) when tracing API origins / authorship that hits the 2011-12-12 import boundary.

## Build

- `dotnet build linq2db.slnx` — full solution
- `dotnet build linq2db.slnx -c Release` — enables Roslyn analyzers + banned-API checks
- `dotnet build linq2db.slnx -c Testing` — fast single-TFM (net10.0) iteration
- `dotnet build Source/LinqToDB/LinqToDB.csproj` — core library only

## Running Tests

Test runner, config, patterns, and debugging are documented in [.claude/docs/testing.md](.claude/docs/testing.md). Read it before writing, modifying, or running tests.

## Solution files

- `linq2db.slnx` — full solution
- `linq2db.playground.slnf` — light filter for working on individual tests
- `linq2db.Benchmarks.slnf` — benchmarks only

## Architecture

Core query pipeline, directory layout, and companion projects are documented in [.claude/docs/architecture.md](.claude/docs/architecture.md). Read it before working on anything under `Source/LinqToDB/`.

## Codebase design

Design invariants that define what linq2db *is* as a library — public-API contract, cross-cutting internals, SQL AST namespace placement, intentional column-aligned formatting — live in [.claude/docs/code-design.md](.claude/docs/code-design.md). Read it before changing anything under `Source/LinqToDB/` that touches namespaces, public types, or AST nodes.

## Code conventions

- Tabs for C#/VB; spaces for F#, YAML, shell, markdown.
- C# 14, nullable enabled, `TreatWarningsAsErrors=true`. .NET SDK 10 (`global.json`). Analyzers run in Release only; banned APIs in `Source/BannedSymbols.txt`.
- XML docs required on new public types/members.
- TFMs: `net462`, `netstandard2.0`, `net8.0`–`net10.0`. Feature-flag macros (e.g. `SUPPORTS_SPAN`, `ADO_ASYNC`) live in `Directory.Build.props`.

## Versioning

Versions live in `Directory.Build.props`. User-triggered bumps go through the `/version-bump` skill.

## Branches

`master` is main; `release` tracks the latest release. Branch-naming and workflow rules live in [.claude/docs/agent-rules.md](.claude/docs/agent-rules.md) → *Creating a new branch*.

## Claude Code setup

`.claude/` layout, settings precedence, and skill discovery are documented in [.claude/docs/claude-setup.md](.claude/docs/claude-setup.md).

@.claude/docs/agent-rules.md
