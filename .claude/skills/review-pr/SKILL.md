---
name: review-pr
description: Deep professional review of a linq2db PR. Accepts PR link, PR number, a linked issue/task number, or a branch name. Loads PR + comments + linked issues, prepares a change summary, spawns code-reviewer and baselines-reviewer subagents in parallel, classifies public-API changes against the PR milestone, assembles a severity-ordered finding list, and posts a draft pending review on GitHub after user confirmation. Never commits or edits code.
---

# review-pr

User-triggered workflow to review a PR on `linq2db/linq2db`.

Shared reference material:

- **Review conventions** (severities, IDs, checkboxes, body structure): `.claude/docs/review-conventions.md`
- **GitHub review API** (endpoints, gotchas, thread-id mapping): `.claude/docs/github-review-api.md`
- **PR context prep** (parallel reads, change summary, baselines clone, temp dir): `.claude/docs/pr-context-prep.md`
- **Baselines repo layout** (branch naming, file grammar): `.claude/docs/baselines-repo-layout.md`
- **PR reference resolver** (URL / number / issue / branch): `.claude/docs/pr-resolver.md`
- **API surface classification** (milestone-driven note-vs-BLK rules): `.claude/docs/api-surface-classification.md`

## When to run

Only when the user explicitly invokes `/review-pr <ref>`. Reference forms and resolver are defined in `.claude/docs/pr-resolver.md`. Draft PRs are reviewed the same way as ready-for-review PRs.

## Steps

### 1. Resolve the target PR

Follow `.claude/docs/pr-resolver.md`. If the resolver returns "no PR for this branch", stop and propose creating one (per `.claude/docs/agent-rules.md` → Pull request rules). Do not review a branch with no PR.

### 2. Target-branch check

If `baseRefName` is not `master`, warn the user:

> This PR targets `<base>`, not `master`. Review anyway? [y/N]

Wait for an explicit `y`. No other guards (no draft-PR guard, no size guard).

### 3. Load context, summarize, prepare baselines

Execute the three sections of `.claude/docs/pr-context-prep.md` in order: **Context load** (parallel reads), **Change summary**, **Baselines clone setup**. The temp-payload directory (`.build/.claude/`) will be used in step 9 — ensure it via `mkdir -p .build/.claude` now.

### 4. Compute the ID-continuation floor

Per `.claude/docs/review-conventions.md` → **ID-continuation floor**: scan all prior reviews and comments authored by the current GitHub user (`gh api /user --jq .login`), regex-match IDs, compute `max(NNN) + 1` per severity. If none, floor is `1` for every severity. Both subagents and the final assembly need it.

### 5. Spawn the two subagents in parallel

Launch `code-reviewer` and `baselines-reviewer` in a **single assistant turn with two Agent tool calls** so they run concurrently.

**Briefing for `code-reviewer`:**

- `mode: initial`
- PR metadata (from step 1), linked issues + comments (from step 3), prior reviews/comments (from step 3).
- Change summary (step 3).
- The diff commands to run (the `git diff` / `git log` commands from pr-context-prep — subagent re-runs them; don't paste the diff).
- ID-continuation floor per severity (from step 4).

**Briefing for `baselines-reviewer`:**

- PR number and head branch.
- Baselines clone path: `../linq2db.baselines`.
- Baselines branch: `baselines/pr_<n>`, or the signal that it doesn't exist.
- Change summary (step 3).
- `mode: initial`.

### 6. Classify public-API surface changes

Apply the decision tree in `.claude/docs/api-surface-classification.md` to the `api_changes` returned by `code-reviewer`, using the PR's milestone title from step 1 and the file list from step 3. Produces notes, potentially BLK findings.

### 7. Assemble the review body

Use the body structure defined in `.claude/docs/review-conventions.md` → **Output body structure**. No legend table — reviewers who need abbreviation meanings consult the conventions doc.

Classify each `code-reviewer` finding into one of three review output locations:

| Finding has | Posted as | Shape |
|---|---|---|
| `file` **and** `line` | Line review comment in the review's `comments[]` | `{path, line, side: "RIGHT", start_line?, body}` |
| `file` but no `line` | File-level review comment in `comments[]` | `{path, subject_type: "file", body}` |
| Neither | Body-section entry under the severity heading | checkbox `[ ]`, `**<ID>** — <title>`, `Why: …`, `Fix: …` |

For line/file comments, build the `body` field as plain markdown with the shape below. (Shown as an indented block so the inner suggestion fence renders correctly in this doc — the actual `body` string contains the literal backticks.)

    **<ID>** — <why>

    Fix: <fix>

    ```suggestion
    <replacement code — only when the finding has a concrete `suggestion` value>
    ```

Append the suggestion fence only when `suggestion` is set. GitHub requires the fenced block body to be the exact replacement for the commented-on line range, preserving indentation.

### 8. Confirm with user, then post

Show the user:

- The assembled review body
- Summary counts: N per-line comments, M file-level comments, K body-section findings by severity, baselines status

Wait for an explicit "post" / "yes".

On approval, post the pending review:

1. Use the `Write` tool to save the payload to `.build/.claude/review-pr-<n>.json`:
   ```json
   {
     "commit_id": "<head_sha>",
     "body": "<assembled body>",
     "comments": [ /* ... */ ]
   }
   ```
2. Run (single Bash call):
   ```
   gh api --method POST /repos/linq2db/linq2db/pulls/<n>/reviews --input .build/.claude/review-pr-<n>.json
   ```

**Do not pass `-f event=...`.** Per `.claude/docs/github-review-api.md`, omitting `event` is what leaves the review as PENDING. Any value other than `APPROVE` / `REQUEST_CHANGES` / `COMMENT` is rejected or silently coerced, submitting the review.

### 9. Report

Capture `id` and `html_url` from the API response. Report both to the user:

- Posted draft review #`<id>`: `<html_url>`
- Counts of comments posted
- Reminder that the draft needs to be submitted manually on GitHub

`/verify-review` reuses `id` later to PUT body edits.

## Don'ts

- **Do not submit** the review. Omit `event` — this is what creates a PENDING draft.
- Do not edit any source file.
- Do not post individual comments with `POST /pulls/<n>/comments` — always go through the reviews endpoint so all findings land inside one draft.
- Do not continue to posting if the user hasn't explicitly approved.
- Do not flag the repo's column-aligned formatting — see `.claude/docs/agent-rules.md` → Code Conventions.
- Do not embed a severity legend in the review body; the conventions doc is the single source of truth.
