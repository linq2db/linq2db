## PR and push procedures

Detail-heavy mechanics for creating PRs and pushing follow-ups. The summary in [`agent-rules.md`](agent-rules.md) → *Push to remote rules* / *Pull request rules* keeps the principles and one-line triggers; this doc is what you load when one of those triggers fires.

### After every successful push: PR body check

Check for a PR on the branch (`gh pr list --head <branch> --json number,title,body,url`):

- If **no PR exists**, propose creating one (see [`Creating a PR`](#creating-a-pr)) and wait for confirmation.
- If **a PR exists**, diff the newly pushed commits against the current PR body. If the body no longer accurately describes the work (new summary bullets, new linked issues, etc.), propose a concrete edit and wait for confirmation before calling `gh pr edit`. **Show the proposed change as a diff between the current body and the new one** (e.g. a unified diff or `- old line` / `+ new line` markers) — do not just paste the new body in full. If the body is still accurate, say so and move on — don't edit gratuitously.
- **When the body update follows a follow-up commit on the user's own PR, append — don't rewrite.** Add a new subsection (typically `## Follow-up commit` or similar) summarising the new commit's deltas and leave the original prose verbatim. Don't paraphrase, restructure, or "neutralise" content the human author already wrote. The "preserve, don't rewrite" rule is suspended only when the user explicitly asks for a tone or structure change to the existing body.

### After every successful push: re-request Copilot review

Copilot's automatic trigger is unreliable — it sometimes doesn't fire on follow-up pushes — so re-request after each successful push (and after the PR-body check above):

```
gh pr edit <N> --repo linq2db/linq2db --add-reviewer copilot-pull-request-reviewer
```

Two slug / endpoint gotchas:

- `gh pr edit --add-reviewer` routes through GraphQL with the bot's user-login (`copilot-pull-request-reviewer`). Passing `Copilot` to `gh pr edit --add-reviewer` errors with `Could not resolve user with login 'copilot'`.
- The REST equivalent (`gh api -X POST repos/.../requested_reviewers -f 'reviewers[]=Copilot'`) accepts the `Copilot` slug, but **silently no-ops when Copilot already reviewed an earlier commit on the same PR** — it returns 200 yet the bot is not re-queued. Always prefer `gh pr edit` for follow-up requests.

After the review lands, fix or reply per thread and resolve via the existing helpers — `gh api repos/.../pulls/<N>/comments` for inline bodies, `gh pr view <N> --json reviews,latestReviews` for the review-level overview. Bulk reply + resolve goes through `.claude/scripts/post-pr-thread-replies.ps1` (see [`github-review-api.md`](github-review-api.md) → **Batch reply + resolve**); GitHub doesn't auto-resolve threads when a follow-up commit fixes the line.

### Creating a PR

When creating a PR on `linq2db/linq2db`:

- **Always open as draft** (`gh pr create --draft`). Never publish a ready-for-review PR unless the user explicitly asks.
- **Confirm title and body with the user before running `gh pr create`.** Propose both, wait for approval, then create.
- **Link referenced issues/tasks as closed on merge.** If the work targets a known issue or task, include `Fixes #<n>` / `Closes #<n>` in the PR body so GitHub auto-closes it when the PR merges. One keyword per issue.
- **Assignee.** Assign the PR to the current GitHub user (`gh pr create --assignee @me`) unless the user specifies someone else.
- **Milestone.**
  - If the linked issue/task has a milestone, reuse it.
  - Otherwise ask the user to pick one. Fetch open milestones via `gh api repos/linq2db/linq2db/milestones?state=open` and present a **numbered list** (so the user can reply with just a number) in this order:
    1. The **next-version milestone** (matching `<Version>` in `Directory.Build.props`, or the closest upcoming version) — always first.
    2. Remaining **versioned** milestones (titles starting with a digit, e.g. `6.x`, `7.0.0`), sorted by version.
    3. **Non-versioned** milestones (e.g. `Backlog`, `In-progress`), sorted alphabetically by title.
- **CI run proposal.** After `gh pr create`, propose running the full provider matrix on Azure Pipelines via a `/azp run test-all` comment. See [`ci-tests.md`](ci-tests.md) for the trigger syntax and when a narrower `/azp run test-<dbname>` makes more sense. Wait for the user to confirm before posting the comment.

### Extending an open PR

Commits that extend an open PR's scope go on that PR's branch, not a new parallel branch. When a review session surfaces an ancillary fix (apostrophe-escape bug found while reviewing #5463, a test regression caused by the PR, a missing guardrail) and the user asks for it as a follow-up, push it onto the PR's existing head branch — don't create a sibling `feature/*` branch and propose a second PR. Mechanics:

- Check `gh pr view <n> --json maintainerCanModify,headRepository,headRefName`. If `maintainerCanModify: true` and `headRepository` is a fork, add the author's fork as a git remote if not already present (`git remote add <owner> https://github.com/<owner>/<repo>.git`) and push via refspec: `git push <owner> <local-branch>:<headRefName>`. The PR auto-updates with the new commit. Propose a body update when the new commit extends the PR's originally described scope (follow the [After every successful push: PR body check](#after-every-successful-push-pr-body-check) flow above).
- If `maintainerCanModify: false`, stop and ask — either the author has to apply the change themselves, or the work needs a separate PR. Don't unilaterally open a parallel branch when the intent was a follow-up commit.
- When pushing to someone else's fork, neutralize accidental pushes afterward if the remote is no longer needed (`git remote set-url --push <owner> no_push` as a guard, or `git remote remove <owner>` if you want it gone). Confirm with the user which — "disable" can mean either.
