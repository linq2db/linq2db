---
name: release-publish
description: Post-prep-merge release publishing flow. Opens the release PR (master → release), resets the linq2db.baselines repo HEAD to the documented anchor commit so CI regenerates clean baselines on the release PR, walks the user through merging the CI-generated baselines PR + copying-and-tagging fresh baselines on the releases branch in linq2db.baselines, then blocks at the prerelease-nuget team-test gate before authorizing the release PR merge. Every step is explicitly user-gated — no implicit chains across destructive or shared-state actions. Invoked by `/release` after the prep PR merges to master.
---

# /release-publish

## What this skill is (and isn't)

**Is:** the publish phase of release. Six gated steps from prep-PR-merged → release-PR-merged. Every step is its own user-confirmed action — destructive ops on shared repos (force-reset baselines), PR merges, tagging, and nuget publish triggers all require explicit go-aheads.

**Isn't:**

- Not the post-tag work (nuget publish verify, docs PR, GitHub release, next-version bump). That's [`/release-postpublish`](../release-postpublish/SKILL.md).
- Not for cherry-picking late fixes onto `release`. If a regression surfaces in step 5 (team-test gate), the skill records the pause and stops — re-opening the prep flow for fixes is the user's call.
- Doesn't run docker / build / test commands. CI does that on the release PR.

## When to run

- Automatically by `/release` orchestrator when the prep PR is detected as merged and `state.currentPhase` transitions to `publish`.
- Manually when resuming a release mid-publish phase.

## Required reading

- [`.claude/docs/release/external-repos.md`](../../docs/release/external-repos.md) — linq2db.baselines path + GitHub release template anchor.
- [`.claude/docs/agent-rules.md`](../../docs/agent-rules.md) → **Push to remote rules**, **Pull request rules**, **Git commit rules**.
- [`.claude/docs/release/first-run-todos.md`](../../docs/release/first-run-todos.md) → the baselines anchor commit + exact gh release invocation are first-run-confirmable.

## Phase state

The publish phase has 6 ordered steps with no implicit transitions. Each step records into `state.publish.steps.<key>` (managed by `release-state.ps1 -Action update`):

| Step | Key | Status flow |
|------|-----|-------------|
| 1. Open release PR | `release-pr-opened` | open → done |
| 2. Reset baselines master | `baselines-master-reset` | open → done |
| 3. Merge CI-generated baselines PR | `baselines-pr-merged` | open → done |
| 4. Copy + tag baselines on releases branch | `baselines-releases-tagged` | open → done |
| 5. Prerelease-nuget team-test gate | `team-test` | open → green / paused |
| 6. Merge release PR | `release-pr-merged` | open → done |

Skill never auto-advances. Each step prints the next action + waits.

## Procedure

### 1. Open release PR (`master` → `release`)

Preconditions:
- Prep PR merged to `master`.
- `state.currentPhase = 'publish'`.

Action:
1. Verify the release branch exists on origin: `gh api repos/linq2db/linq2db/branches/release` (404 → tell user to ensure `release` branch is set up, stop).
2. Confirm with user the title + body draft:
   - **Title:** `Release <version>` (e.g. `Release 6.3.0`)
   - **Body:** include the 6-step publish checklist (rendered from this skill's phase state — `release-state.ps1 -Action render` could optionally extend to publish phase; for now hand-build a markdown checklist with the same `<!-- release-state:checklist:start -->` / `:end` markers).
   - **Milestone:** the release milestone.
   - **Assignee:** `@me`.
   - **Draft:** yes (per `pr-and-push.md`).
3. On user confirmation, create:
   ```
   gh pr create --repo linq2db/linq2db --base release --head master --title "Release <version>" --body-file <path> --milestone <ver> --assignee @me --draft
   ```
4. Record PR number in `state.releasePR`. Update step status to `done`.

### 2. Reset baselines repo HEAD

This is a destructive operation on a shared repo. **Two-tier confirmation: describe + confirm + execute.**

Read the baselines anchor commit from [`first-run-todos.md`](../../docs/release/first-run-todos.md) (or its successor doc once stable). Current documented value: `f6b4f6278e5e53f38b6a26350f80b0609b37e86e` ("update gitattributes"). Surface to user on every release for confirmation — anchor may shift over time.

Action:
1. In `linq2db.baselines` clone (path from `external-repos.md`):
   ```
   git -C <baselines-path> fetch origin
   git -C <baselines-path> switch master
   git -C <baselines-path> log --oneline -5
   ```
2. Surface to user:
   > _"About to **force-reset** `linq2db.baselines` master to commit `<anchor-sha>` ("update gitattributes") and force-push. This discards N commits of baselines on top — they'll be regenerated cleanly by CI on the release PR. Confirm to proceed."_
3. On explicit `yes`:
   ```
   git -C <baselines-path> reset --hard <anchor-sha>
   git -C <baselines-path> push origin master --force-with-lease
   ```
   `--force-with-lease` (not bare `--force`) protects against races — if someone else pushed to master after our fetch, the push fails and we re-verify.
4. Update step status `done`.

### 3. Merge CI-generated baselines PR

CI runs on the release PR, regenerates baselines, opens a PR against `linq2db.baselines` master (head ref `baselines/pr_<release-pr-#>`).

Action:
1. Print: "wait for CI to open the baselines PR on `linq2db.baselines`. Notify me when it's open (or when CI completes — I can detect via `gh pr list --repo linq2db/linq2db.baselines`)."
2. Poll on demand:
   ```
   gh pr list --repo linq2db/linq2db.baselines --state open --search "baselines/pr_<n>"
   ```
3. When PR exists, surface its number + URL + diff stats. Ask the user:
   > _"Merge the baselines PR? (review the diff first — if it has unexpected non-test-change drift, pause and investigate.)"_
4. On `yes`:
   ```
   gh pr merge <baselines-pr> --repo linq2db/linq2db.baselines --merge
   ```
   (Use `--merge` not `--squash` — preserves the per-test commit history that baselines workflows depend on.)
5. Update step status `done`. Capture the merge commit SHA in `state.publish.baselinesMergeSha`.

### 4. Copy fresh baselines to releases branch + tag

After the master baselines have the release commit, copy it onto the `releases` branch in `linq2db.baselines` and tag with the release version.

Action:
1. In `linq2db.baselines`:
   ```
   git -C <baselines-path> fetch origin
   git -C <baselines-path> switch releases
   git -C <baselines-path> merge --ff-only origin/master    # or cherry-pick the specific commit
   ```
   If FF isn't possible (releases has diverged), stop and surface — likely a previous release tag's leftover that needs untangling first.
2. Tag:
   ```
   git -C <baselines-path> tag <version>      # plain tag, no `v` prefix unless the project's tags use it (verify on first run)
   git -C <baselines-path> push origin releases --tags
   ```
   On first run, confirm the tag-name convention by reading `git -C <baselines-path> tag -l` — record in `first-run-todos.md` if non-obvious.
3. Update step status `done`.

### 5. Prerelease-nuget team-test gate [R2-A in plan]

CI on the release PR produces prerelease nugets to pipeline artifacts. This step blocks until the user confirms team tests passed.

Action:
1. Pull the artifact link from the CI build:
   ```
   pwsh -NoProfile -File .claude/scripts/azp-build-failures.ps1 -BuildId <buildId>
   ```
   (Even when there are no failures, the script's output includes the build's pipeline URL — surface that to user.)
   Or `gh pr checks <release-pr> --json link` to get the build run, then construct the artifact URL.
2. Print:
   > _"Prerelease nugets ready at `<CI artifact URL>`. Notify the team for their custom testing (whatever validation they own outside CI test-all). Reply `tests green, proceed` when ready, or `regression found, paused: <description>` to pause the release."_
3. Block. Two valid replies:
   - **`tests green, proceed`** → update step status `green`. Continue to step 6.
   - **`regression found, paused: <reason>`** → update step status `paused` with reason in `state.publish.steps.team-test.note`. Skill stops. On resume:
     1. Ask: "regression fixed? `fixed` to re-test; `still investigating` to wait; `abort release` to abandon."
     2. On `fixed`: ask "where was the fix landed — back on `release-prep/<ver>` (extends prep), or directly on `release`?" — depends on user judgement and project conventions. The skill records the answer; the actual fix is user-driven.
     3. After fix, return to step 5.1 (re-fetch nugets, re-notify team).

### 6. Merge release PR

Preconditions:
- All earlier steps `done` or `green`.
- User explicitly typed `tests green, proceed`.

Action:
1. Verify mergeability:
   ```
   gh pr view <release-pr> --repo linq2db/linq2db --json mergeable,mergeStateStatus,headRefOid,baseRefOid
   ```
2. Print the merge command — **do not run it**:
   ```
   gh pr merge <release-pr> --repo linq2db/linq2db --merge
   ```
   (`--merge`, not squash — `release` branch should reflect the linear `master` history that's been validated.)
3. Ask user to run the merge themselves and confirm done. Skill detects via:
   ```
   gh pr view <release-pr> --repo linq2db/linq2db --json state,mergedAt
   ```
4. On `state == "MERGED"`:
   - Update step status `done`.
   - Update `state.currentPhase = 'postpublish'`.
   - Dispatch to `Skill('release-postpublish')`.

## Don'ts

- Do **not** auto-run `git reset --hard` or `git push --force` in step 2. Always two-tier confirm (describe + confirm + execute), and always with `--force-with-lease`, never bare `--force`.
- Do **not** auto-merge the baselines PR in step 3 or the release PR in step 6. The skill prepares + verifies + asks; the user runs the merge.
- Do **not** skip step 5 (team-test gate). CI test-all passing is necessary but not sufficient — the project ships consumed by external testers, and their feedback is the last release-blocking signal.
- Do **not** transition `state.currentPhase` from `publish` to `postpublish` before step 6 confirms `MERGED`. Premature transition strands the publish phase mid-flow on session resume.
- Do **not** treat a paused team-test as a release failure to retry automatically. It's a deliberate human-driven decision — wait for explicit user direction on resume.
- Do **not** silently use a different baselines anchor SHA than the one documented. If the anchor needs to change, the user updates `first-run-todos.md` (or a successor doc) and confirms the new value.
