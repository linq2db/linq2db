---
name: copilot-loop
description: Iterative absorb loop for inbound Copilot (and other LLM-bot) review threads on a linq2db PR the user authored. Per round: verify each open thread against current HEAD, propose fixes for still-actual ones, batch commit + push, re-request Copilot, post batched reply+resolve via post-pr-thread-replies.ps1, then auto-wait for the next bot review and loop. Each fix round is explicitly user-confirmed; no GitHub writes without per-round approval.
---

# copilot-loop

User-triggered absorb workflow for inbound Copilot review comments. Invoke as
`/copilot-loop` or via the natural-language trigger
"verify and address copilot comments" (no PR number — operates on the PR
attached to the current branch).

This skill is the **PR-author** companion to `/review-pr` and `/verify-review`
(which are PR-reviewer skills). All three share `pr-context.ps1` and
`post-pr-thread-replies.ps1`, but the role assumptions differ:

| Skill | Role | Trigger |
|---|---|---|
| `/review-pr` | reviewer | "review PR `<n>`" |
| `/verify-review` | reviewer | "verify my review on PR `<n>`" |
| `/copilot-loop` | PR author | "verify and address copilot comments" |

Shared reference material:
- **Review orchestration:** `.claude/docs/review-orchestration.md`
- **GitHub review API:** `.claude/docs/github-review-api.md`
- **PR-and-push rules:** `.claude/docs/pr-and-push.md`
- **agent-rules:** `.claude/docs/agent-rules.md`

## When to run

The user just received a Copilot review on a PR they own and wants to absorb
it. Typical session shape:

1. User pushes a commit to their PR branch.
2. Copilot's auto-review (or a manual re-request) lands.
3. User invokes `/copilot-loop`. The skill runs round 1, then **auto-waits**
   for Copilot's next review and runs round 2, and so on. Each fix round is
   user-confirmed; only the wait between rounds is automatic.
4. The loop ends when Copilot's re-review surfaces zero new threads
   (convergence) or the wait times out (user re-invokes later).

Skip when the PR has no inbound bot review (the user is reviewing-side work,
not author-side absorb work) — `/review-pr` and `/verify-review` cover that.

## Steps

### 1. Resolve the active PR

The skill operates on the PR attached to the current branch:

```
gh pr list --repo linq2db/linq2db --head <current-branch> --state open \
  --json number,title,url,headRefName
```

- If zero PRs → stop and tell the user. This skill needs an open PR.
- If multiple → ask the user to pick.

Confirm the PR with the user before continuing. Record `prNumber`, `prHeadSha`.

### 2. Sync local branch to PR HEAD

Pull the remote head so local HEAD matches the PR's current commit:

```
git fetch origin <current-branch>
git pull --ff-only origin <current-branch>
```

If the working tree has uncommitted changes that block fast-forward, stop
and ask (per `agent-rules.md` → *Creating a new branch* → dirty working tree
rule). Don't silently discard.

### 3. Load PR context, identify the latest Copilot review

```
pwsh -NoProfile -File .claude/scripts/pr-context.ps1 -Pr <n>
```

Parse the output (stdout JSON, or its persisted file under
`tool-results/`). From `reviews[]`, find the **latest** review by
`submittedAt` whose `user` matches the case-insensitive regex `copilot`
(catches both `copilot-pull-request-reviewer[bot]` the wrapper and
`Copilot` the inline-comment author — see `github-review-api.md` →
**Filtering Copilot review activity**).

If no Copilot review exists, stop and tell the user.

Record `lastReviewSubmittedAt` (used by step 11 as the polling cursor).

### 4. Filter to threads needing disposition

From `reviewThreads[]` and `reviewComments[]`:
- Keep threads where `isResolved == false`.
- Drop threads where `resolvedBy == currentUser` (we already disposed of them).
- Keep threads whose first comment is from a Copilot/bot user **OR** whose
  body still applies to current code (line-anchored at HEAD).

This is the **work list** — call its size `N`. If `N == 0`, Copilot is
satisfied: report and stop (don't proceed to wait — the loop has converged).

### 5. Per-thread verification

For each thread in the work list:

1. Read the file at the comment's line in current PR HEAD.
2. Classify per the calibration list in `/review-pr` step 2b (rules 1, 4, 5,
   6, **framework-API-removal**) against the bot claim.
3. Verdict ∈ { **Fixed at HEAD**, **Inaccurate at HEAD**, **Still actual** }.
4. For Still-actual findings, draft the smallest viable fix.

Surface a single table summarising N rows:

```
# | comment-id | file:line | verdict | proposed-fix
```

### 6. User confirmation: ask-ask-do-all

In one turn, ask the user:
1. For each Still-actual finding, which strategy (apply / revise / skip / defer)?
2. For Inaccurate-at-HEAD findings, confirm the evidence-and-resolve reply text.
3. Commit message preview.

Batch all questions per `agent-rules.md` → *Ask-ask-do-all, not ask-do-ask-do*.

### 7. Apply fixes locally

Apply confirmed fixes via `Edit` / `Write`. Run `git status` after the batch
to confirm the only modified files are the ones the fix scope justifies (per
`agent-rules.md` → *Verify subagent output*).

### 8. Commit + push

Per `agent-rules.md` → *Git commit rules*. Use the user-confirmed commit
message. `--draft` and other PR-create rules don't apply (PR exists).

After push, re-request Copilot per `agent-rules.md` → *Push to remote rules*:

```
gh pr edit <n> --repo linq2db/linq2db --add-reviewer copilot-pull-request-reviewer
```

Record the new HEAD SHA — call it `newHeadSha`. Update reply bodies in step 9
to reference it.

### 9. Build the reply manifest

For each thread in the work list, write one item to
`.build/.claude/pr<n>-thread-replies-round<r>.json`:

- **Fixed at HEAD before this round:** body = `"Fixed at HEAD: <quote of current code or short reference> — fixed in an earlier commit."`
- **Inaccurate at HEAD:** body = `"Inaccurate at HEAD. <evidence>. <verdict notes>"`
- **Still actual + just fixed:** body = `"Fixed in <newHeadSha>: <one-line description of fix>."`
- **Still actual + deferred:** body = `"Still actual; deferred for follow-up: <reason>. Not resolving."` and set `resolve: false`.

All replies get `resolve: true` except the explicit deferred case.

Manifest shape per `.claude/scripts/post-pr-thread-replies.ps1` header.

### 10. Post replies + resolve

```
pwsh -NoProfile -File .claude/scripts/post-pr-thread-replies.ps1 \
  -ManifestFile .build/.claude/pr<n>-thread-replies-round<r>.json
```

Verify the script's exit code is 0 and the summary shows `failed: 0`. If any
item failed, report which and stop — don't proceed to wait until the user
decides whether to retry the failed posts.

### 11. Auto-wait for the next Copilot review

```
pwsh -NoProfile -File .claude/scripts/wait-for-review.ps1 \
  -Pr <n> \
  -SinceSubmittedAt <lastReviewSubmittedAt-iso8601> \
  -BotLoginRegex copilot \
  -MaxWaitSec 600 \
  -PollIntervalSec 30
```

Output: `{ found: true|false, newReview: {...} | null, waitedSec, polled }`.

- **`found: true`** → loop back to **step 3** in the same invocation. The
  user is not re-engaged until step 6 of the next round.
- **`found: false`** (timeout elapsed) → stop and tell the user:
  - How many rounds completed this invocation.
  - That the loop timed out waiting for Copilot.
  - To re-invoke `/copilot-loop` once Copilot has responded.

Defaults: `MaxWaitSec=600` (10 minutes), `PollIntervalSec=30` (every 30s).
Override on invocation: `/copilot-loop --wait 30m --poll 60s`.

### 12. Report on stop

Whatever ends the loop (convergence, timeout, user `Ctrl+C`, error):

- Rounds completed this invocation.
- New commits pushed.
- Threads resolved per round.
- Outstanding deferred threads (with reasons).
- If convergence: state explicitly "Copilot satisfied — no new threads in
  last review."
- If timeout: state how long the wait was and what to do next.

## Don'ts

- Do not auto-defer findings the agent can't fix. Surface them to the user.
- Do not commit playground scratch or `.claude/` curation diffs accidentally
  carried over (per `agent-rules.md` → *Carrying `.claude/` curation*).
- Do not skip the calibration list in step 5 — bots are systematically wrong
  on `PublicAPI.Unshipped.txt` drift and framework-API-removal claims.
- Do not invent fixes for findings classified as **Inaccurate at HEAD** —
  reply with the verification evidence and resolve, don't "while we're here".
- Do not run multiple commit+push cycles within one round. One round = one
  push. If a finding's fix requires multiple commits, group them and push
  once at step 8.
- Do not bypass the auto-wait by re-running step 3 manually. The wait helper
  exists so the agent doesn't burn context polling; calling it isn't optional
  for the in-invocation loop.
