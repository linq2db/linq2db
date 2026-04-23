---
name: fix-issue
description: Orchestrate the full workflow for reproducing and testing a linq2db issue — load the issue, clarify scope, create an `issue/<n>-<slug>` branch, and drive a regression test through test-writer + test-runner. Does not write the fix code itself; the user drives the fix and the skill wraps the testing loop around it.
---

# /fix-issue

User-triggered orchestrator. Turns a GitHub issue number into:

1. A clean working branch named `issue/<n>-<kebab-slug>`.
2. A regression test that currently demonstrates the bug (pre-fix) and passes (post-fix).
3. A confirmed provider matrix + database lifecycle managed by `/test`.

The skill **does not** write the fix itself — that's the user's call. It owns the "read issue → repro → test" loop around the fix.

## Shared reference material

- **Testing conventions**: `.claude/docs/testing.md`
- **Test database catalog** (provider → script → container): `.claude/docs/test-databases.md`
- **Branch rules + slug format**: `.claude/docs/agent-rules.md` → *Creating a new branch*
- **Test-writer agent contract**: `.claude/agents/test-writer.md`
- **Test-runner agent contract**: `.claude/agents/test-runner.md`
- **Composable `/setup-tests` skill**: `.claude/skills/setup-tests/SKILL.md` — environment wireup (docker + `UserDataProviders.json`).
- **Composable `/test` skill**: `.claude/skills/test/SKILL.md` — `/fix-issue` delegates the write + run flow to `/test`'s step-3/step-4 procedures.

## When to run

The user invokes `/fix-issue <issue-ref>` where `<issue-ref>` is one of:

- An issue number (`5177`)
- A full GitHub URL (`https://github.com/linq2db/linq2db/issues/5177`)
- `current` — the issue already in conversation context (resolved via the most recent `gh issue view` or explicit user reference)

Only invoke this skill for `linq2db/linq2db` issues. For issues in other repos, say so and stop.

## Steps

### 1. Load the issue

1. Resolve the issue reference. For a bare number, call `gh issue view <n> --repo linq2db/linq2db --json number,title,body,state,labels,milestone,comments`.
2. If the issue is **closed** without a linked PR, warn the user and ask whether to proceed (they may be filing a regression against a previously-fixed issue).
3. Read the **body** and all **comments**. Extract:
   - Repro steps / code blocks (often LINQ expressions + expected vs. actual SQL)
   - Provider(s) the reporter pinned (`firebird 4`, `postgres 15`, etc.)
   - linq2db version(s) mentioned — useful when the repro needs a particular dialect path
   - Any attached linked PRs from earlier fix attempts (via the `closingIssues` edges in `closingIssuesReferences`, if present)

### 2. Summarize + ask-ask-do-all (clarify in one round)

Summarize the issue back to the user in 5–8 lines. Then, in **the same message**, batch every clarification question you can anticipate. Typical questions:

1. **Provider scope** — which providers should the test cover? Propose the default set per `.claude/docs/test-databases.md` given the reporter's provider mentions, plus SQLite / SQL Server 2016 (local) as always-available anchors. Offer "all providers mentioned in the issue" / "just <X>" / "let me pick".
2. **Branch slug** — propose a 2–5 word kebab slug derived from the issue title. Show the full branch name (`issue/<n>-<slug>`) and ask for approval or a replacement.
3. **Pre-fix test expectation** — should the test, right now, *fail* on master (demonstrating the bug), or *pass* (if the user has already drafted the fix locally)? Affects whether we run the test before creating the branch's first commit.
4. **Reproduction completeness** — if the issue body is ambiguous (no LINQ snippet, vague expected behavior), ask for whatever's missing.
5. **Fixture location preference** — ask only if the test-writer's fixture-lookup rules (grep for `Issue<N>Tests.cs` → `IssueTests.cs` → feature fixture) don't yield an obvious answer. Otherwise, let test-writer pick and relay its rationale.
6. **Heavy-provider opt-in** — if the issue mentions DB2 / Informix / SAP HANA / SAP ASE, explicitly flag the startup cost and ask whether to include them. Default: exclude.

Number the questions. Wait for answers before moving on. Do not interleave partial actions with further questions unless a later question genuinely depends on the outcome of an earlier action.

### 3. Create the branch

Only after the user confirms the summary + slug. Follow `.claude/docs/agent-rules.md` → *Creating a new branch*:

1. Check for a dirty working tree. If dirty, stop and ask whether to stash.
2. `git fetch origin master` — keep the base fresh.
3. `git checkout -b issue/<n>-<slug> origin/master`.
4. Confirm the new branch is checked out (`git rev-parse --abbrev-ref HEAD`).

Do **not** commit yet — the branch starts empty relative to master.

### 3b. Map existing test coverage

Per `.claude/docs/agent-rules.md` → **Before coding a fix or feature**: before invoking `test-writer` (step 4) or letting the user start the fix, enumerate existing tests that already exercise the affected path. `Grep` under `Tests/` for the target code's keywords (SQL builder type, translator method, provider class), shortlist `<Fixture>.<Test>` entries with a one-line purpose each, and flag what the new regression test will add on top. Show the shortlist to the user and wait for a `go` / adjustment before proceeding.

This step is cheap and catches "the bug is already covered by `X.Y`" surprises before anyone writes code.

### 4. Write the regression test (delegate to test-writer)

Invoke the `test-writer` agent with:

- **Task** — one-paragraph description quoting the expected vs. actual behavior from the issue.
- **Target provider(s)** — translated from the step-2 answer into `TestProvName.All<Family>` / `[DataSources]`.
- **Surrounding code pointer** — whichever `Source/…` file or type the issue points at (often present in the body as a stack trace line). Optional.
- **Preferred test file / class** — omit to let the agent run its fixture-lookup rules. Override only if step 2 pinned a specific file.
- **Issue / task reference** — the issue number + URL.
- **`playgroundLink`** — `true` by default for main-tests (fast iteration). Set `false` for EFCore tests or when the user opted out of playground.

When the agent returns `status: "needDisambiguation"`, surface its `candidates[]` to the user as a numbered list and re-invoke with their choice.

On `status: "written"`, show the user:
- File(s) modified + inserted line range
- The agent's one-sentence rationale
- The build-check result if the agent ran one
- A `Read` snippet of the new test body for quick visual confirmation

### 5. Wire up the environment (delegate to /setup-tests)

Invoke `/setup-tests` with the provider set confirmed in step 2 and the TFM bucket matching the project picked in step 6.1 below (typically `NET100` for Playground runs; `NETFX` / `NET80` / `NET90` / `NET100` for EFCore EF3/EF8/EF9/EF10 respectively).

`/setup-tests` handles docker image/container inspection + startup, `UserDataProviders.json` consent + edits, and optional `BaselinesPath` configuration. Show the user `/setup-tests`'s final summary (providers enabled, containers running) before moving on. Do not duplicate any of its checks here.

### 6. Run the test (delegate to /test's run flow)

Chain into the **Run flow** documented in `.claude/skills/test/SKILL.md` — specifically:

1. **3.1 Determine project + TFM.** For main tests, default to `Tests/Tests.Playground/Tests.Playground.csproj` at `net10.0` (the test file is already linked via step 4). For EFCore tests, use the appropriate `Tests.EntityFrameworkCore.EFx.csproj`.
2. **3.2 Invoke test-runner.** Pass the filter `FullyQualifiedName~<test-name>`, the target shape from 3.1, and `userProvidersConsent: "preconfigured"` — the environment was wired up in step 5.

### 7. Report the result

Relay the agent's per-target summary verbatim. If the pre-fix expectation from step 2 was "should fail", sanity-check:

- If the test **failed** (and the user said "should fail"): the repro is solid. Next step is the user's — write the fix on this branch.
- If the test **passed** (and the user said "should fail"): the repro may not actually reproduce the issue. Surface this to the user explicitly — suggest re-reading the issue for missing repro detail, or that the issue is already fixed on `master`.
- If the test **errored**: relay the first failure's message + stack top and ask the user how to proceed. If the errors look like "connection refused / network unreachable" across every non-SQLite target, suggest re-running `/setup-tests` (the environment may have drifted since step 5).

Do not auto-commit even when the result looks clean. The user drives commits (per `.claude/docs/agent-rules.md` → *Git commit rules*).

### 8. Hand-off

Finish with a short summary:
- Branch: `issue/<n>-<slug>`
- Test: `<path>:<line>` (name + fixture)
- Provider matrix: `[<providers>]`
- Pre-fix test state: `fails as expected` / `passes unexpectedly` / `errored`

Container cleanup is handled by the `cleanup-docker-session` SessionEnd hook — containers `/setup-tests` started this session are stopped automatically when the session exits. Do not prompt for per-container cleanup here.

Stop. The user continues from here — they write the fix, commit, push, and open the PR on their own terms (or via `/review-pr` / an ad-hoc commit request).

## Don'ts

- Do not write the fix code. This skill's scope ends at "test is in place and its current state is understood". Fixes are the user's responsibility, because fix strategy almost always involves judgment calls on SQL / translator design that shouldn't be made silently.
- Do not commit automatically. Even after a successful write+run, wait for explicit `commit` (per agent-rules). The branch is fine in a dirty state until the user decides.
- Do not push the branch or open a PR. `/fix-issue` is a local workflow; `git push` / `gh pr create` require explicit user asks (agent-rules).
- Do not re-prompt the user for answers already given in step 2. Reuse provider confirmation, slug, consent values through the whole session.
- Do not invoke `test-runner` directly. Environment setup goes through `/setup-tests` (step 5); test dispatch goes through `/test`'s run flow (step 6). Bypassing either skips consent / lifecycle safeguards.
- Do not expand scope. If the issue mentions three providers but only one actually exhibits the bug, raise the discrepancy to the user rather than quietly pruning; they may want the regression to cover the other two providers as a baseline.
- Do not propose heavy providers (DB2 / Informix / SAP HANA / SAP ASE) silently. Flag their cost per `.claude/docs/test-databases.md` → *Heavy providers*.
