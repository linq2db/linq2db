---
name: test
description: Write a new linq2db test, run an existing test / filter, or both. Orchestrates the `test-writer` and `test-runner` agents, handles the `UserDataProviders.json` consent prompt, and reports a single pass/fail summary at the end.
---

# /test

User-triggered workflow for everything test-related — writing a new test, running existing tests against a chosen provider matrix, or both in sequence.

Shared reference material:

- **Testing conventions** (framework, patterns, "read the full log" rule): `.claude/docs/testing.md`
- **Test database catalog** (provider → setup script → container → preference): `.claude/docs/test-databases.md`
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

#### 3.1 Determine project + TFM

Resolve `{project, tfm}` for each run:

- **Main linq2db test (default)** — use `Tests/Tests.Playground/Tests.Playground.csproj` at `net10.0`. Playground builds much faster than `Tests/Linq/Tests.csproj`. The test source file must be linked into the playground csproj via `<Compile Include>` (see the test-writer agent's `playgroundLink` flag). If the filter targets a test that isn't linked, either ask the user to re-run the write flow with `playgroundLink: true`, or fall back to `Tests/Linq/Tests.csproj`.
- **Main linq2db test, multiple TFMs required** — use `Tests/Linq/Tests.csproj` with the TFM list the user asked for. Confirm with the user if the test description doesn't make the TFM scope obvious.
- **EFCore test** — expand to all four projects (EF3/EF8/EF9/EF10). Use `{efMatrix: true, providers: [...]}` shorthand on the agent.
- **Explicit filter → specific test** — read the test's attributes to infer `[DataSources]` family and EFCore-vs-main. When the filter matches tests across both main and EFCore projects (rare but possible), ask the user to pick.

#### 3.2 Confirm providers (always)

Provider selection is **always confirmed with the user**, even when the filter's `[DataSources]` would nominally expand to every enabled provider. Running against "all providers" is rarely what the user wants — most test databases are expensive to start, and the user usually has one or two in mind.

**Proposal algorithm.** Consult `.claude/docs/test-databases.md` and propose, in this order:

1. **SQLite** — always first, always enabled (no docker, no startup cost).
2. **SQL Server** — `SqlServer.2016` / `SqlServer.2016.MS` via local non-docker instance (default); fall back to `SqlServer.2022.MS` (docker) if the user asks for docker or the local instance is unavailable.
3. **PostgreSQL** — `PostgreSQL.18` (default); or the "dialect-anchor" set (9.2, 9.3, 9.5, 13, 15, 18) if the user wants full Postgres dialect coverage.
4. The specific provider family the test targets, when the filter or `[IncludeDataSources]` already pins one (e.g. a `MergeTests.Oracle.*` filter pins Oracle — default `oracle11` + `oracle12`).
5. Any additional providers the test-writer declared in its output's `dataSources` — confirm whether to include each.
6. **Heavy providers** (DB2 / Informix / SAP HANA / SAP ASE) are *never* proposed silently. Surface them only if the test's filter / attributes require them, and always flag the cost per the "Heavy providers" section of `test-databases.md`.

Present the proposal as a numbered list with per-provider notes (local vs docker; preferred default). Ask the user to confirm or edit before moving on. Example:

> I'll run `Issue5177Test` against these providers by default:
>
> 1. `SQLite.MS` — always on, no startup cost.
> 2. `SqlServer.2016.MS` — assumed local non-docker instance; say the word if you want docker instead.
> 3. `PostgreSQL.18` — will need to start the `pgsql18` container (see 3.3 below).
>
> OK as-is, or want to add/remove?

Record the user's confirmed list; skip this prompt on subsequent runs in the same session that use the same provider set.

#### 3.3 Docker lifecycle (per non-SQLite provider)

For each confirmed provider that isn't SQLite or a local non-docker SQL Server:

1. Look up the container name + image via `.claude/docs/test-databases.md`.
2. `docker image inspect <image>` via Bash — succeeds if the image layer is cached locally.
3. `docker container inspect <container>` via Bash — succeeds with the container status (`running`, `exited`, `created`) or fails if the container doesn't exist.
4. Decision tree:
   - **Container running** — use as-is; do not touch.
   - **Container exited/created** — `docker start <container>`. Record `startedByUs[<container>] = true` for end-of-run cleanup prompt.
   - **Container missing OR image missing** — ask the user (single prompt, numbered options): "Run `Data/Setup Scripts/<script>.cmd` now? (creates + starts the container, may pull the image)". On confirmation, run the script via Bash. Record `startedByUs[<container>] = true`.
5. For heavy providers (per `test-databases.md`), prefix the startup prompt with the cost note ("SAP HANA typically takes 5–10 min to become ready and uses several GB of RAM — proceed?").

Batch the `docker image inspect` + `docker container inspect` checks across all providers in a single turn (independent calls). Record the full lifecycle state (`running-existing` / `started-by-us` / `created-by-us`) for each container so step 3.6 can offer to stop only the ones we started.

#### 3.4 UserDataProviders.json consent

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

#### 3.5 Invoke test-runner

Call `test-runner` with:
- `testPattern` — the `--filter` value.
- `targets` — resolved in 3.1 / 3.2. Prefer the shorthand forms when applicable; remember `{mainTests: true, providers: [...]}` defaults to Playground at `net10.0`, so only set explicit `project` / `tfm` when overriding.
- `userProvidersConsent` — the choice from 3.4 (`"auto-backup"` / `"skip-backup"`).
- `restoreOnCompletion` — default `true`. Offer to flip to `false` when the user intends to keep the current provider set for follow-up manual runs.
- `config` — `"Debug"` unless the user asked for Release.
- `verbosity` — `"normal"`; flip to `"detailed"` when the user needs SQL-dump output from `TestContext.Out.WriteLine`.

#### 3.6 Report + container cleanup prompt

Relay the agent's per-target summary:

- One row per target: `<project> (<tfm>) — passed/failed/none_matched · N passed, M failed, K skipped`
- For `failed` targets, include the first failure's message + top stack frame.
- For `none_matched` targets, cite the agent's `note` (usually a `#if !NETFRAMEWORK` guard).
- Backup path (when `auto-backup`) so the user can inspect / roll back manually if needed.
- Whether `UserDataProviders.json` was restored.

Then — for every container recorded as `started-by-us` or `created-by-us` in step 3.3 — ask the user whether to stop it:

> I started these containers for this run. Stop them now, or leave running for follow-up tests?
>
> 1. `pgsql18` — started by me, running since <time>.
> 2. `sql2022` — I created it from scratch (image pull + setup script); running since <time>.
>
> Reply with numbers to stop (e.g. `1,2`), `all` to stop all, or `none` to leave running.

Default to **leave running** on an empty reply — the user may want follow-up runs and the container is cheap to keep. Only stop what the user explicitly named. Never auto-stop; always ask.

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
