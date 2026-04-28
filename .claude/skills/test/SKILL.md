---
name: test
description: Write a new linq2db test and/or run tests via `test-writer` + `test-runner`. Assumes the local environment (docker containers, `UserDataProviders.json`) has already been wired up by `/setup-tests`. Reports a single pass/fail summary at the end.
---

# /test

User-triggered workflow for writing a new test, running existing tests, or both in sequence. Environment setup is **not** this skill's job — the user is expected to have run `/setup-tests` earlier in the session (or to have a manually-configured `UserDataProviders.json` + running containers). This skill dispatches tests against whatever is currently enabled and running.

Shared reference material:

- **Environment setup**: `.claude/skills/setup-tests/SKILL.md` — providers + docker wireup.
- **Testing conventions** (framework, patterns, "read the full log" rule): `.claude/docs/testing.md`
- **Test database catalog** (provider → setup script → container): `.claude/docs/test-databases.md`
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

**Never bypass /test to call `test-runner` directly.** The skill provides the `"preconfigured"` consent value, baselines diff, and test-pattern normalization. Calling `test-runner` directly skips all three. If the user says "run tests…", invoke `/test`, never `Agent(test-runner)`.

**Permission-prompt discipline.** The skill itself should not issue `dotnet build` or `dotnet test` — delegate to the agent.

**Environment assumption.** `/test` trusts that docker containers for the enabled providers are running and `UserDataProviders.json` is correctly configured. If a run comes back with connection-refused / network-unreachable failures across every non-SQLite provider, report the failure verbatim and suggest running `/setup-tests` to re-wire the environment. Do **not** start containers or edit `UserDataProviders.json` from within `/test`.

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
- **EFCore test** — expand to all four projects (EF3/EF8/EF9/EF10). Use `{efMatrix: true, providers: []}` shorthand on the agent.
- **Explicit filter → specific test** — read the test's attributes to infer `[DataSources]` family and EFCore-vs-main. When the filter matches tests across both main and EFCore projects (rare but possible), ask the user to pick.

The `providers[]` field in `targets` is informational under `"preconfigured"` consent — the actual provider set is whatever `/setup-tests` left enabled in the matching bucket. Include the known-enabled provider list there when you have it (from the user's recent `/setup-tests` invocation or a quick read of `UserDataProviders.json`); otherwise pass `[]` and let the test run against whatever's enabled.

#### 3.2 Invoke test-runner

Call `test-runner` with:
- `testPattern` — the `--filter` value. **Always prepend `FullyQualifiedName~CreateData.CreateDatabase|`** unless the filter is already `CreateDatabase`-only or the user explicitly overrides. See `.claude/docs/testing.md` → **Database initialization** for why.
- `targets` — resolved in 3.1. Prefer the shorthand forms when applicable; remember `{mainTests: true, providers: [...]}` defaults to Playground at `net10.0`, so only set explicit `project` / `tfm` when overriding.
- `userProvidersConsent: "preconfigured"` — tells the agent the file is already wired up; no edits, no restore. Always this value; if the environment isn't configured, the user is expected to have run `/setup-tests` first.
- `config` — `"Debug"` unless the user asked for Release.
- `verbosity` — `"normal"`; flip to `"detailed"` when the user needs SQL-dump output from `TestContext.Out.WriteLine`.

#### 3.3 Baselines diff (when baselines are written)

If `UserDataProviders.json` → `MyConnectionStrings.BaselinesPath` is set **and** the run touched at least one provider whose baselines live under that path:

1. **Before** calling `test-runner`, snapshot `BaselinesPath` with `snap-baselines.ps1` (stdin manifest `{ paths: [BaselinesPath], outFile: ".build/.claude/baselines-pre-<run-id>.json" }`).
2. **After** the run, diff the snapshot with `diff-baselines.ps1` (stdin manifest `{ preFile, paths: [BaselinesPath] }`). For up to five entries in the returned `changed[]`, `Read` the post-run file and show a 3–5-line excerpt; cite the rest by count (e.g. "15 more files changed under `Firebird.5/...`"). Treat `added[]` / `removed[]` the same way.
3. If a file is on the `.claude/docs/testing.md` → **Known flaky baselines** list, note it explicitly and skip its preview.
4. If `BaselinesPath` is **unset** and the user's change is expected to move baselines (new SQL emission, new provider path), suggest running `/setup-tests` to set it (that skill has the prompt for proposing `c:\\GitHub\\linq2db.bls`). Otherwise skip silently.

#### 3.4 Report

Relay the agent's per-target summary:

- One row per target: `<project> (<tfm>) — passed/failed/none_matched · N passed, M failed, K skipped`
- For `failed` targets, include the first failure's message + top stack frame.
- For `none_matched` targets, cite the agent's `note` (usually a `#if !NETFRAMEWORK` guard or a filter that doesn't match any currently-enabled provider's tests).
- If the failure pattern looks like "connection refused / network unreachable on every non-SQLite target", suggest `/setup-tests` and stop — don't retry.

Finally, if the branch is on an open PR and the local run uncovered no regressions, mention the `/azp run` option (see `.claude/docs/ci-tests.md`). Do not auto-post — just surface it; the user decides.

### 4. Write-and-run (chain)

Execute the **Write flow** first. Before chaining to the **Run flow**:

1. Show the user the inserted test (file path + line range; optionally the test body via `Read` if they ask).
2. Ask: "Run the new test now? [y/N]". On `y`, proceed to step 3 using `FullyQualifiedName~<new-test-name>` as the filter. On `N`, stop cleanly.

## Don'ts

- Do not edit `UserDataProviders.json`. That's `/setup-tests`'s job. If the run needs a provider that isn't currently enabled, stop and suggest `/setup-tests`.
- Do not run `docker` lifecycle commands (`inspect`, `start`, `stop`, `rm`, or setup scripts). Same rule: that's `/setup-tests`.
- Do not invoke `test-runner` with any consent value other than `"preconfigured"`. If the environment isn't wired up, the right answer is `/setup-tests`, not a mid-`/test` consent prompt.
- Do not run tests in `Release` config by default — analyzers + banned-API checks are slow and rarely what the user wants for a single-test run.
- Do not edit source files yourself. Writing is `test-writer`'s job; running is `test-runner`'s job. The skill only orchestrates and relays.
- Do not run targets in parallel. The agents won't anyway (sequential is built into `test-runner`), but do not try to fan out multiple agent invocations with overlapping target sets either.
- Do not suppress or skim the agent's output. If the agent reports a failure, relay the verbatim error message; don't paraphrase.
