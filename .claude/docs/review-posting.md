## Posting a PR review via the wrapper

Shared posting mechanics for `/review-pr` and `/verify-review`. Every review — initial or follow-up — goes through `post-pr-review.ps1` via one `-ManifestScript <path>` Bash call. The skill's step that references this doc supplies the per-review body template; everything else on this page is constant.

### Manifest format — one pwsh file, here-string bodies

Write exactly **one** file to disk: `.build/.claude/pr<n>-manifest.ps1` (for `/review-pr`) or `.build/.claude/pr<n>-verify-manifest.ps1` (for `/verify-review`). The file is a PowerShell script that returns a hashtable; every comment body lives inline as a single-quoted here-string (`@'…'@`), so no JSON escaping, no triple-backtick pain, no per-comment `.md` files. One `Write` tool call + one `Bash` tool call, regardless of how many findings the review carries. Do **not** write one `.md` file per comment and chain them via `bodyFile` refs — that pattern cost one user confirmation per comment and has been retired.

Template (outer fence is 4 backticks so the inner 3-backtick `suggestion` fence inside a body here-string renders):

````powershell
# .build/.claude/pr<n>-manifest.ps1
@{
    pr       = <n>
    commitId = '<head SHA>'
    verify   = $true
    body     = @'
<assembled review body — see the calling skill's body template>
'@
    lineComments = @(
        @{
            path = 'Source/LinqToDB/...cs'
            line = 27
            side = 'RIGHT'
            body = @'
**Blocker · BLK001** — <why>

Fix: <fix>

```suggestion
<replacement>
```
'@
        }
        @{
            path = 'Source/LinqToDB/...cs'
            line = 44
            startLine = 42
            side = 'RIGHT'
            startSide = 'RIGHT'
            body = @'
**Nit · NIT004** — <why>

Fix: <fix>
'@
        }
        # ... one hashtable per line comment
    )
    fileComments = @(
        @{
            path = 'CLAUDE.md'
            body = @'
**Minor · MIN005** — <why>

Fix: <fix>
'@
        }
    )
    # replyComments are posted as threaded replies scoped to an existing review
    # comment. Two uses:
    #   • /verify-review — partial-fix follow-ups (✓ Partial fix in <sha>. Residual …)
    #   • retractions / corrections of previously-posted findings (any skill). See
    #     the "Retracting a posted finding" section below for the rule.
    # Usually empty on initial /review-pr runs.
    replyComments = @(
        # @{
        #     inReplyTo = 'PRRC_kwDO...'   # GraphQL node ID of the existing review comment
        #     body      = @'
        # **Retraction:** This finding is out of scope for this PR — <one-sentence reason>.
        # '@
        # }
    )
}
````

Invoke in one Bash call:

```
pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1 -ManifestScript .build/.claude/pr<n>-manifest.ps1
```

### Manifest-to-finding mapping

| Input                           | Manifest field | Notes |
|---------------------------------|----------------|-------|
| Assembled review body           | `body`         | One here-string; includes the agentic-review disclaimer, review notes, body-section findings (by severity), and baselines section. |
| Line-level findings             | `lineComments[]` | One hashtable per finding with `path`, `line`, `side = 'RIGHT'`, optional `startLine` + `startSide` for multi-line ranges, and `body` as a here-string. |
| File-level findings             | `fileComments[]` | `path` + `body`. Wrapper attaches each as a thread via GraphQL `addPullRequestReviewThread` after the REST POST. No separate `gh api graphql` Bash calls from the skill. |
| Thread replies (verify follow-ups + retractions) | `replyComments[]` | `inReplyTo` = the GraphQL node ID of the existing review comment (not its integer REST id). Wrapper attaches each via `addPullRequestReviewComment` scoped to the new pending review, so replies stay hidden until the user submits the draft. Used by `/verify-review` for partial-fix follow-ups and by any skill for retracting a previously-posted finding (see **Retracting a posted finding** below). |

Set `verify = $true` on every run. After posting, the wrapper re-fetches the stored review body and each line comment from GitHub and byte-compares them to what was sent. Mismatches surface in the output's `verify` block and trigger exit code 2. Cheap insurance against any future stdio-encoding regression silently corrupting non-ASCII comment content.

The wrapper already omits `event`, so the review is created as PENDING per the API rule in [`github-review-api.md`](github-review-api.md).

### Reporting back to the user

The wrapper prints a single JSON object to stdout containing `reviewId`, `nodeId`, `url`, `lineComments[]`, `fileThreads[]`, `replyComments[]`, and an optional `verify` block. Relay to the user:

- Posted draft review #`<reviewId>`: `<url>`
- Line-comment, file-thread, and reply-comment counts (from the wrapper's per-item arrays)
- Any `fileThreads[].ok == false` or `replyComments[].ok == false` entries — those need a retry (wrapper exit code 2 signals this)
- Any `verify.body.ok == false` / `verify.lineComments[*].ok == false` entries — mismatches in what GitHub stored vs. what was sent; investigate before submitting
- Reminder that the draft needs to be **submitted manually on GitHub**

`/verify-review` captures and reuses `reviewId` later when it PUTs edits to the review body.

### Retracting a posted finding

When withdrawing a previously posted finding — your own earlier review, a finding picked up by `/verify-review`, or any comment that turned out to be wrong or out of scope — **reply on the thread**. Do not `PATCH` the original comment body, and do not `PUT` a prior review body's text with a rewrite.

**Why:** overwriting erases the public history of what was said. A reviewer who followed the thread, was notified, or linked to it elsewhere loses the context of what's being retracted; the PR author's prior responses look like they're addressing something else. A reply preserves the original wording alongside the correction, which is more honest and easier for other reviewers to follow.

**How:**

- **Line / file review comments.** Add a `replyComments[]` entry to the manifest. Use the thread's `firstCommentId` (integer REST id) resolved to its GraphQL node ID via the thread map the skill already collected from `pr-context.ps1` output (`reviewThreads[]`). The wrapper attaches each reply via `addPullRequestReviewComment` scoped to the new pending review, so replies stay hidden until the user submits the draft. Reply body leads with `**Retraction:**` or `**Correction:**`, states what was wrong and why in 1–2 sentences. Keep the prose short — the original comment's full context is already on the thread.
- **Review body (top-level).** The review body isn't a thread, so post a new review (through the normal `post-pr-review.ps1` flow) whose body explicitly references the prior review id / URL and states the retraction. Do not `PUT` the prior review body to overwrite it.
- **Exception.** Typo / broken-link / formatting-only edits that don't change meaning are fine in place — use `PATCH` for those.

**Always check review state before editing.** Before any `PUT` / `PATCH` on an existing review body or comment, fetch the review and confirm `state` + `submitted_at`. A review created by `post-pr-review.ps1` starts as `PENDING` with `submitted_at: null`, but the user may submit it from the GitHub UI at any point between the initial post and a later edit. A submitted review (`state` ∈ {`APPROVED`, `CHANGES_REQUESTED`, `COMMENTED`}, `submitted_at` populated) is public history — overwrite it and you've erased what was said. One-liner to check: `gh api repos/<o>/<r>/pulls/<n>/reviews/<id> --jq '{state, submitted_at}'`. If submitted, switch to the new-review flow above; if pending, in-place edit is fine.

**`replyComments[]` in both skills.** The manifest field is usable from `/review-pr` (for retracting findings from an earlier review while preparing a new one in the same cycle) and from `/verify-review` (for partial-fix follow-ups). It is **not** exclusive to verify-mode.

### Heredoc escaping caveat

PowerShell single-quoted here-strings (`@'…'@`) end on the **first line** whose only content is `'@` (with the `'@` at column 0). If a comment body ever needs to contain that literal sequence, use a double-quoted here-string (`@"…"@`) instead — but then escape any literal `$` with a backtick (`` `$ ``) and double-quote marks with `` `" ``. Single-quoted here-strings are almost always the right choice because markdown content is nearly always safe inside them.

### Don'ts

- **Do not submit** the review. Omit `event` in the manifest; the wrapper already does this, so the review is created PENDING and the human user submits it from the GitHub UI after previewing the draft.
- Do not post individual comments with `POST /pulls/<n>/comments` — always go through the wrapper so all findings land inside one draft.
- Do not continue to posting if the user hasn't explicitly approved.
- Do not write one `.md` file per comment and chain them via `bodyFile` refs. Use inline here-strings in the manifest.
