## PR context preparation

Common preparation done by `/review-pr` and `/verify-review` before spawning subagents. Both skills reference this doc instead of restating the steps.

### Context load (parallel)

Run all of the following in a single assistant turn as parallel Bash tool calls. Replace `<n>` with the PR number and `<headRefName>` with the head branch name.

- `gh api /repos/linq2db/linq2db/pulls/<n>/comments --paginate` (line comments from all prior reviews)
- `gh api /repos/linq2db/linq2db/issues/<n>/comments --paginate` (conversation comments)
- `gh api /repos/linq2db/linq2db/pulls/<n>/reviews --paginate` (prior reviews — bodies and commit SHAs)
- `gh api graphql -f query='query($n:Int!){ repository(owner:"linq2db",name:"linq2db"){ pullRequest(number:$n){ closingIssuesReferences(first:20){ nodes{ number } } } } }' -F n=<n>` (linked closing issues)
- `git fetch origin <headRefName>:refs/remotes/origin/<headRefName>` (make sure the head ref is local)
- `git diff --stat origin/master...origin/<headRefName>` (shortstat)
- `git diff --name-status origin/master...origin/<headRefName>` (file list)
- `git log --format='%h %s' origin/master..origin/<headRefName>` (commit list)

### Expand the related-issue set

After the initial batch, build the set of issues/PRs to fetch as "related". **Do not recurse** — go one level deep only (issues referenced *from the PR*, not issues referenced from those issues).

Sources to scan, with a single unified regex: the PR body, all commit messages (`git log --format='%B' origin/master..origin/<headRefName>` in a single Bash call alongside the earlier `git log`), every conversation comment body, every review body, and every review comment body.

Extraction patterns (combine into one **case-insensitive** regex pass):

- `#\d+` bare reference
- `https?://github\.com/linq2db/linq2db/(?:issues|pull)/\d+` full URL
- `linq2db/linq2db#\d+` cross-repo shorthand
- `(?:Close[sd]?|Fix(?:e[sd])?|Resolve[sd]?)(?:\s*:|\s+)+#\d+` closing keywords in free text

The closing-keywords pattern is **partially redundant** with the `closingIssuesReferences` GraphQL call — that call already walks the PR body for keywords. The free-text scan earns its keep on commit messages, conversation comments, and review comments, which `closingIssuesReferences` does not cover.

Union the extracted numbers with the `closingIssuesReferences` result from the initial GraphQL call, deduplicate, and filter out the PR's own number. For each remaining number, fetch in a parallel batch:

- `gh api /repos/linq2db/linq2db/issues/<issue>`
- `gh api /repos/linq2db/linq2db/issues/<issue>/comments --paginate`

Numbers that resolve to PRs (not issues) are fine — the `/issues/` endpoint serves both and returns the PR body. Don't fetch their commits, diffs, or review threads — that would be a second level, out of scope.

### Change summary

From the PR body, commit messages, file list, and linked-issue bodies, write 3–8 bullets covering:

- **Purpose** — what the PR is for, what it fixes.
- **Areas touched** — which `Source/*` projects, which providers, which tests.
- **Observable impact** — public API changes, generated SQL changes, behavior changes.
- **Anything unusual** the reviewer should know up front.

This summary is the briefing fed to both subagents so the baselines-reviewer can rationalize why each baseline moved.

### Baselines clone setup

The baselines clone is expected at **`../linq2db.baselines`** (sibling of this repo).

1. If the directory **exists**: `git -C ../linq2db.baselines fetch origin` (single Bash call).
2. If it **does not exist**: stop and ask the user:
   > Baselines clone not found at `../linq2db.baselines`. Clone `https://github.com/linq2db/linq2db.baselines.git` there now? [y/N]

   - On `y`: `git clone https://github.com/linq2db/linq2db.baselines.git ../linq2db.baselines`.
   - On `N`: skip baseline review, proceed with code review only, note the skip in the final review body.
3. After fetching, check whether the PR's baseline branch exists locally:
   ```
   git -C ../linq2db.baselines rev-parse --verify refs/remotes/origin/baselines/pr_<n>
   ```
   Non-zero exit ⇒ the PR has no baseline changes.

Layout and branch-naming conventions for the baselines repo are in `.claude/docs/baselines-repo-layout.md`.
