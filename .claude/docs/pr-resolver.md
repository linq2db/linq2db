## PR reference resolution

Shared resolver for `/review-pr` and `/verify-review`. Takes a single user-supplied reference and returns PR metadata.

### Accepted reference forms

- A PR URL: `https://github.com/linq2db/linq2db/pull/1234`
- A PR number: `1234`, `#1234`, `PR 1234`
- An issue / task number whose resolution is a PR (linked via "Closes #", "Fixes #", or cross-referenced)
- A local or remote branch name

### Resolution algorithm

Try in order, stop at the first match.

1. **PR number or URL.** Extract the number. **Do not** run `gh pr view` here — the caller's subsequent `pr-context.ps1` call (per `.claude/docs/pr-context-prep.md` → *Context load*) returns full PR metadata (`title`, `body`, `baseRefName`, `headRefName`, `milestone`, `labels`, `state`, `isDraft`, `url`, etc.) as part of its main load, so an extra `gh pr view` is redundant and just costs a permission prompt.

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

The resolver returns only what's needed to identify the PR. Full metadata is loaded downstream by `pr-context.ps1`.

- **Case 1 (PR number / URL).** No `gh` call. Resolver returns just `{ "number": 1234 }`.
- **Cases 2 and 3 (issue → PR, branch → PR).** The `gh` call used to pick the right PR already returns a few fields; the resolver forwards them so the caller can display them if needed. Minimum shape: `{ "number": 1234, "state": "OPEN", "isDraft": false }`. `state` is one of `"OPEN"`, `"CLOSED"`, `"MERGED"`.

In all cases the full metadata (title, body, baseRefName, headRefName, milestone, labels, url, commits, mergeable, …) is populated by the caller's subsequent `pr-context.ps1` invocation — the skill should rely on that output for every one of those fields rather than re-fetching via `gh`.

### Notes

- Draft PRs are reviewed the same way as ready-for-review PRs. No special handling.
- The resolver does **not** filter out closed/merged PRs — the caller decides whether to proceed.
- When more than one candidate matches, always route the choice to the user. Never pick silently.
