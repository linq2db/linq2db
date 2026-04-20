## PR reference resolution

Shared resolver for `/review-pr` and `/verify-review`. Takes a single user-supplied reference and returns PR metadata.

### Accepted reference forms

- A PR URL: `https://github.com/linq2db/linq2db/pull/1234`
- A PR number: `1234`, `#1234`, `PR 1234`
- An issue / task number whose resolution is a PR (linked via "Closes #", "Fixes #", or cross-referenced)
- A local or remote branch name

### Resolution algorithm

Try in order, stop at the first match.

1. **PR number or URL.** Extract the number.
   ```
   gh pr view <n> --repo linq2db/linq2db --json number,title,body,baseRefName,headRefName,milestone,labels,state,isDraft,commits,url,mergeable
   ```

2. **Issue / task number.** Find PRs that reference it.
   ```
   gh api graphql -f query='
   query($n:Int!) {
     repository(owner:"linq2db", name:"linq2db") {
       issue(number:$n) {
         timelineItems(first:30, itemTypes:[CROSS_REFERENCED_EVENT, CONNECTED_EVENT]) {
           nodes {
             ... on CrossReferencedEvent { source { ... on PullRequest { number state merged } } }
             ... on ConnectedEvent       { subject { ... on PullRequest { number state merged } } }
           }
         }
       }
     }
   }' -F n=<issue-number>
   ```
   Pick the most recent `OPEN` PR. If none are open, pick the most recent merged PR. If multiple open PRs match, ask the user to pick.

3. **Branch name.**
   ```
   gh pr list --repo linq2db/linq2db --head <branch> --state all --json number,title,state,isDraft
   ```
   If one PR exists, use it. If multiple exist, prefer the `OPEN` one; otherwise ask the user to pick.

4. **Branch identified but has no PR.** Stop. Do not review. Propose creating a PR per `agent-rules.md` → **Pull request rules** (draft, assignee, milestone, linked issues, etc.) and wait for user confirmation.

### Return shape

The caller expects a structured object with the following shape:

```json
{
  "number":      1234,
  "title":       "…",
  "body":        "…",
  "baseRefName": "master",
  "headRefName": "feature/my-branch",
  "milestone":   { "title": "6.3.0", "number": 42 },
  "labels":      ["bug", "provider/sqlserver"],
  "state":       "OPEN",
  "isDraft":     false,
  "url":         "https://github.com/linq2db/linq2db/pull/1234"
}
```

`milestone` may be `null`. `labels` may be an empty array. `state` is one of `"OPEN"`, `"CLOSED"`, `"MERGED"`.

### Notes

- Draft PRs are reviewed the same way as ready-for-review PRs. No special handling.
- The resolver does **not** filter out closed/merged PRs — the caller decides whether to proceed.
- When more than one candidate matches, always route the choice to the user. Never pick silently.
