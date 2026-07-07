---
name: verify-review
description: Re-verify prior `/review-pr` findings on a linq2db PR against the current PR HEAD. Collects all prior reviews, parses findings by severity-ID, reruns code-reviewer and baselines-reviewer in `verify` mode, then applies in-place edits (checkbox flips on prior review bodies, annotations + thread resolves on prior review comments) and posts a new draft review for partial fixes and new findings. Requires explicit user confirmation before any GitHub write.
---

# verify-review

User-triggered follow-up workflow after the PR author has addressed findings from a prior `/review-pr`. Invoke as `/verify-review <ref>`.

Shared reference material:

- **Review orchestration** (shared skeleton with `/review-pr`): `.agents/docs/review-orchestration.md`
- **Review conventions**: `.agents/docs/review-conventions.md`
- **GitHub review API**: `.agents/docs/github-review-api.md`
- **PR context prep**: `.agents/docs/pr-context-prep.md`
- **Baselines repo layout**: `.agents/docs/baselines-repo-layout.md`
- **PR reference resolver**: `.agents/docs/pr-resolver.md`
- **API surface classification**: `.agents/docs/api-surface-classification.md`
- **Review posting**: `.agents/docs/review-posting.md`

## When to run

Only when the user explicitly invokes `/verify-review`. Do not run it opportunistically after a PR push.

## Steps

Permission-prompt discipline, PR resolution, context loading, subagent spawning, API classification, posting, and the command-usage audit closing step are defined once in [`review-orchestration.md`](../../docs/review-orchestration.md). This skill layers `verify`-mode specifics on top: collecting **prior reviews** and **prior findings** (steps 2–3), the **per-finding action table** (step 7), and the **in-place edits** via `apply-verify-writes.ps1` (step 9).

### 1. Resolve the target PR

Per `review-orchestration.md` → **Resolving the target PR**.

### 2. Collect all prior reviews on the PR

Load PR context per `review-orchestration.md` → **Loading PR context**. The one `pr-context.ps1` call returns `reviews`, `reviewComments`, `issueComments`, `currentUser`, and `reviewThreads[]` — each entry shaped `{ threadId, isResolved, resolvedBy, firstCommentId }`, where `resolvedBy` is the GitHub login of the user who resolved the thread or `null` when the thread is open. The audit logic in step 2b reads `resolvedBy` directly (string compare against `currentUser`), no further GraphQL needed. **No separate `gh api graphql` call** — the mapping is already in-hand.

Reviews authored by `currentUser` are the only ones whose **bodies + line/file comments are parsed for prior findings** (step 3) — those use the structured `BLK001` / `MIN014` / etc. IDs we own.

Reviews and threads authored by **other users** (bots + humans) are not parsed for IDs, but are **still audited** in step 2b. Specifically, audit:

- Every open thread that is not `resolvedBy == currentUser`.
- Every closed thread `resolvedBy != currentUser` — the closure may have been premature.

Each kept review already carries its `review_id` in the `id` field of the listing response — record it; step 7 needs it to target a `PUT` at the right review.

Build an in-memory lookup `{ comment_id → { threadId, isResolved } }` by matching each `reviewComments[*].id` against `reviewThreads[*].firstCommentId`.

### 2b. Audit prior reviewer claims (bot + human)

Run the same audit pass as [`/review-pr` step 2b](../review-pr/SKILL.md). Scope is both surfaces from **other** authors (bot + human): every thread in `reviewThreads[]` not `resolvedBy == currentUser` (open + closed-by-others), **and** every discrete claim in the `body` of any `reviews[]` entry whose `user != currentUser` not already covered by one of that review's threads. Classify each as **Fixed at HEAD** / **Inaccurate at HEAD** / **Still actual**, surface every verdict in the `## Prior-review audit` section of the new draft review's body, then disposition the **thread** items per the same table (body-summary claims have no thread to mutate — the audit line is their disposition; Still-actual ones also feed the finding stream):

- Fixed / Inaccurate → reply + resolve (`{ resolve: true }` in the `post-pr-thread-replies.ps1` manifest).
- Still actual + resolved-by-other → reply + unresolve (`{ unresolve: true }`).
- Still actual + open → leave open, feed into the regular finding stream.

Apply the per-rule classification calibration (rules 1, 4, 5, 6 of `code-reviewer.md`) only to **LLM-reviewer** threads — those are the rules external bots most often outpace the subagent on. For human-reviewer threads, re-verify the concern factually against HEAD without applying bot-specific patterns.

**Same review-quality-signal capture applies.** When step 2b here classifies a thread as Still actual AND the corresponding concern is not present in `code-reviewer`'s `findings[]` for the verify run, append one JSON-line entry to `.build/.agents/review-quality-signal.jsonl` per the schema in `/review-pr` SKILL step 2b. The verify pass and the initial pass write to the same log.

### 3. Parse prior findings

Per `.agents/docs/review-conventions.md` → **ID-continuation floor**: regex-match IDs across every prior review body and every review comment body authored by the current user. For each match, record:

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

Execute the **Change summary** and **Baselines clone setup** sections of `.agents/docs/pr-context-prep.md` against the current PR HEAD. Per the project decision, baselines grouping is rerun from scratch in verify mode — do not try to diff incrementally against a prior baselines review.

### 5. Spawn subagents in `verify` mode (parallel)

Per `review-orchestration.md` → **Spawning the two subagents in parallel**. This skill adds only `verify`-mode specifics on top of the common briefing:

**Refresh the diff cache before spawning (verify-mode only).** A prior `/review-pr` run leaves a `writeDir` cache populated at *that* run's HEAD. Re-run `diff-reader.ps1` for all changed files at current HEAD first, so a subagent whose own `diff-reader` call is denied (background runs can't surface a permission prompt) falls back to a *fresh* cache rather than the stale prior-run one — a stale cache produces false line-level findings (a dropped-then-readded comment surfaced as a NIT on PR #5639). Pairs with the live-blob cross-check in `pr-context-prep.md` → *Cache freshness*.

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

Edit-in-place of prior review bodies uses GitHub's `PUT` endpoint — see `.agents/docs/github-review-api.md`. Mechanics: the prior review body is already in memory from step 2's `GET /pulls/<n>/reviews` response — do not re-fetch. Apply a targeted substring replacement on the exact line containing the finding's ID to flip its checkbox, then PUT the whole new body in one call per review.

### 7b. Out-of-scope disposition gate

When the verify-mode `code-reviewer` returned a non-empty `out_of_scope_observations[]`, run the per-observation **promote / create-tracking-issue / leave-as-is** gate exactly as [`/review-pr` step 7b](../review-pr/SKILL.md) — a single batched prompt, the user decides each one before the preview. Promoted observations become fresh findings in the new draft review (numbered from the step-3 floor); tracked ones get a `/create-issue` filing and a `— tracked as #<n>` suffix; leave-as-is ones render in the new review's `## Out-of-scope observations` section. Skip when the array is empty.

### 8. Preview, then run the mode-choice gate

Print a single plan preview that includes:

- All planned in-place edits (body PUTs, comment PATCHes) grouped by review
- All planned thread mutations (resolve for Fixed/Inaccurate; **unresolve** for Still-actual + resolved-by-other)
- Count of partial-fix follow-ups and genuinely new findings
- Count of out-of-scope observations from the verify-mode `code-reviewer` run
- Compact baselines grouping summary
- Any `compressionFeedback[]` entries from `baselines-reviewer` — present as **"Proposed follow-up improvements to `baselines-diff.ps1`'s normaliser"**, one short bullet per entry; not part of the verification output itself

Then run the **mode-choice gate** defined in [`review-orchestration.md`](../../docs/review-orchestration.md) → **Mode-choice gate**. Verify-mode specifics:

- On `submit-all`: post the new draft review via `post-pr-review.ps1`, run the step-2b thread-disposition bundle through `post-pr-thread-replies.ps1`, and run `apply-verify-writes.ps1` for prior-review in-place edits (step 9 below). One preview, one approval, all writes go.
- On `interactive`: walk every reviewable item (partial-fix follow-ups, new findings, out-of-scope observations, baselines anomalies, audited threads) per the orchestration doc's order, with per-item `fix | reject | accept-for-post`. Items accepted for post accumulate into the final draft review; in-place edits run after the walk completes.
- On `cancel`: exit without writes.

### 9. Apply in-place edits

All three kinds of write — body PUTs, comment PATCHes, thread resolves — go through a single call to `.agents/scripts/apply-verify-writes.ps1`. One pwsh invocation, one allowlist rule, one permission prompt, regardless of how many writes the plan carries. The script handles fan-out parallelism internally.

```
pwsh -NoProfile -File .agents/scripts/apply-verify-writes.ps1 <<'EOF'
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

**Posting mechanics are defined in [`.agents/docs/review-posting.md`](../../docs/review-posting.md)** — manifest-script format, invocation, manifest-to-finding mapping, verify semantics, heredoc caveats, and the stdout reporting shape. The skill's job here is to supply the per-review content that fills the manifest template.

Per-review content for this skill:

- **Manifest path:** `.build/.agents/pr<n>-verify-manifest.ps1`.
- **`body` here-string:** the verification-update body using the template below.
- **`lineComments[]`:** every new finding with both `file` and `line`.
- **`fileComments[]`:** every new finding with `file` but no `line`.
- **`replyComments[]`:** partial-fix follow-ups from step 7. Each entry's `inReplyTo` is the GraphQL node ID of the existing review comment being replied to. Pull the node ID from the prior-comment data loaded in step 2; do **not** use the integer REST id (the GraphQL mutation rejects it).

Verification-update body template:

```
## Verification update — <date>, against HEAD <short_sha>

## Prior-review audit
<!-- step-2b audit of OTHER authors' threads + review-body summaries; omit when none.
     One line per item: **<verdict>** · <author> — <claim> per review-conventions.md. -->

### Fixed
- [x] BLK001 — <short title>
- [x] MAJ003 — <short title>

### Still actual
- [ ] MAJ002 — <short title>   (original: review #<review_id>)

### Partially / incorrectly fixed
- [ ] MIN007 — <short title>   (original: review #<review_id>; follow-up: MIN008 below)

### New findings
<severity-grouped entries — body structure per .agents/docs/review-conventions.md>

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
- **Don't act on a prior-review out-of-scope observation as a fixable defect without confirming its premise against current source.** OOS observations carry no severity and are often hedged ("if X holds…") — lower confidence than findings. Before fixing one (especially on a user "fix it" request), verify its load-bearing claim at HEAD; a "dead"-code OOS may be live, a "wrong default" OOS may already be correct. Same discipline as `agent-rules.md` → *Issue-proposed fix details are written from memory*. (Surfaced on PR #5639: a "dead" copy ctor was live — it backs every record `with` — and a "wrong default" was already correct.)
