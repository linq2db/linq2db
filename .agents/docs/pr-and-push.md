## PR and push procedures

Detail-heavy mechanics for creating PRs and pushing follow-ups. The summary in [`agent-rules.md`](agent-rules.md) → *Push to remote rules* / *Pull request rules* keeps the principles and one-line triggers; this doc is what you load when one of those triggers fires.

### After every successful push: PR body check

Check for a PR on the branch (`gh pr list --head <branch> --json number,title,body,url`):

- If **no PR exists**, propose creating one (see [`Creating a PR`](#creating-a-pr)) and wait for confirmation.
- If **a PR exists**, diff the newly pushed commits against the current PR body. If the body no longer accurately describes the work (new summary bullets, new linked issues, etc.), propose a concrete edit and wait for confirmation before calling `gh pr edit`. **Show the proposed change as a diff between the current body and the new one** (e.g. a unified diff or `- old line` / `+ new line` markers) — do not just paste the new body in full. If the body is still accurate, say so and move on — don't edit gratuitously.
- **When the body update follows a follow-up commit on the user's own PR, append — don't rewrite.** Add a new subsection (typically `## Follow-up commit` or similar) summarising the new commit's deltas and leave the original prose verbatim. Don't paraphrase, restructure, or "neutralise" content the human author already wrote. The "preserve, don't rewrite" rule is suspended only when the user explicitly asks for a tone or structure change to the existing body.
  - **Use `.agents/scripts/pr-body-edit.ps1` to do the append** — it inserts text at an ASCII anchor (e.g. a heading or trailer line near the PR-body footer, to land a section above it) in a single allowlisted, UTF-8-safe call. Do **not** hand-roll a fetch-body → mutate → `gh pr edit` loop (Python/pwsh): that re-hits the cp850 encoding garble the script exists to avoid, and a `gh … > .build/.agents/x.json` redirect lands in the Bash cwd (which may be a **worktree**, not the primary clone) so a follow-up reader using the primary-clone path fails with `FileNotFound`.

### After every successful push: re-request Copilot review

Copilot's automatic trigger is unreliable — it sometimes doesn't fire on follow-up pushes — so re-request after each successful push (and after the PR-body check above):

```
gh pr edit <N> --repo linq2db/linq2db --add-reviewer copilot-pull-request-reviewer
```

Two slug / endpoint gotchas:

- `gh pr edit --add-reviewer` routes through GraphQL with the bot's user-login (`copilot-pull-request-reviewer`). Passing `Copilot` to `gh pr edit --add-reviewer` errors with `Could not resolve user with login 'copilot'`.
- The REST equivalent (`gh api -X POST repos/.../requested_reviewers -f 'reviewers[]=Copilot'`) accepts the `Copilot` slug, but **silently no-ops when Copilot already reviewed an earlier commit on the same PR** — it returns 200 yet the bot is not re-queued. Always prefer `gh pr edit` for follow-up requests.

After the review lands, fix or reply per thread and resolve via the existing helpers — `gh api repos/.../pulls/<N>/comments` for inline bodies, `gh pr view <N> --json reviews,latestReviews` for the review-level overview. Bulk reply + resolve goes through `.agents/scripts/post-pr-thread-replies.ps1` (see [`github-review-api.md`](github-review-api.md) → **Batch reply + resolve**); GitHub doesn't auto-resolve threads when a follow-up commit fixes the line.

### After every successful push: refresh the release-notes draft

If the PR already has a release-notes draft comment (the marker `<!-- release-notes:draft:`), the pushed commits may have changed what ships. Refresh it via [`/release-notes`](../skills/release-notes/SKILL.md) → mode `refresh <pr>`:

- `release-notes-draft.ps1 -Action find -Pr <n>` reports `present` + the `lastSha` the draft was generated from. If `present: false`, **do nothing** — drafts are created on explicit request (`/release-notes draft`), never auto-created on a stray push. If `lastSha` equals the new PR HEAD, it's a no-op.
- When HEAD moved, regenerate the proposed text, **show the user the change and confirm**, then `upsert`. The maintainer verifies correctness on every update — so regeneration is safe, but it's still gated by that confirm.

Fold this into the push bundle alongside the PR-body check, Copilot re-request, and baselines cleanup.

### Stale CHANGES_REQUESTED reviews after follow-up commits

Despite branch protection's `dismiss_stale_reviews: true`, GitHub's auto-dismissal sometimes lags or doesn't fire on rebase-merges from a different actor. After pushing follow-up commits to address a `CHANGES_REQUESTED` review, check `gh pr view <n> --json reviewDecision`. If it still shows `CHANGES_REQUESTED`, the stale review must be dismissed manually before `gh pr merge --admin` will succeed (without it, the merge fails with `Repository rule violations found / 1 review requesting changes by reviewers with write access`).

Manual dismissal:

```
pwsh -NoProfile -File .agents/scripts/dismiss-stale-reviews.ps1 -Pr <n>          # add -DryRun to preview
```

It resolves the numeric REST review ids (GraphQL ids from `gh pr view --json reviews` won't work for the PUT) and dismisses each `CHANGES_REQUESTED` review with a non-empty `-Message` (default `Stale`).

Two gotchas on the dismissal call:

- `-f message=""` is **rejected** with HTTP 422 — GitHub mandates a non-empty message. Use a one-word placeholder like `"Stale"` if there's nothing more to say (per [`github-authoring.md`](github-authoring.md) → *Wording discipline*: terse > apologetic).
- The dismissal is a metadata change, not a content edit, so it's exempt from the `never edit content authored by others` rule (per [`github-authoring.md`](github-authoring.md) → *Never edit content authored by others*). Still, ask the user before dismissing — visible action on someone else's review.

### After test renames / moves / deletes: clean up stale baselines

`linq2db.baselines` files are keyed by the fully-qualified test name (`<Namespace>.<Fixture>.<Method>(<Provider>).sql`). When follow-up commits rename a test method, rename its fixture class, change its namespace, or delete the test, the existing baselines PR for the linq2db PR — typically `linq2db/linq2db.baselines#<m>`, linked from the bot comment "Test baselines changed by this PR" — carries files keyed to the *old* names and never auto-prunes. Leaving it open means the next CI run produces a second baselines PR while the stale one lingers; the diff between the two is hard for a reviewer to read.

After pushing follow-up commits that rename / move / delete any test:

Run **`pwsh -NoProfile -File .agents/scripts/close-stale-baselines.ps1 -Pr <n>`** (add `-DryRun` to preview). It finds the baselines PR keyed to head `baselines/pr_<n>`, closes it with an explanatory comment, deletes the branch ref (treating an already-gone ref as success), and `git fetch --prune`s the local `../linq2db.baselines` clone. The next CI run (e.g. `/azp run test-all` on the renamed commit) then produces a fresh baselines PR under the up-to-date names — no further manual action.

The same applies when a follow-up commit changes a test's projection shape / SQL output without renaming it — the old PR's files no longer match the new expected output. Out of scope: pure test *additions* that don't rename anything (the existing baselines PR is incremental — new files just get added on the next CI run), and bug-fix commits that update SQL but leave both names and structure untouched (the existing baselines PR's diff updates in-place).

**Also covers the case where the baselines PR became `CONFLICTING` because *other* source PRs landed on master first.** The baselines PR is keyed against a specific source-PR commit; once master moves, the baselines diff often no longer applies cleanly even if the source PR's tests didn't change. Same close+delete-branch action — the next CI run on the now-merged source PR (or its squashed master commit) regenerates fresh baselines under master's current state. Don't try to merge-resolve a baselines PR; the cost of regenerating is much lower than the cost of getting the resolution wrong.

Both `/review-pr` (in interactive-mode `fix`-path post-walk) and `/verify-review` (when a partial-fix follow-up renames a test) trigger this cleanup. Make it part of the publish bundle that pushes the follow-up commits — push, body update, Copilot re-request, baselines close+delete-branch, `/azp run test-all`.

### On PR merge

When merging a PR (or right after the user reports having merged one), two release-bookkeeping tasks run — both user-confirmed:

1. **Milestone consistency.** A merged PR and the issues it closes should share a milestone. Run `pwsh -NoProfile -File .agents/scripts/milestone-consistency.ps1 -Action check -Pr <n>`. If it reports `laggards`, propose assigning the PR's milestone to them and, on confirmation, run `-Action assign -Pr <n>` (REST PATCH by numeric milestone id; verifies after). Laggards flagged `likelyIntentional` (issue on an earlier/closed milestone — fix shipped in a past release, this PR is a follow-up) are skipped by default; don't reassign them unless you really mean to (`-IncludeReleased`). Milestone is metadata so it's exempt from "never edit content authored by others", but the change is visible — **propose, then confirm**. (Same check fires from `/review-pr` when a discrepancy surfaces and from `/release-milestone-check`.)
2. **Release notes draft** — ensure the PR has a draft comment (`/release-notes draft <pr>` if missing). This always happens; the wiki write does not.
3. **Release notes → wiki (optional, explicit).** The team often updates the wiki right after merge so users can preview what's coming — but it's an **opt-in step the user requests explicitly**, not automatic on merge. The authoritative full-section generation is at release prep (`/release` task 5 → `sweep`/`harvest`/`apply`). When the user asks, run [`/release-notes`](../skills/release-notes/SKILL.md) → mode `apply` (wiki strategy B: regenerate the whole version section in the local `linq2db.wiki` clone → show the diff → push on confirm). Skip `omit`-flagged drafts. Because the section is regenerated wholesale, assemble the cumulative bullet set via `/release-notes harvest --milestone <ver>` rather than a single bullet when more than one PR has merged into the version.

If the PR was merged **by the user outside the agent**, both tasks are missed — they're backfilled later by `/release-notes sweep` (release prep task 5, or ad-hoc).

### Creating a PR

When creating a PR on `linq2db/linq2db`:

- **Always open as draft** (`gh pr create --draft`). Never publish a ready-for-review PR unless the user explicitly asks.
- **Confirm title and body with the user before running `gh pr create`.** Propose both, wait for approval, then create.
- **Link referenced issues/tasks as closed on merge.** If the work targets a known issue or task, include `Fixes #<n>` / `Closes #<n>` in the PR body so GitHub auto-closes it when the PR merges. One keyword per issue.
- **Assignee.** Assign the PR to the current GitHub user (`gh pr create --assignee @me`) unless the user specifies someone else. If `@me` resolution fails with a transient `502 Bad Gateway` (it does a live API call during create, and the PR is **not** created when it fails), resolve the handle explicitly — `gh api user --jq '.login'` — and pass it as `--assignee <login>` on the retry.
- **Milestone.**
  - If the linked issue/task has a milestone, reuse it.
  - Otherwise ask the user to pick one. Fetch open milestones via `gh api repos/linq2db/linq2db/milestones?state=open` and present a **numbered list** (so the user can reply with just a number) in this order:
    1. The **next-version milestone** (matching `<Version>` in `Directory.Build.props`, or the closest upcoming version) — always first.
    2. Remaining **versioned** milestones (titles starting with a digit, e.g. `6.x`, `7.0.0`), sorted by version.
    3. **Non-versioned** milestones (e.g. `Backlog`, `In-progress`), sorted alphabetically by title.
- **CI run proposal.** After `gh pr create`, propose running the full provider matrix on Azure Pipelines via a `/azp run test-all` comment. See [`ci-tests.md`](ci-tests.md) for the trigger syntax and when a narrower `/azp run test-<dbname>` makes more sense. Wait for the user to confirm before posting the comment.

### Setting a PR's project-board lane

linq2db PRs are tracked on org **Project #8 "PR Review Queue"** (id `PVT_kwDOAA01hc4BZqGZ`). The `Status` field (id `PVTSSF_lADOAA01hc4BZqGZzhUnP5s`) has options: `Todo` · `In Progress` (`47fc9ee4`) · `Waiting For Review` · `In Review` · `Done`. "Work In Progress" lane = **In Progress**. When the user asks to put a PR in a lane:

```
gh project item-add 8 --owner linq2db --url <pr-url> --format json   # returns the project item id
gh project item-edit --id <item-id> --project-id PVT_kwDOAA01hc4BZqGZ \
  --field-id PVTSSF_lADOAA01hc4BZqGZzhUnP5s --single-select-option-id <option-id>
```

Re-fetch option ids via `gh project field-list 8 --owner linq2db --format json` if they ever drift.

### Extending an open PR

Commits that extend an open PR's scope go on that PR's branch, not a new parallel branch. When a review session surfaces an ancillary fix (apostrophe-escape bug found while reviewing #5463, a test regression caused by the PR, a missing guardrail) and the user asks for it as a follow-up, push it onto the PR's existing head branch — don't create a sibling `feature/*` branch and propose a second PR. Mechanics:

- Check `gh pr view <n> --json maintainerCanModify,headRepository,headRefName`. If `maintainerCanModify: true` and `headRepository` is a fork, add the author's fork as a git remote if not already present (`git remote add <owner> https://github.com/<owner>/<repo>.git`) and push via refspec: `git push <owner> <local-branch>:<headRefName>`. The PR auto-updates with the new commit. Propose a body update when the new commit extends the PR's originally described scope (follow the [After every successful push: PR body check](#after-every-successful-push-pr-body-check) flow above).
- If `maintainerCanModify: false`, stop and ask — either the author has to apply the change themselves, or the work needs a separate PR. Don't unilaterally open a parallel branch when the intent was a follow-up commit.
- **`maintainerCanModify` only gates *fork* PRs.** Check `isCrossRepository` (`gh pr view <n> --json isCrossRepository,headRepositoryOwner`) first: for a **same-repo** PR branch (`isCrossRepository: false`, head owner `linq2db`), `maintainerCanModify` is irrelevant — anyone with repo write pushes straight to `origin <headRefName>` (with user approval). Don't apply the "stop and ask" rule above to a same-repo branch. (PR #5604: `maintainerCanModify: false` but same-repo — a regression-test commit pushed directly to `feature/prefer-client-calculation`.)
- When pushing to someone else's fork, neutralize accidental pushes afterward if the remote is no longer needed (`git remote set-url --push <owner> no_push` as a guard, or `git remote remove <owner>` if you want it gone). Confirm with the user which — "disable" can mean either.

### Renaming a branch that is the head of an open PR closes the PR

GitHub's branch rename (UI, or `POST repos/<o>/<r>/branches/<b>/rename`) updates the *base* branch of open PRs but **closes** a PR that uses the renamed branch as its *head* — it does not retarget head-side PRs. The old name then resolves only as a redirect, so the closed PR **cannot be reopened** (`reopenPullRequest` fails with "Could not open the pull request"). Recovery is a fresh PR from the renamed branch (reuse the old PR's title/body via `pr-body-edit.ps1`, link back), which loses the original review thread. So **defer a curation / feature branch rename until after its open PR merges** — no head-side PR to close then — or accept the new-PR cost. (Surfaced renaming `infra/claude-curation` → `infra/agents-curation`: it closed PR #5521; #5670 replaced it.)

### Amending a commit on a non-checked-out branch with a dirty current tree

Don't `stash` → `switch` → `--amend` → `switch -` → `stash pop` — the pop can conflict on overlapping files. Use **`pwsh -NoProfile -File .agents/scripts/amend-branch-commit.ps1 -Branch <branch> -Message <text>`** (`-MessageFile <path>` for multi-line; `-Sign` if the original was GPG-signed). It reuses the branch tip's tree (a message/metadata amend — content unchanged), rebuilds the commit object preserving the original author, and atomically retargets the ref with the old-SHA safety check — all while staying on the current branch.

### Merging master into a feature PR — recurring conflict recipes

When syncing `origin/master` into a long-lived feature PR, three collisions recur:

- **`LinqOptions` (positional record) params: update all five sites.** A master-vs-PR collision on its parameter list (both sides add options before the trailing param) recurs on most option-adding PRs. Keeping *all* new params means updating, consistently: (1) the **primary constructor** param list, (2) the **copy constructor** assignments, (3) the **`ConfigurationID`** hash `.Add(...)` calls, (4) **`PublicAPI.Unshipped.txt`** (the full ctor signature *and* the `Deconstruct` signature, in source param order), and (5) the **binary-compat shim** — the `Deconstruct` shim's trailing `out _` discard count must equal the number of params added after the shim's last named one. A Release `net10.0` build of `Source/LinqToDB/LinqToDB.csproj` validates both the ctor/Deconstruct arity and the RS0016 PublicAPI match. (Surfaced on PR #5450: master added `PreferClientCalculation`, the PR added `DefaultEagerLoadingStrategy` + `ImplicitCollectionLoading`.)
- **A merge that inserts a parameter *ahead* of an existing optional one breaks positional callers silently — convert them to named arguments.** When resolving a method-signature conflict by keeping params from both sides (e.g. the PR's pre-build `adjustArguments` hook *and* master's post-build `transform` hook on `TranslateWindowFunction`), an auto-merged caller that passed a *later* argument **positionally** now binds it to the wrong slot. A Release build catches it only when the types differ; a same-type mis-bind compiles and runs wrong. After such a merge, audit every caller of the widened method and switch later positional args to **named** (`transform: f => …`). (Surfaced syncing PR #5468: master's `TranslateRowNumber` passed its ROW_NUMBER-cast lambda positionally, which would have bound to `adjustArguments` after the merge added it ahead of `transform`.)
- **Merging an end-appended serialized enum keeps the master side's members first.** Enums whose ordinals are serialized over the LinqService wire (`QueryElementType`, etc.) get new members appended at the end on both sides. When master and a feature branch both append, order the resolution as *master's members first, then the branch's* — never interleave — so master's already-shipped ordinals don't shift. (Surfaced syncing PR #5468: master's `SqlCteField`/`SqlCteTableField` placed before the PR's `SqlKeepClause`.)
