---
name: test
description: Write a new linq2db test, run an existing test / filter, or both. Orchestrates the `test-writer` and `test-runner` agents, handles the `UserDataProviders.json` consent prompt, and reports a single pass/fail summary at the end.
---

# /test

User-triggered workflow for everything test-related — writing a new test, running existing tests against a chosen provider matrix, or both in sequence.

Shared reference material:

- **Testing conventions** (framework, patterns, "read the full log" rule): `.claude/docs/testing.md`
- **Test-writing agent contract**: `.claude/agents/test-writer.md`
- **Test-running agent contract**: `.claude/agents/test-runner.md`

## When to run

The user invokes `/test <args>`. Accepted arg shapes, in decreasing priority:

| Arg shape | Intent |
|---|---|
| `write <description>` / `add <description>` / `create <description>` | Write flow only. |
| `run <filter>` / a bare test name / `FullyQualifiedName~...` | Run flow only. |
| `write-and-run <description>` / `add+run <description>` | Write flow, then run flow on the new test after user confirms the diff. |
| empty `/test` | Ask: "write, run, or write-and-run?" |

If the args are ambiguous (e.g. a phrase like "bulk copy identity test" that could be read either way), ask before dispatching — do not guess.

## Steps

**Permission-prompt discipline.** Every `Bash` call is evaluated against the allowlist. When `test-runner` runs, its only shell calls are `dotnet test` invocations (and optionally a `cp` for the backup). The skill itself should not issue `dotnet build` or `dotnet test` — delegate to the agent.

### 1. Resolve intent

Parse args per the table above. On ambiguity or empty args, ask the user (single prompt, numbered options so they can reply with a number).

### 2. Write flow (if applicable)

Collect requirements:

1. **Task description** — what the test should verify. Reuse the user's phrasing; ask only if the description is too vague to act on.
2. **Target provider(s)** — if the description doesn't make it obvious (e.g. "add a regression for issue #5439 on SQL Server" → `TestProvName.AllSqlServer`), ask.
3. **Preferred test file / class** — optional. When the task cites an issue number, try to find an existing `Issue<N>Tests.cs` or the nearest `IssueTests.cs` by grep before asking.
4. **Issue / task reference** — optional; ask for the number or URL when the test is an issue regression.

Invoke `test-writer` with those inputs. When it returns `status: "needDisambiguation"`, present its `candidates[]` to the user as a numbered list and re-invoke with the choice.

On success, report to the user:
- File(s) modified, line range of the inserted test.
- The one-sentence rationale from the agent.
- The build-check result (when the agent ran one).

### 3. Run flow (if applicable)

#### 3.1 Determine targets

Resolve `{project, tfm, providers}` for each run:

- **Explicit filter → specific test** — read the test's attributes to infer `[DataSources]` family and EFCore-vs-main. When the filter matches tests across both main and EFCore projects (rare but possible), ask the user to pick.
- **EFCore test** — expand to all four projects (EF3/EF8/EF9/EF10) with the matching TFM. Use `{efMatrix: true, providers: [...]}` shorthand on the agent.
- **Main test with `[IncludeDataSources]`** — use `Tests/Linq/Tests.csproj`, TFM `net10.0` unless the user asks for another.

Provider selection default: **lowest supported version per provider family** (matches `/review-pr`'s convention when picking a single representative for a family). For SQL Server, `SqlServer.2016.MS` is the lowest tested. Ask the user to confirm before applying the matrix.

#### 3.2 UserDataProviders.json consent

**Before any call to `test-runner`**, check whether the run will require editing `UserDataProviders.json` (compare the current enabled providers per TFM bucket to the targets). If an edit is needed AND this is the **first** edit of the session:

Prompt the user once:

> `UserDataProviders.json` is gitignored and holds your local test config. Running this target needs to change the enabled providers per TFM bucket. Options:
>
> 1. **auto-backup** — I copy the current file to `.build/.claude/UserDataProviders.json.bak.<timestamp>` before editing, and restore the original after the run. (recommended)
> 2. **skip-backup** — edit in place without a backup copy, still restore the original from an in-memory snapshot after the run.
> 3. **cancel** — abort, don't touch the file.
>
> Choose 1, 2, or 3.

Record the choice. For subsequent runs in the same session, don't re-prompt — pass the same consent value through.

#### 3.3 Invoke test-runner

Call `test-runner` with:
- `testPattern` — the `--filter` value.
- `targets` — resolved in 3.1 (prefer the shorthand forms when applicable).
- `userProvidersConsent` — the choice from 3.2 (`"auto-backup"` / `"skip-backup"`).
- `restoreOnCompletion` — default `true`. Offer to flip to `false` when the user intends to keep the current provider set for follow-up manual runs.
- `config` — `"Debug"` unless the user asked for Release.
- `verbosity` — `"normal"`; flip to `"detailed"` when the user needs SQL-dump output from `TestContext.Out.WriteLine`.

#### 3.4 Report

Relay the agent's per-target summary:

- One row per target: `<project> (<tfm>) — passed/failed/none_matched · N passed, M failed, K skipped`
- For `failed` targets, include the first failure's message + top stack frame.
- For `none_matched` targets, cite the agent's `note` (usually a `#if !NETFRAMEWORK` guard).
- Backup path (when `auto-backup`) so the user can inspect / roll back manually if needed.
- Whether `UserDataProviders.json` was restored.

### 4. Write-and-run (chain)

Execute the **Write flow** first. Before chaining to the **Run flow**:

1. Show the user the inserted test (file path + line range; optionally the test body via `Read` if they ask).
2. Ask: "Run the new test now? [y/N]". On `y`, proceed to step 3 using `FullyQualifiedName~<new-test-name>` as the filter. On `N`, stop cleanly.

### 5. End-of-session housekeeping

When the session ends (last turn before user moves on), and `UserDataProviders.json` was edited with `restoreOnCompletion: false` at any point, remind the user: the file is still in its edited state; the pre-edit backup (if one was taken) is at the path shown earlier.

## Don'ts

- Do not invoke `test-runner` without a resolved `userProvidersConsent`. The agent will refuse; avoid the round-trip.
- Do not run tests in `Release` config by default — analyzers + banned-API checks are slow and rarely what the user wants for a single-test run.
- Do not edit source files yourself. Writing is `test-writer`'s job; running is `test-runner`'s job. The skill only orchestrates and relays.
- Do not run targets in parallel. The agents won't anyway (sequential is built into `test-runner`), but do not try to fan out multiple agent invocations with overlapping target sets either.
- Do not suppress or skim the agent's output. If the agent reports a failure, relay the verbatim error message; don't paraphrase.
