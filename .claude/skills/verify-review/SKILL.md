---
name: verify-review
description: Re-verify prior `/review-pr` findings on a linq2db PR against the current PR HEAD. Collects all prior reviews, parses findings by severity-ID, reruns code-reviewer and baselines-reviewer in `verify` mode, then applies in-place edits (checkbox flips on prior review bodies, annotations + thread resolves on prior review comments) and posts a new draft review for partial fixes and new findings. Requires explicit user confirmation before any GitHub write.
---

# verify-review

User-triggered follow-up workflow after the PR author has addressed findings from a prior `/review-pr`. Invoke as `/verify-review <ref>`.

Shared reference material:

- **Review conventions**: `.claude/docs/review-conventions.md`
- **GitHub review API**: `.claude/docs/github-review-api.md`
- **PR context prep**: `.claude/docs/pr-context-prep.md`
- **Baselines repo layout**: `.claude/docs/baselines-repo-layout.md`
- **PR reference resolver**: `.claude/docs/pr-resolver.md`
- **API surface classification**: `.claude/docs/api-surface-classification.md`

## When to run

Only when the user explicitly invokes `/verify-review`. Do not run it opportunistically after a PR push.

## Steps

### 1. Resolve the target PR

Per `.claude/docs/pr-resolver.md`. If the branch has no PR, stop — there's nothing to verify.

### 2. Collect all prior reviews on the PR

Single call:

```
pwsh -NoProfile -File .claude/scripts/pr-context.ps1 <<'EOF'
{ "pr": <n> }
EOF
```

This returns `reviews`, `reviewComments`, `issueComments`, `currentUser`, plus everything step 4 needs. Build the thread-ID map in a second Bash call:

```
gh api graphql -F pr=<n> -f query='…' # see .claude/docs/github-review-api.md → Thread-ID ← comment-databaseId mapping
```

Keep only reviews authored by `currentUser` — those are the reviews produced by prior `/review-pr` or `/verify-review` runs. List other reviews (human or bot) for the user, but do not parse or modify them.

Each kept review already carries its `review_id` in the `id` field of the listing response — record it; step 7 needs it to target a `PUT` at the right review.

Build the thread-ID map: `{comment_id (databaseId) → thread.id}` from the GraphQL response.

### 3. Parse prior findings

Per `.claude/docs/review-conventions.md` → **ID-continuation floor**: regex-match IDs across every prior review body and every review comment body authored by the current user. For each match, record:

```
{
  id: "BLK001",
  severity: "BLK",
  number: 1,
  location: {
    kind: "body" | "line" | "file",
    review_id: <int>,          # for kind=body
    comment_id: <int>,         # for kind=line|file
    thread_id: <string>,       # for kind=line (from the thread map above)
    path: "<file>",            # for kind=line|file
    line: <int>                # for kind=line only
  },
  original_text: "<paragraph/line containing the ID>",
  current_checkbox: " " | "x" | "~"   # for kind=body only
}
```

Dedup by ID — if the same ID appears in multiple places, keep the most recent location. Compute the **ID-continuation floor** per severity: `max(number) + 1`, or `1` when no prior matches.

### 4. Prepare change summary and baselines state

Execute the **Change summary** and **Baselines clone setup** sections of `.claude/docs/pr-context-prep.md` against the current PR HEAD. Per the project decision, baselines grouping is rerun from scratch in verify mode — do not try to diff incrementally against a prior baselines review.

### 5. Spawn subagents in `verify` mode (parallel)

Launch `code-reviewer` and `baselines-reviewer` in a single assistant turn with two Agent tool calls.

**`code-reviewer` briefing:**

- `mode: verify`
- PR metadata + linked-issue context (per `/review-pr` step 3).
- Change summary.
- **Prior findings list** (the full parsed structure from step 3).
- `writeDir: .build/.claude/pr<n>` — same disk-dump instruction as `/review-pr`, so the subagent navigates file bodies via `Read`/`Grep` on a real path.
- ID-continuation floor per severity.

The subagent returns `prior_finding_status` (fixed / still_actual / partial), plus fresh `findings` for `partial` cases and any genuinely new issues, plus `api_changes`.

**`baselines-reviewer` briefing:** same inputs as `/review-pr`, with `mode: verify`.

### 6. Apply API-surface classification

Run the decision tree in `.claude/docs/api-surface-classification.md` against the fresh `api_changes` (not the prior one). Produces the new set of notes and any fresh BLK findings.

### 7. Plan the updates

For each prior finding, pick the update action based on `status` × `location.kind`:

| status       | location.kind | Action |
|--------------|---------------|--------|
| fixed        | body          | Edit the prior review body via `PUT` (see github-review-api): flip `[ ]` → `[x]` on that finding's line. |
| fixed        | line          | `PATCH` the comment body: append `\n\n— ✓ Fixed in <head_sha_short>`. Offer to resolve the thread (per-thread confirmation, batched in step 8). |
| fixed        | file          | `PATCH` the comment body with the same fixed annotation. (File-subject threads don't have a GraphQL resolve equivalent — leave as-is.) |
| still_actual | any           | No-op. Listed in the new draft review's verification header for reviewer visibility. |
| partial      | body          | Edit the prior review body: flip `[ ]` → `[~]`. Also post a fresh follow-up finding in the new draft review, referencing the original ID. |
| partial      | line          | Reply to the thread (`POST /pulls/<n>/comments/<comment_id>/replies`) as part of the new draft review, quoting the original and explaining the residual concern. |
| partial      | file          | Post a fresh file-level comment in the new draft review, referencing the original ID. |

Edit-in-place of prior review bodies uses GitHub's `PUT` endpoint — see `.claude/docs/github-review-api.md`. Mechanics: the prior review body is already in memory from step 2's `GET /pulls/<n>/reviews` response — do not re-fetch. Apply a targeted substring replacement on the exact line containing the finding's ID to flip its checkbox, then PUT the whole new body in one call per review.

### 8. Confirm everything with the user in one batch

Print a single plan preview that includes:

- All planned in-place edits (body PUTs, comment PATCHes) grouped by review
- All planned thread-resolve mutations (each with a yes/no slot)
- Count of partial-fix follow-ups and genuinely new findings
- Compact baselines grouping summary
- Any `compressionFeedback[]` entries from `baselines-reviewer` — present as **"Proposed follow-up improvements to `baselines-diff.ps1`'s normaliser"**, one short bullet per entry; not part of the verification output itself
- The final single question "Proceed? [y / edit / cancel]"

Ask **all** questions in this one message — the main "proceed" question and the per-thread resolve answers. Wait for the batched reply before any write.

### 9. Apply in-place edits

Order (some steps are independent and may be batched as parallel Bash calls in one assistant turn; I note this explicitly per step):

1. **Review-body PUTs** — one PUT per distinct prior review that needs flipping. Different reviews may go **in parallel** in one turn; the same review must go serially (there's only one PUT per review body).
2. **Comment PATCHes** — each targets a distinct comment, so all may go **in parallel** in one turn. May also run **in parallel with the PUTs in step 1** since they target different resources.
3. **Thread-resolve GraphQL mutations** — for each thread the user approved. All may go **in parallel** in one turn. Also **parallel with steps 1–2**.

If any write fails, stop and report; do not attempt rollback (these are human-reviewable state changes — partial progress is preferable to silently losing work).

### 10. Post the new draft review

Only if there is anything to post (partial-fix follow-ups, fresh findings, fresh baselines grouping that differs meaningfully from the prior review, or a verification header the user wants visible). If the plan is purely "edit in place, nothing new", skip this step.

Follow `/review-pr` step 8 — post via the `post-pr-review.ps1` wrapper (see `.claude/docs/github-review-api.md` → **Posting a review via the wrapper**). One Bash call:

```
pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1 <<'EOF'
{ "pr": <n>, "commitId": "<sha>", "body": "…follow-up body…", "lineComments": [...], "fileComments": [...] }
EOF
```

Body template:

```
## Verification update — <date>, against HEAD <short_sha>

### Fixed
- [x] BLK001 — <short title>
- [x] MAJ003 — <short title>

### Still actual
- [ ] MAJ002 — <short title>   (original: review #<review_id>)

### Partially / incorrectly fixed
- [ ] MIN007 — <short title>   (original: review #<review_id>; follow-up: MIN008 below)

### New findings
<severity-grouped entries — body structure per .claude/docs/review-conventions.md>

## Baselines
<from baselines-reviewer output>
```

Per-line comments (new findings and reply-to-thread follow-ups) go into the `comments[]` array of the same new review.

Post it as PENDING — omit the `event` field. Reason and exact command in `.claude/docs/github-review-api.md`.

### 11. Report

- New draft review URL and ID (if a new review was posted)
- Count of body PUTs, comment PATCHes, threads resolved
- Reminder that the draft review (if posted) needs to be submitted manually on GitHub

## Don'ts

- **Never submit** the new review — omit `event`.
- **Never resolve a thread** without explicit per-thread user approval in step 8.
- **Never delete** a prior review or comment. Only `PUT` / `PATCH`.
- Do not edit reviews authored by other users.
- Do not edit any source file or push anything.
- Do not skip the batched confirmation in step 8 — one preview, one approval, then all writes in order.
