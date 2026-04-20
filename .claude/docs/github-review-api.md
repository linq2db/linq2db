## GitHub PR review API — cheat sheet for review skills

Minimal, opinionated reference for what `/review-pr` and `/verify-review` need from the GitHub API. Links to the canonical docs when in doubt:

- Reviews: <https://docs.github.com/en/rest/pulls/reviews>
- Review comments: <https://docs.github.com/en/rest/pulls/comments>
- GraphQL schema: <https://docs.github.com/en/graphql/reference/mutations>

### Posting a review via the wrapper

The normal posting path is the `post-pr-review.ps1` wrapper at `.claude/scripts/post-pr-review.ps1`. One Bash call, one permission rule, handles the REST POST **plus** every file-level thread attach **plus** every thread-reply follow-up, all in a single process.

**Preferred: `-ManifestScript` (one pwsh file, here-string bodies).** The caller writes one `.build/.claude/pr<n>-manifest.ps1` that returns a hashtable; every comment body is an inline here-string (`@'…'@`), no JSON escaping. The wrapper dot-sources the file, converts to JSON internally, and posts. This replaces the older "one `.md` file per comment + `bodyFile` refs" pattern, which cost one user confirmation per comment — ~19 for a 17-comment review, now reduced to 2 (one for the manifest, one for the Bash call).

```
pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1 -ManifestScript .build/.claude/pr<n>-manifest.ps1
```

Manifest-script shape (all fields optional except `pr`, `commitId`, `body`):

````powershell
@{
    pr       = <n>
    commitId = '<head sha>'
    verify   = $true
    body     = @'
<assembled review body>
'@
    lineComments = @(
        @{ path = 'Source/...cs'; line = 42; side = 'RIGHT'; body = @'…'@ }
        @{ path = 'Source/...cs'; line = 44; startLine = 42; side = 'RIGHT'; startSide = 'RIGHT'; body = @'…'@ }
    )
    fileComments = @(
        @{ path = 'Source/...cs'; body = @'…'@ }
    )
    replyComments = @(
        # inReplyTo = GraphQL node ID of the existing review comment. REST
        # returns it as `node_id` on each /pulls/<n>/comments entry. Replies
        # are scoped to the new pending review (via pullRequestReviewId) so
        # they stay hidden until the user submits the draft.
        @{ inReplyTo = 'PRRC_kwDO...'; body = @'…'@ }
    )
}
````

**Legacy: JSON on stdin.** Still accepted for shell heredocs / external callers:

```
pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1 <<'EOF'
{
  "pr":       <n>,
  "commitId": "<head sha>",
  "body":     "<assembled review body>",
  "lineComments":  [ { "path": "Source/...cs", "line": 42, "side": "RIGHT", "body": "..." } ],
  "fileComments":  [ { "path": "Source/...cs", "body": "..." } ],
  "replyComments": [ { "inReplyTo": "PRRC_kwDO...", "body": "..." } ]
}
EOF
```

The wrapper outputs a single JSON object to stdout:

```
{
  "reviewId": 123,
  "nodeId":   "PRR_xxx",
  "url":      "https://github.com/...",
  "lineComments":  [ { "path": "...", "line": 42, "ok": true } ],
  "fileThreads":   [ { "path": "...", "ok": true, "threadId": "T_xxx", "databaseId": 456 } ],
  "replyComments": [ { "inReplyTo": "PRRC_xxx", "ok": true, "commentId": "PRRC_yyy", "databaseId": 789 } ]
}
```

Capture `reviewId` (the REST `review_id` — `/verify-review` needs it to `PUT` the body later) and `nodeId` (GraphQL node ID). Exit `0` = clean, `2` = review created but one or more file threads / reply comments failed (retry just those), `1` = hard error (nothing to retry — stdin bad, review POST rejected, etc.).

For bodies long enough that a here-string in the manifest is awkward (e.g. embedded content the agent just wants to drop from disk), pass `bodyFile = '<path>'` instead of `body` on any entry; the wrapper reads the file during post.

### Create a pending (draft) review manually (fallback path)

**Do not** pass `event`. The REST `event` parameter accepts only `APPROVE`, `REQUEST_CHANGES`, `COMMENT`. Omitting it is what leaves the review in PENDING state. Passing `"PENDING"` does not work — it's rejected or silently dropped and the review is submitted as `COMMENT`.

If the wrapper is unavailable (e.g. debugging), write the payload to a temp file and POST manually:

```
# payload.json
{
  "commit_id": "<head_sha>",
  "body": "<assembled body>",
  "comments": [ { "path": "...", "line": 42, "side": "RIGHT", "body": "..." }, ... ]
}
```

```
gh api --method POST repos/linq2db/linq2db/pulls/<n>/reviews --input payload.json
```

The response JSON contains `id` (the `review_id`), `node_id` (GraphQL), and `html_url`. File-level comments still need the separate GraphQL attach — see **File-level comments** below.

### Submit a pending review (user-initiated, not by the skill)

The skill never does this. If it ever needed to, the endpoint is:

```
POST /repos/{o}/{r}/pulls/{n}/reviews/{review_id}/events
body: { "event": "APPROVE" | "REQUEST_CHANGES" | "COMMENT", "body": "optional" }
```

### Edit a review body (after submission)

```
PUT /repos/{o}/{r}/pulls/{n}/reviews/{review_id}
body: { "body": "<new body>" }
```

Used by `/verify-review` to flip `[ ]` → `[x]` on body-section findings.

### Edit a review comment body

```
PATCH /repos/{o}/{r}/pulls/comments/{comment_id}
body: { "body": "<new body>" }
```

Used by `/verify-review` to append `— ✓ Fixed in <sha>` to prior line/file comments.

### Reply to a review-comment thread

```
POST /repos/{o}/{r}/pulls/{n}/comments/{comment_id}/replies
body: { "body": "<reply>" }
```

Creates a new review comment in the same thread, parented to `{comment_id}`. Used by `/verify-review` for partial-fix follow-ups.

### File-level comments — attach via GraphQL after review creation

The bulk `POST /pulls/<n>/reviews` does **not** support file-level comments. Its `DraftPullRequestReviewComment` schema lacks `subject_type`; passing it returns `422`. Confirmed by GitHub staff in community discussion <https://github.com/orgs/community/discussions/143197>.

The per-comment REST endpoint `POST /pulls/<n>/comments` accepts `subject_type: "file"` but always creates a **new** pending review to hold the comment — it returns `422 user_id can only have one pending review per pull request` when the current user already has a pending draft. So it cannot attach to an existing pending review either.

**Correct approach:** create the pending review first via the bulk REST POST (line-level comments + body only), then attach each file-level thread to the pending review via the GraphQL `addPullRequestReviewThread` mutation:

```
gh api graphql -f query='
mutation($rid:ID!, $path:String!, $body:String!) {
  addPullRequestReviewThread(input:{
    pullRequestReviewId: $rid,
    subjectType: FILE,
    path: $path,
    body: $body
  }) {
    thread { id comments(first:1) { nodes { databaseId } } }
  }
}' -F rid=<review_node_id> -F path=<file path> -F body=<comment body>
```

- `pullRequestReviewId` takes the GraphQL **node ID** of the pending review — the `node_id` field from the REST response of `POST /pulls/<n>/reviews`, not the numeric `id`.
- `subjectType` accepts `LINE` or `FILE`. For file-level findings always pass `FILE`.
- `path` and `body` are strings. `line` / `side` / `startLine` / `startSide` are omitted for `FILE`.
- Input fields verified via introspection on 2026-04-19: `clientMutationId, path, body, pullRequestId, pullRequestReviewId, line, side, startLine, startSide, subjectType`.

Returns `thread.id` and the comment's `databaseId`, both useful for later edits or resolves. The file-level comment becomes part of the pending review and is submitted together with the rest when the reviewer clicks "Finish your review" on GitHub.

### List prior reviews and comments

```
gh api repos/linq2db/linq2db/pulls/<n>/reviews --paginate
gh api repos/linq2db/linq2db/pulls/<n>/comments --paginate
gh api repos/linq2db/linq2db/issues/<n>/comments --paginate
```

Each endpoint returns arrays across all pages when `--paginate` is used.

### Thread-ID ← comment-databaseId mapping

`/review-pr` and `/verify-review` should read this map from `reviewThreads[]` returned by `.claude/scripts/pr-context.ps1` — that script already runs the GraphQL query below in parallel with its other jobs. Issue the raw query only if you need it outside the PR-context flow.

Resolving a review thread requires GraphQL, which uses **node IDs**, not REST comment IDs. To resolve a thread given a REST `comment_id`:

```
gh api graphql -f query='
query($pr:Int!) {
  repository(owner:"linq2db", name:"linq2db") {
    pullRequest(number:$pr) {
      reviewThreads(first:100) {
        nodes {
          id
          isResolved
          comments(first:1) { nodes { databaseId } }
        }
      }
    }
  }
}' -F pr=<n>
```

Build a map `databaseId → thread.id` from `reviewThreads.nodes[*].comments.nodes[0].databaseId`. The REST `comment_id` you have (from listing review comments) equals that `databaseId`. Look up the thread node `id` and pass it to the resolve mutation.

### Resolve a review thread

```
gh api graphql -f query='
mutation($tid:ID!) {
  resolveReviewThread(input:{threadId:$tid}) {
    thread { isResolved }
  }
}' -F tid=<thread_id>
```

Only call this when the user explicitly approved resolving the thread (see `/verify-review` step 7 — batched per-thread confirmation).

### Bash-rule note

Every API call above should be a **single** `gh api` invocation. Do not chain with `&&`, `;`, or inline `for` loops. When you need to run several calls, use multiple parallel Bash tool calls in the same assistant turn. When you need to construct a structured payload, write a temp file with the `Write` tool first, then one Bash call that passes `--input <file>`.

### Line comments: `line` is silently translated to `position`

When you POST a review with `comments[].line` + `comments[].side`, GitHub converts `line` into a diff **`position`** (the 1-indexed offset into the unified diff, counting every context / `+` / `-` / `@@` line) and stores only `position`. The response returns `line: null` for every such comment.

Consequences:

- **A wrong line that happens to be inside a hunk is not rejected.** GitHub will attach the comment at the diff position that corresponds to the (wrong) right-side line, not at the code the reviewer actually meant to comment on. There is no feedback signal from the API.
- **A line outside every hunk is rejected** with `422: Line could not be resolved`.
- To produce a correct line comment, the caller must verify — against the PR head file and against hunk boundaries — that the `line` matches the code being discussed **before** submitting. See `.claude/agents/code-reviewer.md` → **Line-number verification** (the subagent runs `verify-lines.ps1` on every finding before emitting it).

### Git Bash on Windows: drop the leading `/`

When the shell is Git Bash (MSYS / MINGW) on Windows, `gh api /repos/...` is path-mangled into a Windows filesystem path by MSYS and rejected by `gh` with:

```
invalid API endpoint: "C:/Program Files/Git/repos/...". Your shell might be rewriting URL paths as filesystem paths.
```

**Always write `gh api` endpoints without a leading slash** — `gh api repos/linq2db/linq2db/pulls/<n>/reviews`, not `gh api /repos/...`. This works on every platform; the leading slash only ever helps on POSIX shells and breaks Git Bash. The same rule applies to `gh api user --jq .login` (not `gh api /user`).

GraphQL calls (`gh api graphql`) are unaffected because the endpoint is a literal `graphql` without a leading slash to begin with.
