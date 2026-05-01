---
name: test
description: Write a new linq2db test, run an existing test / filter, or both. Orchestrates the `test-writer` and `test-runner` agents and reports a single pass/fail summary at the end. Env management (docker containers, `UserDataProviders.json`) lives in the `/test-providers` skill, not here — `/test` reads the configured state but never edits it.
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

**Never bypass /test to call `test-runner` directly.** The skill gates `CreateDatabase` injection (step 3.3) and the baselines diff (step 3.4). Calling `test-runner` directly skips both — empty-DB failures and missed baselines drift typically trace back to exactly that bypass. If the user says "run tests…", invoke `/test`, never `Agent(test-runner)`.

**Env management is out of scope.** Docker container lifecycle and `UserDataProviders.json` edits are owned by `/test-providers` (`.claude/skills/test-providers/SKILL.md`). `/test` does not start, stop, or inspect containers, and does not edit `UserDataProviders.json` — `test-runner` is read-only on it (see the agent contract). If a run fails because a container is down or a provider is disabled, surface the failure as-is and tell the user to run `/test-providers <provider> [...]` to fix the env, then re-run `/test`. Do not pre-validate env state, do not auto-dispatch to `/test-providers`.

**Permission-prompt discipline.** Every `Bash` call is evaluated against the allowlist. When `test-runner` runs, its only shell calls are `dotnet test` invocations. The skill itself should not issue `dotnet build` or `dotnet test` — delegate to the agent.

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

#### 3.2 Resolve target providers

Provider state is owned by `/test-providers` and read by `test-runner` from `UserDataProviders.json`. `/test` does **not** propose providers, edit the file, or pre-validate that the requested set is enabled — that's the user's job ahead of running `/test`.

Determine the provider list to pass to `test-runner` per target:

- **User named providers in `/test` args** (e.g. `/test run Issue5177Test on PostgreSQL.18, SQLite.MS`) — pass that list through verbatim. If a named provider isn't currently enabled in the matching TFM bucket, `test-runner` will abort with a "Provider X not enabled" message pointing at `/test-providers`; relay that as-is.
- **No providers in args** — `Read` the relevant TFM bucket of `UserDataProviders.json` once, list the currently enabled providers, and ask the user to confirm or narrow before passing the set through. Do **not** silently default to "every enabled provider" — most users want a small subset for a single-test run.

If the test was just authored by the write flow, the test-writer's `dataSources` output is a hint about scope, not a provider list — still pass the user's explicit choice (or confirmed read-back) to `test-runner`.

#### 3.3 Invoke test-runner

Call `test-runner` with:
- `testPattern` — the `--filter` value. **Always prepend `FullyQualifiedName~CreateData.CreateDatabase|`** unless the filter is already `CreateDatabase`-only or the user explicitly overrides. See `.claude/docs/testing.md` → **Database initialization** for why.
- `targets` — resolved in 3.1 / 3.2. Prefer the shorthand forms when applicable; remember `{mainTests: true, providers: [...]}` defaults to Playground at `net10.0`, so only set explicit `project` / `tfm` when overriding.
- `config` — `"Debug"` unless the user asked for Release.
- `verbosity` — `"normal"`; flip to `"detailed"` when the user needs SQL-dump output from `TestContext.Out.WriteLine`.

`test-runner` is read-only on `UserDataProviders.json` — there is no `userProvidersConsent` or `restoreOnCompletion` input, and no provider edits happen as a side effect of the run. If the agent reports `status: "blocked"` with a missing-provider message, relay it to the user with a one-liner: "Run `/test-providers <provider>` to enable it, then re-run `/test`."

#### 3.4 Baselines diff (when baselines are written)

If `UserDataProviders.json` → `MyConnectionStrings.BaselinesPath` is set **and** the run touched at least one provider whose baselines live under that path:

1. **Before** calling `test-runner`, snapshot `BaselinesPath` with `snap-baselines.ps1` (stdin manifest `{ paths: [BaselinesPath], outFile: ".build/.claude/baselines-pre-<run-id>.json" }`).
2. **After** the run, diff the snapshot with `diff-baselines.ps1` (stdin manifest `{ preFile, paths: [BaselinesPath] }`). For up to five entries in the returned `changed[]`, `Read` the post-run file and show a 3–5-line excerpt; cite the rest by count (e.g. "15 more files changed under `Firebird.5/...`"). Treat `added[]` / `removed[]` the same way.
3. If a file is on the `.claude/docs/testing.md` → **Known flaky baselines** list, note it explicitly and skip its preview.
4. If `BaselinesPath` is **unset** and the user's change is expected to move baselines, mention it once: "Set `BaselinesPath` via `/test-providers` if you want diffs." Do not edit the file from here — that's `/test-providers` step 4. Skip the snapshot/diff for this run.

#### 3.5 Report

Relay the agent's per-target summary:

- One row per target: `<project> (<tfm>) — passed/failed/none_matched · N passed, M failed, K skipped`
- For `failed` targets, include the first failure's message + top stack frame.
- For `none_matched` targets, cite the agent's `note` (usually a `#if !NETFRAMEWORK` guard).

If any failure looks like an env problem (connection refused, "Provider X not enabled" block, missing schema), point at `/test-providers` for the fix — do not investigate or auto-repair from here. Otherwise just relay the agent's output verbatim.

Finally, if the branch is on an open PR and the local run uncovered no regressions, mention the `/azp run` option (see `.claude/docs/ci-tests.md`). Do not auto-post — just surface it; the user decides. If containers were started for this run via `/test-providers`, remind the user once: "Run `/test-providers stop` when you're done with the containers."

### 4. Write-and-run (chain)

Execute the **Write flow** first. Before chaining to the **Run flow**:

1. Show the user the inserted test (file path + line range; optionally the test body via `Read` if they ask).
2. Ask: "Run the new test now? [y/N]". On `y`, proceed to step 3 using `FullyQualifiedName~<new-test-name>` as the filter. On `N`, stop cleanly.

### 5. End-of-session housekeeping

If containers were started during the session via `/test-providers`, remind the user once that they're still running and `/test-providers stop` will shut them down. Do not auto-stop them, do not invoke `/test-providers` from here.

## Don'ts

- Do not edit `UserDataProviders.json` or invoke `docker` commands from `/test`. Env changes route through `/test-providers` exclusively. If the run needs a different provider set or a stopped container started, tell the user — don't fix it.
- Do not run tests in `Release` config by default — analyzers + banned-API checks are slow and rarely what the user wants for a single-test run.
- Do not edit source files yourself. Writing is `test-writer`'s job; running is `test-runner`'s job. The skill only orchestrates and relays.
- Do not run targets in parallel. The agents won't anyway (sequential is built into `test-runner`), but do not try to fan out multiple agent invocations with overlapping target sets either.
- Do not suppress or skim the agent's output. If the agent reports a failure, relay the verbatim error message; don't paraphrase.
- Do not pre-validate the env state before invoking `test-runner`. Trust the user; let the agent's own provider-check abort surface any mismatch. Auto-fix attempts here defeat the boundary.
