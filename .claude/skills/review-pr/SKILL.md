---
name: review-pr
description: Deep professional review of a linq2db PR. Accepts PR link, PR number, a linked issue/task number, or a branch name. Loads PR + comments + linked issues, prepares a change summary, spawns code-reviewer and baselines-reviewer subagents in parallel, classifies public-API changes against the PR milestone, assembles a severity-ordered finding list, and posts a draft pending review on GitHub after user confirmation. Never commits or edits code.
---

# review-pr

User-triggered workflow to review a PR on `linq2db/linq2db`.

Shared reference material:

- **Review conventions** (severities, IDs, checkboxes, body structure): `.claude/docs/review-conventions.md`
- **GitHub review API** (endpoints, gotchas, thread-id mapping): `.claude/docs/github-review-api.md`
- **PR context prep** (one-call loader, change summary, baselines clone): `.claude/docs/pr-context-prep.md`
- **Baselines repo layout** (branch naming, file grammar): `.claude/docs/baselines-repo-layout.md`
- **PR reference resolver** (URL / number / issue / branch): `.claude/docs/pr-resolver.md`
- **API surface classification** (milestone-driven note-vs-BLK rules): `.claude/docs/api-surface-classification.md`

The workflow relies on four PowerShell Core helper scripts to keep the permission surface to one allowlist entry per script. They share a common shape (stdin JSON → stdout JSON, no temp files, no compound commands) — see `.claude/docs/agent-rules.md` → **PowerShell Core scripts for complex operations** for the pattern:

- `.claude/scripts/pr-context.ps1` — fetches PR metadata, reviews, comments, linked issues, diff stat / name-status / commits, `origin/pr/<n>` head, in one call.
- `.claude/scripts/diff-reader.ps1` — batch file content + diff + hunk reader, called by `code-reviewer`.
- `.claude/scripts/verify-lines.ps1` — batch snippet + hunk verification, called by `code-reviewer`.
- `.claude/scripts/baselines-diff.ps1` — one-shot baselines diff + grammar parse, called by `baselines-reviewer`.
- `.claude/scripts/post-pr-review.ps1` — REST review POST + file-thread GraphQL in one process.

## When to run

Only when the user explicitly invokes `/review-pr <ref>`. Reference forms and resolver are defined in `.claude/docs/pr-resolver.md`. Draft PRs are reviewed the same way as ready-for-review PRs.

## Steps

### 1. Resolve the target PR

Follow `.claude/docs/pr-resolver.md`. If the resolver returns "no PR for this branch", stop and propose creating one (per `.claude/docs/agent-rules.md` → Pull request rules). Do not review a branch with no PR.

Per the resolver doc, the output of this step is the PR **number** only — no standalone `gh pr view` call. Full metadata lands in step 2.

### 2. Load context, summarize, prepare baselines

Execute the three sections of `.claude/docs/pr-context-prep.md` in order: **Context load** (one script call), **Change summary**, **Baselines clone setup**.

### 3. Target-branch check

Using `pr.baseRefName` from step 2's context output, if it is not `master`, warn the user:

> This PR targets `<base>`, not `master`. Review anyway? [y/N]

Wait for an explicit `y`. No other guards (no draft-PR guard, no size guard).

### 4. Compute the ID-continuation floor

Per `.claude/docs/review-conventions.md` → **ID-continuation floor**: using `reviews` + `reviewComments` + `currentUser` already loaded in step 2, filter to entries authored by `currentUser`, regex-match IDs across their bodies, compute `max(NNN) + 1` per severity. If none, floor is `1` for every severity. Both subagents and the final assembly need it.

### 5. Spawn the two subagents in parallel

Launch `code-reviewer` and `baselines-reviewer` in a **single assistant turn with two Agent tool calls** so they run concurrently.

**Briefing for `code-reviewer`:**

- `mode: initial`
- PR metadata, linked issues + comments, prior reviews/comments (all from step 2).
- Change summary (step 2).
- Head ref / base ref (`origin/pr/<n>`, `origin/master`) and the file list from `nameStatus`. The subagent reads content and hunks via `.claude/scripts/diff-reader.ps1`; do not paste the diff into the briefing.
- `writeDir: .build/.claude/pr<n>` — instruct the subagent to pass this on its first `diff-reader.ps1` call so full file bodies land on disk and can be navigated with `Read` / `Grep` instead of as inline JSON strings.
- ID-continuation floor per severity (from step 4).

**Briefing for `baselines-reviewer`:**

- PR number and head branch.
- Baselines clone path: `../linq2db.baselines`.
- Baselines branch: `baselines/pr_<n>` (the subagent calls `.claude/scripts/baselines-diff.ps1` which handles the "branch missing" case itself).
- Change summary (step 2).
- `mode: initial`.

### 6. Classify public-API surface changes

Apply the decision tree in `.claude/docs/api-surface-classification.md` to the `api_changes` returned by `code-reviewer`, using the PR's milestone title and file list from step 2. Produces notes, potentially BLK findings.

Compute the `suppressions_updated` flag required by step 1 of the classification doc by filtering the already-loaded `nameStatus` array in-memory — look for any entry whose `path` matches `Source/**/CompatibilitySuppressions.xml`. Do **not** re-run `git diff --name-only | grep` in a Bash call; the data is already in hand and the pipe would prompt on the allowlist.

`code-reviewer` already verifies its own line numbers (see its spec's **Line-number verification** section). Trust that output — do not re-run verification here, and in particular do not `git show origin/pr/<n>:path` to spot-check snippets. The subagent's first `diff-reader.ps1` call with `writeDir: .build/.claude/pr<n>` persisted every changed file's full HEAD body, base-ref body, and per-file diff to disk — if parent-skill reasoning ever needs to look at a file, `Read` / `Grep` it directly at the paths listed in `.claude/docs/pr-context-prep.md` → **`writeDir` directory layout**. Do **not** `ls` the directory to discover the shape; the layout is fixed and documented. Re-fetching via `git show ref:path | sed -n` pipes costs a permission prompt each and is forbidden. Post-subagent sanity is limited to: each `line` is a positive integer, `line_end >= line` when present, and `file` points to a path that actually appears in the PR's changed-file list from step 2. Findings that fail those lightweight checks go straight to body-section — no disk caching, no second pass.

### 7. Assemble the review body

Use the body structure defined in `.claude/docs/review-conventions.md` → **Output body structure**. No legend table — reviewers who need abbreviation meanings consult the conventions doc.

Classify each `code-reviewer` finding into one of three review output locations:

| Finding has | Posted as | Shape |
|---|---|---|
| `file` **and** `line` | Line review comment in the review's `comments[]` | `{path, line, side: "RIGHT", start_line?, body}` |
| `file` but no `line` | File-level thread via GraphQL `addPullRequestReviewThread`, posted **after** the REST review create (step 8.4) — **not** in `comments[]` | n/a in REST bulk POST |
| Neither | Body-section entry under the severity heading | checkbox `[ ]`, `**<ID>** — <title>`, `Why: …`, `Fix: …` |

For line/file comments, build the `body` field as plain markdown with the shape below. The leading `<Severity>` is the spelled-out name (`Blocker`, `Major`, `Minor`, `Suggestion`, `Nit`) so a human reader seeing an isolated comment on a file line decodes the ID without context. (Shown as an indented block so the inner suggestion fence renders correctly in this doc — the actual `body` string contains the literal backticks.)

    **<Severity> · <ID>** — <why>

    Fix: <fix>

    ```suggestion
    <replacement code — only when the finding has a concrete `suggestion` value>
    ```

Append the suggestion fence only when `suggestion` is set. GitHub requires the fenced block body to be the exact replacement for the commented-on line range, preserving indentation.

**Enforce the suggestion-block rule.** Per `code-reviewer.md` → output rules, every **line-level** finding whose fix is expressible as a textual replacement must carry `suggestion`. Before assembling, audit the subagent output: for each line-level finding that has a concrete `fix` but no `suggestion`, decide whether the fix is structural (OK to omit — refactors, new methods, multi-location edits) or a direct replacement (the subagent missed it). For the latter, either push back to the subagent for the suggestion or synthesize one yourself from the `fix` text. Do not post a line-level finding with a replaceable fix but no suggestion block.

**Baselines section rendering.** Use the subagent's structured output to compose the `## Baselines` section with these rules:

1. **Section header.** Lead with one sentence citing the baselines review anchor:

       ## Baselines
       Delta: [linq2db.baselines PR #<baselineReview.number>](<baselineReview.url>) (<baselineReview.state>) · [compare view](<baselineCompareUrl>)

   If `baselineReview` is null, drop the PR link and keep the compare link only. If `status == "no_baselines"`, emit `No baseline changes.` and skip the rest.

2. **Per-group heading.** One `###` heading per `groups[].heading`, optionally followed by the group's `summary`.

3. **Per-subgroup rendering.** One `-` bullet per subgroup, prefixed with `**<reason>** — <subgroup.summary>`. Then render its entries as a nested list:

   | entry `sampleStatus` | Rendering |
   |---|---|
   | `A` (added)    | `- [<test>](<sampleUrl>) — added (<providerCount> providers: <comma list>)` |
   | `M` (modified) | `- [<test>](<sampleUrl>) — modified (<providerCount> providers: …)` followed by a collapsed `<details><summary>sample diff</summary>` block containing the `sampleDiff` inside a ```diff fence. |
   | `D` (deleted)  | `- <test> — deleted (<providerCount> providers: …)` (plain text, no link) |

   Provider lists longer than ~8 items get compacted to `<first 5>, … (N providers total)`. Entry `note` fields go after the parenthetical on the same line.

4. **Cross-provider anomalies** under `### Cross-provider anomalies`, one bullet per entry.

5. **Compression feedback.** Do NOT render `compressionFeedback[]` in the review body — that surfaces separately in step 8 as proposed follow-up improvements.

Entries with empty `sampleUrl` / `samplePath` (rollup entries not tied to a specific pattern) render as plain `- <test> — <providers…>` with no link and no diff block.

### 8. Confirm with user, then post

Show the user:

- The assembled review body
- Summary counts: N per-line comments, M file-level comments, K body-section findings by severity, baselines status
- Any `compressionFeedback[]` entries from `baselines-reviewer` — present these as **"Proposed follow-up improvements to `baselines-diff.ps1`'s normaliser"**, one short bullet per entry. These are not part of the review itself; the point is to let the user decide whether to act on them in a separate change after the review is posted.

Wait for an explicit "post" / "yes".

On approval, post the pending review via the **`post-pr-review.ps1` wrapper** — see `.claude/docs/github-review-api.md` → **Posting a review via the wrapper** for full schema. One Bash call, one permission rule, regardless of how many findings:

```
pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1 < .build/.claude/review-pr-<n>.manifest.json
```

Or feed the manifest inline via heredoc to avoid the scratch file entirely:

```
pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1 <<'EOF'
{ "pr": <n>, "commitId": "<sha>", "body": "…", "lineComments": [...], "fileComments": [...] }
EOF
```

Manifest-to-finding mapping:

- Body-section findings → assembled review body → `body` field (or `bodyFile` if the body is long enough that you've written it to disk already).
- Line-level findings → `lineComments[]` with `path`, `line`, optional `startLine`, and `body`.
- File-level findings → `fileComments[]` with `path` and `body`. The wrapper attaches each as a thread via GraphQL after the REST POST. **No** separate `gh api graphql` Bash calls from the skill.
- `verify: true` — recommended. After posting, the wrapper re-fetches the review body and each line comment from GitHub and byte-compares them to the sent payload. Mismatches surface in the output's `verify` block and trigger exit code 2. Cheap insurance against any future stdio-encoding regression silently corrupting non-ASCII comment content.

The wrapper already omits `event`, so the review is created as PENDING per the API rule in `.claude/docs/github-review-api.md`.

When the review body is long enough that embedding it inline would make the manifest unreadable, use `Write` to drop it to `.build/.claude/review-pr-<n>.md` and reference it via `"bodyFile": ".build/.claude/review-pr-<n>.md"` in the manifest. Same pattern for per-comment bodies if their markdown has enough backticks/fences to fight with JSON quoting.

### 9. Report

The wrapper prints a single JSON object to stdout containing `reviewId`, `nodeId`, `url`, and per-finding status. Relay to the user:

- Posted draft review #`<reviewId>`: `<url>`
- Line comments, file-level threads (from the wrapper's counts), and body-section findings
- Any `fileThreads[].ok == false` entries — those need a retry (wrapper exit code 2 signals this)
- Reminder that the draft needs to be submitted manually on GitHub

`/verify-review` reuses `reviewId` later to PUT body edits.

## Don'ts

- **Do not submit** the review. Omit `event` — this is what creates a PENDING draft.
- Do not edit any source file.
- Do not post individual comments with `POST /pulls/<n>/comments` — always go through the reviews endpoint so all findings land inside one draft.
- Do not continue to posting if the user hasn't explicitly approved.
- Do not flag the repo's column-aligned formatting — see `.claude/docs/agent-rules.md` → Code Conventions.
- Do not embed a severity legend in the review body; the conventions doc is the single source of truth.
