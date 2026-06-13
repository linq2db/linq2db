---
name: enable-disabled-test
description: Enable a regression test that was committed under `[ActiveIssue]` after its tracking issue has been closed. Finds the gated test(s), verifies pass on current master, bisects to identify the load-bearing fix PR (so the enable-PR cites the real cause, not the close-comment attribution), removes the gate, and opens a draft enable-PR with the issue milestone backfilled. Read-only on `UserDataProviders.json` and docker — env state is the user's; the skill consumes whatever providers are already enabled.
---

# /enable-disabled-test

User-triggered. Turns a closed issue number into a draft PR that drops its `[ActiveIssue]` regression-test gate, with bisect-verified attribution to the PR that actually fixed the underlying bug.

## Shared reference material

- **Branch + commit + push rules**: `.claude/docs/agent-rules.md` → *Creating a new branch*, *Git commit rules*, *Push to remote rules*, *Pull request rules*
- **Enabling-test guardrail**: `.claude/docs/bug-investigation.md` → *Enabling an `[ActiveIssue]` test after the issue closes*
- **Worktree mechanics for bisect**: `.claude/docs/worktree.md`
- **Bisect-across-SDK gotcha**: `.claude/docs/windows-gotchas.md` → *Bisecting across SDK upgrades*
- **Closed-milestone PATCH**: `.claude/docs/github-authoring.md` → *`gh issue edit --milestone` rejects closed milestones*
- **`/test` skill** (used to verify pass on master): `.claude/skills/test/SKILL.md`
- **`pr-resolver.md`** (issue → PR lookup): `.claude/docs/pr-resolver.md`

## When to run

User invokes `/enable-disabled-test <issue-ref>` where `<issue-ref>` is an issue number, a full GitHub URL, or `current` (the issue most recently shown in the conversation).

Don't run if the issue is still **open** — its test is gated for a reason. Stop and tell the user.

## Steps

### 1. Resolve the issue + find its disabled tests

1. Resolve the reference. `gh issue view <n> --repo linq2db/linq2db --json number,title,state,closedAt,milestone,comments`. Verify state is `CLOSED`.
2. `Grep` `Tests/` for the issue number (broad search: both `<n>` and the `Issue<n>Test` / `Issue<n>` naming pattern). Filter for `[ActiveIssue]` attributes within ~5 lines of the match.
3. Catalogue the gated tests: per match, capture `<file>:<line>`, the test method name, the `[<DataSources>]` family it runs against, and any `#if <TFM>` guards.
4. If **zero** disabled tests reference the issue, stop and tell the user — there's nothing to enable. (Either the test was already enabled, never authored, or lives elsewhere.)
5. If multiple gated tests reference the issue, list them and ask which to enable in this PR (default: all).

Note: a single issue may have **multiple** disabled tests across different fixtures — e.g. one in `IssueTests.cs` for general scenarios and one in `CustomContextIssueTests.cs` for a provider-specific scenario. They may already be in different states (one enabled, one gated). Treat each separately.

### 2. Verify the test passes on current master

Invoke `/test` with:

- `testPattern` — `FullyQualifiedName~<TestClass>.<TestMethod>` (per gated test).
- `targets` — the project owning the test and currently-enabled providers in its TFM bucket. `/test` reads `UserDataProviders.json` and will surface "Provider X not enabled" if the matrix is empty; defer env changes to the user (don't auto-enable).

Three outcomes:

- **Pass on all targets.** Proceed to step 3.
- **Fail on at least one target.** Stop. The issue is closed but the test still fails — surface the failure detail and ask the user how to proceed. Possibilities: regression, partial fix, env mismatch, or wrong test scope.
- **No matching tests.** Filter typo. Re-check step 1's catalogue.

### 3. Bisect to find the load-bearing fix PR

Per the agent-rules guardrail, don't trust the close-comment attribution. Bisect to find the commit that flipped the test from fail → pass.

1. **Bound the range.** Earliest fail point ≈ the commit that introduced the test (find via `git log --diff-filter=A --follow -- <test-file>` or `git log -S "<TestMethod>" -- <test-file>`). Latest pass point = current `origin/master`.
2. **Set up a worktree.** Ask the user before creating one (worktrees need explicit OK per agent-rules). Suggested path: `../<repo>.bisect-<issue>`. Detach to the earliest fail-point candidate as starting HEAD.
3. **Provision the worktree's `UserDataProviders.json`.** Copy from the main repo's root per `worktree.md` → *`UserDataProviders.json` in a worktree*. Adjust the providers list to a small subset (typically SQLite.MS + one provider matching the test's domain). Containers required by that subset must already be running in the main session — the skill doesn't touch docker.
4. **Loop.** For each candidate commit:
   - `git -C <worktree> switch --detach <sha>`
   - Edit out `[ActiveIssue]` on the target test method (`Edit` only — do not commit in the worktree)
   - `dotnet test --project <project> --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~<TestClass>.<TestMethod>" -c Debug --settings .runsettings -p:TreatWarningsAsErrors=false -p:NoWarn=CS9336` — the `TreatWarningsAsErrors=false` + `NoWarn` flags are needed when bisecting across SDK upgrades (see [`windows-gotchas.md`](../../docs/windows-gotchas.md) → *Bisecting across SDK upgrades*); extend `NoWarn` with extra IDs as historic-code warnings surface.
   - Record pass/fail. Reset `git checkout -- <test-file>` before the next switch.
5. **Halve** the range each step. Typically 4–7 iterations covers ≤ 100 commits.
6. **Identify the transition.** The PR whose merge commit flips fail → pass is the citation. Confirm via `gh pr view <n> --repo linq2db/linq2db --json number,title,milestone,mergedAt,files`.
7. **Clean up the worktree.** `git worktree remove --force <path>` may fail with a permission error if dotnet is still holding files — follow [`worktree.md`](../../docs/worktree.md) → *Removing a worktree blocked by file locks*: `dotnet build-server shutdown` → `Remove-Item -Recurse -Force <path>` → `git worktree prune`.

If the bisect cost looks large up front (> 200 commits in the range), surface it to the user and ask whether to bisect at all or trust source-diff reasoning. Default: bisect; the user paid for the verification.

### 4. Branch + remove `[ActiveIssue]`

1. Switch to main repo (`C:\GitHub\linq2db.claude` typically); confirm working tree is clean (the worktree edits don't propagate — they're independent).
2. Create branch from fresh `origin/master`: `git fetch origin master` → `git switch -c issue/<n>-<kebab-slug> origin/master`. Slug per agent-rules; verb-led (`enable-enum-mapping-test`, `enable-cte-alias-test`).
3. If carrying `.claude/` from `infra/claude-curation` (per agent-rules → *Carrying `.claude/` curation across branch switches*), do it now: `git checkout origin/infra/claude-curation -- .claude/`. These changes must NOT be committed on this branch.
4. Edit each gated test to remove `[ActiveIssue]`. One-line change per test (the attribute on its own line).
5. Stage **only** the test file(s): `git add <test-file>`. If `.claude/` was carried, `git restore --staged .claude/` immediately after to drop the auto-stage.

### 5. Commit + push

Stop and confirm the commit message with the user before committing. Default template:

```
Enable <TestName> now that <subject> is fixed

Issue #<n> ("<title>") was closed by the reporter after verifying the
underlying bug was resolved. Bisected to:

  - #<fix-PR> ("<fix-PR-title>", <milestone>)
    <one-line description of what the fix did>

Verified locally on <providers> (<project> / <tfm>):
N passed, 0 failed. New baselines confirm <what the baseline change shows>.
```

After confirmation: `git commit`, then ask explicitly before `git push -u origin <branch>`. Per agent-rules → *Push to remote rules*, every push needs an explicit per-turn ask.

### 6. Draft PR

Per agent-rules → *Pull request rules*: always `--draft`, always `--assignee @me`, always confirm title + body. Include `Closes #<n>`.

Body should include:

- Closes line.
- One-paragraph context (issue closed by reporter, etc.).
- **Bisect table** with each tested commit + result. The transition row is the citation.
- The one-line source-diff that explains *why* it fixed the test (the actual code change from the fix PR).
- Local-verification result (provider × pass/fail).
- Mention any sibling tests for the same issue that were already enabled (and which PRs fixed them, if known).

Milestone: derive from the user-confirmed milestone. Typically the **next-version** milestone (the version this PR will ship in), **not** the version the bug was originally fixed in. Use that older version for the issue milestone in step 7.

After `gh pr create`, propose `/azp run test-all` per [`ci-tests.md`](../../docs/ci-tests.md) and wait for confirmation before posting. Re-request Copilot review (`gh pr edit <N> --add-reviewer copilot-pull-request-reviewer`).

### 7. Backfill the issue milestone

If issue #<n> has no milestone set, propose assigning it to the milestone of the **fix PR** identified in step 3 (this is retroactive attribution — the version the bug actually shipped fixed in).

- If that milestone is **open**: `gh issue edit <n> --repo <o>/<r> --milestone "<X.Y.Z>"`.
- If **closed** (common — bug shipped fixed in a past release): use REST API with the numeric id per [`github-authoring.md`](../../docs/github-authoring.md) → *`gh issue edit --milestone` rejects closed milestones*: `gh api -X PATCH repos/<o>/<r>/issues/<n> -F milestone=<id>`.

Milestone changes on issues authored by others are exempt from the *Never edit content authored by others* rule (metadata, not content).

### 8. Report

End with:

- Branch + PR number + URL.
- Bisect summary (range narrowed in N steps, fix PR cited).
- Per-test verification result.
- Issue milestone backfilled (yes/no + which milestone).
- Any sibling gated tests left unaddressed (if the user only chose a subset in step 1).

## Don'ts

- Don't trust the close-comment attribution. The reporter cites symptoms; the bisect cites the cause. Per agent-rules → *Enabling an `[ActiveIssue]` test after the issue closes*.
- Don't edit `UserDataProviders.json` or `docker start <name>` from this skill. Env state is the user's; if the bisect's provider matrix is empty, ask the user to enable providers via `/test-providers` and resume.
- Don't auto-create a worktree without explicit user OK (agent-rules → *Worktrees*).
- Don't commit `.claude/` carryover on the working branch. Stage only the test file(s).
- Don't push without an explicit user ask, even when the commit is ready (agent-rules → *Push to remote rules*).
- Don't backfill the issue milestone silently. Propose the value, get user OK, then PATCH.
- Don't expand scope beyond the test enablement — fixing the underlying bug if step 2 fails is out of scope; that's `/fix-issue` territory.
