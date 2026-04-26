---
name: verify-review
description: Re-verify prior `/review-pr` findings on a linq2db PR against the current PR HEAD. Collects all prior reviews, parses findings by severity-ID, reruns code-reviewer and baselines-reviewer in `verify` mode, then applies in-place edits (checkbox flips on prior review bodies, annotations + thread resolves on prior review comments) and posts a new draft review for partial fixes and new findings. Requires explicit user confirmation before any GitHub write.
---

# verify-review

User-triggered follow-up workflow after the PR author has addressed findings from a prior `/review-pr`. Invoke as `/verify-review <ref>`.

Shared reference material:

- **Review orchestration** (shared skeleton with `/review-pr`): `.claude/docs/review-orchestration.md`
- **Review conventions**: `.claude/docs/review-conventions.md`
- **GitHub review API**: `.claude/docs/github-review-api.md`
- **PR context prep**: `.claude/docs/pr-context-prep.md`
- **Baselines repo layout**: `.claude/docs/baselines-repo-layout.md`
- **PR reference resolver**: `.claude/docs/pr-resolver.md`
- **API surface classification**: `.claude/docs/api-surface-classification.md`
- **Review posting**: `.claude/docs/review-posting.md`

## When to run

Only when the user explicitly invokes `/verify-review`. Do not run it opportunistically after a PR push.

## Steps

Permission-prompt discipline, PR resolution, context loading, subagent spawning, API classification, posting, and the command-usage audit closing step are defined once in [`review-orchestration.md`](../../docs/review-orchestration.md). This skill layers `verify`-mode specifics on top: collecting **prior reviews** and **prior findings** (steps 2–3), the **per-finding action table** (step 7), and the **in-place edits** via `apply-verify-writes.ps1` (step 9).

### 1. Resolve the target PR

Per `review-orchestration.md` → **Resolving the target PR**.

### 2. Collect all prior reviews on the PR

Load PR context per `review-orchestration.md` → **Loading PR context**. The one `pr-context.ps1` call returns `reviews`, `reviewComments`, `issueComments`, `currentUser`, and `reviewThreads[]` (the databaseId → thread.id map with `isResolved` flags), plus everything step 4 needs. **No separate `gh api graphql` call** — the mapping is already in-hand.

Keep only reviews authored by `currentUser` — those are the reviews produced by prior `/review-pr` or `/verify-review` runs. List other reviews (human or bot) for the user, but do not parse or modify them.

Each kept review already carries its `review_id` in the `id` field of the listing response — record it; step 7 needs it to target a `PUT` at the right review.

Build an in-memory lookup `{ comment_id → { threadId, isResolved } }` by matching each `reviewComments[*].id` against `reviewThreads[*].firstCommentId`.

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

Per `review-orchestration.md` → **Spawning the two subagents in parallel**. This skill adds only `verify`-mode specifics on top of the common briefing:

- **`code-reviewer`:** `mode: verify`; **prior findings list** (the full parsed structure from step 3). The subagent returns `prior_finding_status` (fixed / still_actual / partial), plus fresh `findings` for `partial` cases and any genuinely new issues, plus `api_changes`.
- **`baselines-reviewer`:** `mode: verify`.

### 6. Apply API-surface classification

Per `review-orchestration.md` → **Classifying public-API surface changes**, against the fresh `api_changes` (not the prior one). Produces the new set of notes and any fresh BLK findings.

### 7. Plan the updates

For each prior finding, pick the update action based on `status` × `location.kind`:

| status       | location.kind | Action |
|--------------|---------------|--------|
| fixed        | body          | Edit the prior review body via `PUT` (see github-review-api): flip `[ ]` → `[x]` on that finding's line. |
| fixed        | line          | `PATCH` the comment body: append `\n\n— ✓ Fixed in <head_sha_short>`. Offer to resolve the thread (per-thread confirmation, batched in step 8). |
| fixed        | file          | `PATCH` the comment body with the same fixed annotation. (File-subject threads don't have a GraphQL resolve equivalent — leave as-is.) |
| still_actual | any           | No-op. Listed in the new draft review's verification header for reviewer visibility. |
| partial      | body          | Edit the prior review body: flip `[ ]` → `[~]`. Also post a fresh follow-up finding in the new draft review, referencing the original ID. |
| partial      | line          | Add an entry to the new draft review's `replyComments[]` with `inReplyTo` set to the existing comment's GraphQL node ID and a body that quotes the original and explains the residual concern. The wrapper attaches it via `addPullRequestReviewComment` scoped to the new pending review, so the reply stays hidden until the user submits the draft (do **not** use the `/replies` REST endpoint — it posts immediately, outside the draft, which breaks the "preview before submit" flow). |
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

All three kinds of write — body PUTs, comment PATCHes, thread resolves — go through a single call to `.claude/scripts/apply-verify-writes.ps1`. One pwsh invocation, one allowlist rule, one permission prompt, regardless of how many writes the plan carries. The script handles fan-out parallelism internally.

```
pwsh -NoProfile -File .claude/scripts/apply-verify-writes.ps1 <<'EOF'
{
  "pr": <n>,
  "appendNote": "— ✓ Fixed in <head_sha_short>",
  "commentPatches": [<comment_id>, <comment_id>, ...],
  "threadResolves": [
    { "threadId": "PRRT_...", "label": "MIN001" }
  ],
  "reviewBodyEdits": [
    { "reviewId": <review_id>, "newBody": "<full replacement body>" }
  ]
}
EOF
```

Manifest rules:

- `commentPatches[]` entries may be bare integers (use top-level `appendNote`) or `{ commentId, appendNote }` (per-entry override — rarely needed). The script fetches the current comment body, appends `\n\n<appendNote>`, and `PATCH`es. Idempotent: if the note already appears in the body the write is skipped.
- `threadResolves[]` entries may be bare strings or `{ threadId, label }`. `label` surfaces in the output so per-finding pass/fail is identifiable.
- `reviewBodyEdits[]` needs the **full new body** — the caller is responsible for starting from the prior review body (already in memory from step 2's `GET /pulls/<n>/reviews` response) and doing the targeted substring replacement for each finding's checkbox. Do not re-fetch.

The script exits 0 on full success, 2 when at least one item failed (per-item `ok: false` in the JSON output). On failure, report which items failed and stop — do not attempt rollback (these are human-reviewable state changes; partial progress is preferable to silently losing work).

### 10. Post the new draft review

Only if there is anything to post (partial-fix follow-ups, fresh findings, fresh baselines grouping that differs meaningfully from the prior review, or a verification header the user wants visible). If the plan is purely "edit in place, nothing new", skip this step.

**Posting mechanics are defined in [`.claude/docs/review-posting.md`](../../docs/review-posting.md)** — manifest-script format, invocation, manifest-to-finding mapping, verify semantics, heredoc caveats, and the stdout reporting shape. The skill's job here is to supply the per-review content that fills the manifest template.

Per-review content for this skill:

- **Manifest path:** `.build/.claude/pr<n>-verify-manifest.ps1`.
- **`body` here-string:** the verification-update body using the template below.
- **`lineComments[]`:** every new finding with both `file` and `line`.
- **`fileComments[]`:** every new finding with `file` but no `line`.
- **`replyComments[]`:** partial-fix follow-ups from step 7. Each entry's `inReplyTo` is the GraphQL node ID of the existing review comment being replied to. Pull the node ID from the prior-comment data loaded in step 2; do **not** use the integer REST id (the GraphQL mutation rejects it).

Verification-update body template:

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
<!-- render per `/review-pr` → step 7 → **Baselines section rendering**:
     header with baselineReview URL + compare link, per-entry file links,
     sample diffs inside <details> for modified entries. -->
<from baselines-reviewer output>
```

### 11. Report

The wrapper's stdout block is covered in [`review-posting.md`](../../docs/review-posting.md) → **Reporting back to the user**. In addition to those fields, this skill also surfaces its own in-place-edit work:

- Count of body PUTs, comment PATCHes, and threads resolved in step 9
- Any step-9 writes that failed (those need retry)

### 12. Offer command-usage audit

Per `review-orchestration.md` → **Command-usage audit (closing step)**.

## Don'ts

- **Never submit** the new review — omit `event`.
- **Never resolve a thread** without explicit per-thread user approval in step 8.
- **Never delete** a prior review or comment. Only `PUT` / `PATCH`.
- Do not edit reviews authored by other users.
- Do not edit any source file or push anything.
- Do not skip the batched confirmation in step 8 — one preview, one approval, then all writes in order.
