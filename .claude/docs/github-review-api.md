## GitHub PR review API — cheat sheet for review skills

Minimal, opinionated reference for what `/review-pr` and `/verify-review` need from the GitHub API. Links to the canonical docs when in doubt:

- Reviews: <https://docs.github.com/en/rest/pulls/reviews>
- Review comments: <https://docs.github.com/en/rest/pulls/comments>
- GraphQL schema: <https://docs.github.com/en/graphql/reference/mutations>

### Create a pending (draft) review

**Do not** pass `event`. The REST `event` parameter accepts only `APPROVE`, `REQUEST_CHANGES`, `COMMENT`. Omitting it is what leaves the review in PENDING state. Passing `"PENDING"` does not work — it's rejected or silently dropped and the review is submitted as `COMMENT`.

Pattern — write the payload to a temp file, then one Bash call:

```
# payload.json
{
  "commit_id": "<head_sha>",
  "body": "<assembled body>",
  "comments": [ { "path": "...", "line": 42, "side": "RIGHT", "body": "..." }, ... ]
}
```

```
gh api --method POST /repos/linq2db/linq2db/pulls/<n>/reviews --input payload.json
```

The response JSON contains `id` (the `review_id`) and `html_url`. Capture `id` — `/verify-review` needs it to `PUT` the body later.

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

### List prior reviews and comments

```
gh api /repos/linq2db/linq2db/pulls/<n>/reviews --paginate
gh api /repos/linq2db/linq2db/pulls/<n>/comments --paginate
gh api /repos/linq2db/linq2db/issues/<n>/comments --paginate
```

Each endpoint returns arrays across all pages when `--paginate` is used.

### Thread-ID ← comment-databaseId mapping

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
