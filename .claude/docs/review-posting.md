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
    # /verify-review populates replyComments with partial-fix follow-ups
    # scoped to existing review threads. /review-pr leaves it empty.
    replyComments = @(
        # @{
        #     inReplyTo = 'PRRC_kwDO...'   # GraphQL node ID of the existing review comment
        #     body      = @'
        # — ✓ Partial fix in <sha>. Residual concern: ...
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
| Partial-fix reply follow-ups (verify only) | `replyComments[]` | `inReplyTo` = the GraphQL node ID of the existing review comment (not its integer REST id). Wrapper attaches each via `addPullRequestReviewComment` scoped to the new pending review, so replies stay hidden until the user submits the draft. |

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

### Heredoc escaping caveat

PowerShell single-quoted here-strings (`@'…'@`) end on the **first line** whose only content is `'@` (with the `'@` at column 0). If a comment body ever needs to contain that literal sequence, use a double-quoted here-string (`@"…"@`) instead — but then escape any literal `$` with a backtick (`` `$ ``) and double-quote marks with `` `" ``. Single-quoted here-strings are almost always the right choice because markdown content is nearly always safe inside them.

### Don'ts

- **Do not submit** the review. Omit `event` in the manifest; the wrapper already does this, so the review is created PENDING and the human user submits it from the GitHub UI after previewing the draft.
- Do not post individual comments with `POST /pulls/<n>/comments` — always go through the wrapper so all findings land inside one draft.
- Do not continue to posting if the user hasn't explicitly approved.
- Do not write one `.md` file per comment and chain them via `bodyFile` refs. Use inline here-strings in the manifest.
