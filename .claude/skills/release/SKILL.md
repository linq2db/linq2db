---
name: release
description: One-stop orchestrator for the linq2db release-preparation workflow. Surveys release state on the active branch, renders a two-level checklist (0 branch+version, 1 deps, 2 PublicAPI, 3 milestone-check, 4 test-matrix, 5 release-notes, 6 final-verification + ad-hoc 7.<n>), and dispatches each task to its dedicated sub-skill (`/release-deps`, `/release-publicapi`, `/release-milestone-check`, `/release-test-matrix`, `/release-notes` + `/release-notes-validate`, `/release-verify`, then `/release-publish` post-merge, `/release-postpublish` post-tag). State is persisted in the release PR body (canonical) and mirrored to `.build/.claude/release-<version>.json` for session resume. Use when the user says "release", "/release", "start a release", or "resume release".
---

# /release

## What this skill is (and isn't)

**Is:** the release-preparation dispatcher. It owns the per-release checklist + state file, dispatches each task to its sub-skill, runs the CI-status probe after every task, and handles transitions between prep / publish / postpublish phases. Mirrors `/chores`'s table-and-pick model but for a single multi-day release rather than weekly maintenance.

**Isn't:**

- Not the actual worker — every checklist item dispatches to a dedicated skill (`/release-deps`, `/release-publicapi`, `/release-milestone-check`, `/release-test-matrix`, `/release-notes` + `/release-notes-validate`, `/release-publish`, `/release-postpublish`, plus existing `/version-bump` and `/api-baselines`).
- Not for a fresh repo-wide maintenance scan — that's `/chores`.
- Not silent — every push, merge, milestone-create, force-reset, and nuget-publish trigger is an explicit user-confirmed step.

## When to run

User-invoked. Good moments:

- Starting a new release (milestone is ready to ship).
- Resuming a release after a session break (state file or PR body checklist still has open items).
- Asking "where am I on this release?" mid-flow.

## Required reading

Read these once at start (not on every re-entry):

- [`.claude/docs/release/overview.md`](../../docs/release/overview.md) — high-level layout + cross-links.
- [`.claude/docs/release/branch-and-pr.md`](../../docs/release/branch-and-pr.md) — branch name + PR body conventions.
- [`.claude/docs/release/external-repos.md`](../../docs/release/external-repos.md) — sibling clone paths (linq2db.docs, linq2db.baselines), wiki page names, GitHub release template anchor.
- [`.claude/docs/agent-rules.md`](../../docs/agent-rules.md) → **Creating a new branch**, **Git commit rules**, **Push to remote rules**, **Pull request rules**.

## Procedure

### 0. Session-reload notice

Whenever this skill or any sub-skill instructs the user to edit `.claude/` (record a new package rule, add a provider init step, add an external-repo path, etc.), the turn must end with:

> 📌 Reload session to pick up `.claude/` edits before continuing.

Same applies whenever a new sub-skill is added or an existing one materially changes during the interactive build-out of this skill set.

### 1. Resolve target + ad-hoc tasks

1. Run `git branch --list 'release-prep/*'` and `git ls-remote --heads origin 'release-prep/*'`. If a `release-prep/<ver>` branch exists locally or on origin, **resume that release**: capture the version, jump to step 2.
2. Otherwise: this is a new release.
   - Fetch open milestones: `gh api repos/linq2db/linq2db/milestones?state=open --jq '.[] | {title, number, open_issues, closed_issues, due_on}'`.
   - Render a numbered list, default to the **lowest-version** milestone whose title parses as `M.m.p`. Ask the user to confirm or pick.
   - Ask: "any release-specific tasks beyond the standard set (deps / PublicAPI / milestone-check / test-matrix / release-notes)? Free-form, one per line, or 'none'." User-supplied entries become numbered checklist items `6.<n>+` after the standard 0–5.
   - Show the normalized ad-hoc list back for one confirm-or-edit gate.
   - Tell the user: "ready to create branch `release-prep/<ver>` from `origin/master`, plus PR `Release prep <ver>` (draft, assigned to @me, milestone `<ver>`, body = initial checklist). Confirm to proceed."
   - On confirmation:
     - `git fetch origin master`.
     - `git switch -c release-prep/<ver> origin/master`.
     - Apply `/version-bump`'s edit logic on `release-prep/<ver>` (see step 1a below — do **not** invoke the `/version-bump` skill unchanged here, since that would create the `infra/bump-versions` branch, which is wrong for release prep).
     - Commit the version bump on `release-prep/<ver>` (not `infra/bump-versions` for this case).
     - Push, create the prep PR with the initial checklist body, assign milestone.
     - Tick `[x] 0. Branch + version bump`.

**1a. `/version-bump` on the prep branch.** Existing `/version-bump` creates `infra/bump-versions` and edits `Directory.Build.props`. For the release-prep flow, perform the same edits on the already-created `release-prep/<ver>` branch instead. The edit logic (set `<Version>` to milestone, increment each `<EFxVersion>` minor by 1, one batched Edit) is identical — just the branch is different. Do not create `infra/bump-versions` here. Tell the user this explicitly when dispatching: "running /version-bump's edit logic on release-prep/<ver>, not on a separate infra/bump-versions branch — same edits, different branch."

### 2. Load state

1. Determine state-file path: `.build/.claude/release-<version>.json`.
2. Call `release-state.ps1 -Action load -Version <ver> -PrepPR <n>` — this:
   - Reads the state file if present.
   - Else reads the PR body's checklist (via `gh pr view <n> --json body`) and synthesizes state from it.
   - Else creates default state from the milestone + ad-hoc list.
   - Returns the merged state JSON.
3. **PR body wins on conflict**: if both a state file and PR body checklist exist and disagree (e.g. user manually flipped a `[ ]` to `[x]` on the PR), the PR-body state is canonical. The script handles this — agent just trusts the returned state.

#### 2a. Pre-fetch discovery (optional)

For each release, the read-only discovery phases of tasks 1 (deps), 2 (PublicAPI), 3 (milestone-check), and 5 (release-notes-validate) can run in parallel before any user picks. Pre-fetched plans are cached at `.build/.claude/release-<ver>-{deps,publicapi,milestone,notes}-plan.json` and consumed by the sub-skills at dispatch time, eliminating re-discovery on each task pick.

Offer pre-fetch when at least two of tasks 1/2/3/5 are still `open`:

> _"Pre-fetch discovery for tasks 1/2/3/5 in parallel? Wall-clock ≈ length of the slowest sub-task (PublicAPI's `dotnet build` typically dominates at 2-3 minutes; the other three are 10-30s each over `gh api` + `nuget.org`). Reply `yes` / `skip` / `subset 1,3,5` to choose."_

On `yes`:

```
pwsh -NoProfile -File .claude/scripts/release-prefetch.ps1 -Action discover-all -Version <ver> -Milestone <ver> -PrepPR <n> -SkipFresh
```

The script's output shows per-task `status` (`ok` / `cached` / `error`) and elapsed time. Surface failures to the user — a script error here is a real issue (build broke, gh auth lapsed, etc.) and blocks the next phase.

**Freshness rule.** A cached plan is considered fresh for 30 minutes (default; tunable via `-FreshnessMin`). Older plans get re-discovered. Sub-skills check plan-file mtime when dispatched and re-run discovery themselves if stale — pre-fetch is a wall-clock optimization, not a contract. **A plan older than ~1 hour, or any plan from before the last commit on master, must be re-fetched** (master may have new merged PRs that change milestone / notes audit results).

**Status probe.** To inspect what's cached without re-fetching:

```
pwsh -NoProfile -File .claude/scripts/release-prefetch.ps1 -Action status -Version <ver>
```

Returns per-task `exists` / `ageMinutes` / `sizeBytes`. Useful when resuming a session and deciding whether a fresh pre-fetch is worth the wall-clock vs. running tasks one-at-a-time.

### 3. Render status table

Call `release-state.ps1 -Action render -StateFile <path>`. Output looks like:

```
Release 6.3.0 — branch release-prep/6.3.0 — PR #5530 — phase: prep

  [x] 0. Branch + version bump
  [x] 1. Dependencies update              (5 updates applied, 2 skipped)
  [~] 2. PublicAPI reconciliation         (in progress)
  [ ] 3. Milestone check
  [ ] 4. Test matrix
        [-] 4.1 Provider container + DB init   (skipped — already initialized)
        [ ] 4.0 T4 binary prerequisite
        [ ] 4.2 DB2 iSeries decision
        [ ] 4.3 LINQPad 5 (.lpx)
        ...
  [ ] 5. Release-notes validation
  [ ] 6.1 [ad-hoc] Audit IDataProvider surface for breaking changes

Next: continue task 2 → /release-publicapi
```

Status tokens: `[ ]` open · `[x]` done · `[~]` in progress · `[-]` user-skipped.

### 4. Dispatch + CI watch

For each user pick (or the "next recommended" task):

1. **CI probe.** Before dispatching, call `release-state.ps1 -Action ci-probe -StateFile <path> -PrepPR <n>`. The probe runs `gh pr checks <n> --json name,status,conclusion,detailsUrl` and diffs run IDs against `state.ci.lastReportedRunIds[]`. On a **new** failed/cancelled run, fetch top-of-log via `pwsh -NoProfile -File .claude/scripts/azp-build-failures.ps1 -BuildId <buildId>` and surface a compact summary (job name + first failing test/build error). User must acknowledge ("noted" / "investigate" / "ignore for now") before continuing — `release-state.ps1 -Action ci-ack` records the run IDs as reported.

2. **Invoke sub-skill.** Map the pick to the right skill:

   | Task | Skill |
   |------|-------|
   | 0 (already done after step 1) | (no dispatch — handled inline) |
   | 1 Dependencies | `Skill('release-deps')` |
   | 2 PublicAPI | `Skill('release-publicapi')` |
   | 3 Milestone check | `Skill('release-milestone-check')` |
   | 4.x Test matrix | `Skill('release-test-matrix')` (skill handles sub-track selection) |
   | 5 Release notes | `Skill('release-notes')` → mode `sweep` (backfill drafts + wiki for user-merged PRs) then `harvest` (assemble the GitHub-release brief), then `Skill('release-notes-validate')` for the final coverage check |
   | 6 Final verification | `Skill('release-verify')` — see ordering note below |
   | 7.x Ad-hoc | dispatch is per-task — describe the entry to the user and ask which sub-tool / sub-skill to invoke; record the chosen handler in state for future resume |

   **Ordering note for task 6:** Task 6 is the **build gate, not the final gate**. It must run **after tasks 1, 3, 5** (and any ad-hoc 7.x that doesn't itself need a clean build) and **before tasks 2 and 4** — both of which depend on a clean build state that task 6 establishes:

   - Task 2 (`/release-publicapi`) normally builds in its own step 1 to surface RS0016/RS0017 diagnostics. Once task 6 has run, those diagnostics are already addressed (or recorded in `Unshipped.txt`), so task 2 can move `Unshipped → Shipped` without re-building.
   - Task 4 (`/release-test-matrix`) tests built artifacts; task 6 produces the verified-clean build the matrix exercises.

   `/release-verify` runs the only Release build of the release-prep cycle (no other sub-skill is allowed to run a verification build). It owns the `build → fix-or-disable analyzer errors → /api-baselines refresh → consolidated commit` loop end-to-end and runs **once** per release prep. If the user picks task 6 before tasks 1/3/5 are done, refuse and surface the open prerequisites first.

3. **Wait for sub-skill to complete its loop.** This skill never preempts another skill's interactive flow.

4. **Refresh state.** When the sub-skill returns, call `release-state.ps1 -Action update -StateFile <path>` with the task ID and the sub-skill's status (`done` / `partial` / `skipped` / `aborted`). The script also calls `release-state.ps1 -Action sync-to-pr -StateFile <path> -PrepPR <n>` to push the checklist refresh into the PR body in-place (preserving any non-checklist prose).

5. **Re-render table**, loop.

### 5. Prep-merge gate

When all standard tasks (0–6) and all ad-hoc tasks are `[x]` or `[-]`:

1. Print the final state table.
2. Ask explicitly: **"go for prep PR merge?"** — single confirmation, no implicit chain.
3. On "yes":
   - Confirm the PR is mergeable: `gh pr view <n> --json mergeable,mergeStateStatus,headRefName,baseRefName`.
   - Tell user the merge command but **do not** run it yourself — merging the prep PR is a deliberate user action.
   - Once user reports the merge done (or skill detects `state=MERGED` on next probe), update state: `currentPhase = "publish"`, write `.build/.claude/release-<ver>.json`, transition to `Skill('release-publish')`.

### 6. Publish + postpublish phases

After prep-PR merge, the orchestrator dispatches to `/release-publish`, then on tag to `/release-postpublish`. Those skills own their own internal steps and gates — this orchestrator only renders the high-level phase + recent-step summary on re-entry.

When a sub-skill in publish or postpublish completes, it calls `release-state.ps1 -Action update -Phase publish|postpublish -Step <key> -Status done` and the orchestrator re-renders.

## Push semantics rule

Every push to `release-prep/<ver>` cancels any in-flight CI test-all run. This skill (and all sub-skills it dispatches) **never pushes implicitly**.

Before every push:

1. State the change being pushed (one line: "pushing N commits: <subjects>").
2. Confirm explicitly with the user.
3. After the push, ask: "re-trigger `/azp run test-all`?"
   - Default **yes, re-trigger** if user is mid-other-prep and CI was already in flight.
   - Default **no** if the just-pushed change was non-functional (whitespace, comment, doc-only).
4. On `yes`, dispatch via `.claude/scripts/azp-run.ps1` (see [`ci-tests.md`](../../docs/ci-tests.md)) targeting `test-all` on the prep PR.

Same rule applies to force-pushes after rebase.

## First-run docs

Every sub-skill that hits an unknown (new package without a known release-notes URL, new provider without a recorded DB-init invocation, new external-repo path) writes the user-provided answer to the corresponding `.claude/docs/release/<topic>.md` and ends the turn with the session-reload notice from step 0. This is how the skill set learns across releases.

## Don'ts

- Do not run any sub-skill's work inline. The contract is that each sub-skill owns its own approval gates.
- Do not auto-commit, auto-push, auto-merge, auto-tag, auto-publish. Every shared-state mutation needs an explicit user request (per `agent-rules.md` → **Git commit rules** / **Push to remote rules** / **Pull request rules**).
- Do not silently skip a checklist item. Every `[-]` skip needs the user's explicit "skip this".
- Do not edit `.claude/` files committed on the `release-prep/<ver>` branch. `.claude/` changes always live on `infra/claude-curation` per `agent-rules.md` → **Carrying `.claude/` curation across branch switches**. The orchestrator surfaces this rule whenever it switches branches.
- Do not assume `/version-bump`'s default `infra/bump-versions` branch is the right target during release prep — it's not. Run the edit logic on `release-prep/<ver>` directly (step 1a).
