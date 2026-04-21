---
name: review-pr
description: Deep professional review of a linq2db PR. Accepts PR link, PR number, a linked issue/task number, or a branch name. Loads PR + comments + linked issues, prepares a change summary, spawns code-reviewer and baselines-reviewer subagents in parallel, classifies public-API changes against the PR milestone, assembles a severity-ordered finding list, and posts a draft pending review on GitHub after user confirmation. Never commits or edits code.
---

# review-pr

User-triggered workflow to review a PR on `linq2db/linq2db`.

Shared reference material:

- **Review orchestration** (shared skeleton with `/verify-review`): `.claude/docs/review-orchestration.md`
- **Review conventions** (severities, IDs, checkboxes, body structure): `.claude/docs/review-conventions.md`
- **GitHub review API** (endpoints, gotchas, thread-id mapping): `.claude/docs/github-review-api.md`
- **PR context prep** (one-call loader, change summary, baselines clone): `.claude/docs/pr-context-prep.md`
- **Baselines repo layout** (branch naming, file grammar): `.claude/docs/baselines-repo-layout.md`
- **PR reference resolver** (URL / number / issue / branch): `.claude/docs/pr-resolver.md`
- **API surface classification** (milestone-driven note-vs-BLK rules): `.claude/docs/api-surface-classification.md`
- **Review posting** (manifest format + wrapper invocation): `.claude/docs/review-posting.md`

The workflow relies on five PowerShell Core helper scripts to keep the permission surface to one allowlist entry per script. They share a common shape (stdin JSON → stdout JSON, no temp files, no compound commands) — see `.claude/docs/agent-rules.md` → **PowerShell Core scripts for complex operations** for the pattern:

- `.claude/scripts/pr-context.ps1` — fetches PR metadata, reviews, comments, linked issues, diff stat / name-status / commits, `origin/pr/<n>` head, in one call.
- `.claude/scripts/diff-reader.ps1` — batch file content + diff + hunk reader, called by `code-reviewer`.
- `.claude/scripts/verify-lines.ps1` — batch snippet + hunk verification, called by `code-reviewer`.
- `.claude/scripts/baselines-diff.ps1` — one-shot baselines diff + grammar parse, called by `baselines-reviewer`.
- `.claude/scripts/post-pr-review.ps1` — REST review POST + file-thread GraphQL in one process.

## When to run

Only when the user explicitly invokes `/review-pr <ref>`. Reference forms and resolver are defined in `.claude/docs/pr-resolver.md`. Draft PRs are reviewed the same way as ready-for-review PRs.

## Steps

Permission-prompt discipline, PR resolution, context loading, subagent spawning, API classification, posting, and the command-usage audit closing step are defined once in [`review-orchestration.md`](../../docs/review-orchestration.md). This skill layers `initial`-mode specifics on top: a **scope confirmation gate** (step 4 below), a **target-branch warning** (step 3), and the **review-body assembly** (step 8).

### 1. Resolve the target PR

Per `review-orchestration.md` → **Resolving the target PR**.

### 2. Load context, summarize, prepare baselines

Per `review-orchestration.md` → **Loading PR context**.

### 3. Target-branch check

Using `pr.baseRefName` from step 2's context output, if it is not `master`, warn the user:

> This PR targets `<base>`, not `master`. Review anyway? [y/N]

Wait for an explicit `y`. No other guards (no draft-PR guard, no size guard).

### 4. Pre-review confirmation

After the target-branch check passes and the change summary is in hand, ask the user two bundled questions in a single prompt so both answers land in one reply (per `agent-rules.md` → **Batching and user interaction**):

> Before I run the reviewers:
> 1. My read of the scope: `<one–two-sentence summary>`. Confirm? [y / correction / skip]
> 2. Include baselines review (test/SQL baseline diff analysis)? [y / n, default y]

**Question 1 — scope.** Answers:
- `y` — proceed with the stated scope as the confirmed scope.
- A correction — re-state the corrected scope in one sentence back to the user for implicit confirmation (no second prompt), then proceed with the corrected version.
- `skip` — proceed without a confirmed scope (only when the user explicitly opts out).

Carry the confirmed scope forward into the `code-reviewer` briefing (step 6) as an explicit `scope` field. The reviewer uses it to keep findings inside the PR's intent and to push tangential concerns to `out_of_scope_observations[]` instead of `findings[]` (see `.claude/agents/code-reviewer.md` → **Scope discipline**). Without this gate, it's easy to surface findings about pre-existing behavior that the PR doesn't cause and wasn't trying to address.

**Question 2 — baselines opt-out.** Default is include. Answers:
- `y` (or empty) — spawn `baselines-reviewer` in step 6 as usual.
- `n` — skip the `baselines-reviewer` spawn entirely. Step 6 runs `code-reviewer` alone; the `## Baselines` section in step 8 is replaced with a single line `Baselines review skipped per user request.` and none of the per-group rendering applies. Use this when the PR has no baseline changes, or when the user has already reviewed them separately and wants to save a subagent run.

### 5. Compute the ID-continuation floor

Per `.claude/docs/review-conventions.md` → **ID-continuation floor**: using `reviews` + `reviewComments` + `currentUser` already loaded in step 2, filter to entries authored by `currentUser`, regex-match IDs across their bodies, compute `max(NNN) + 1` per severity. If none, floor is `1` for every severity. Both subagents and the final assembly need it.

The floor is internal numbering bookkeeping — it steers the IDs you assign, not content for the reader. **Do not mention it in the review body** (not under `## Review notes`, not as a trailing meta line). The reader sees IDs like `MIN001` / `MIN014` directly; they don't need to know what the starting point was.

### 6. Spawn the two subagents in parallel

Per `review-orchestration.md` → **Spawning the two subagents in parallel**. This skill adds only `initial`-mode specifics on top of the common briefing:

- **`code-reviewer`:** `mode: initial`; **confirmed scope** from step 4 (absent only when the user explicitly opted out via `skip` — the reviewer falls back to the change summary in that case); ID-continuation floor per severity (from step 5).
- **`baselines-reviewer`:** `mode: initial`. **Skip this spawn entirely** when the user answered `n` to step 4's question 2 — fire only the `code-reviewer` Agent call. The two-subagents-in-one-turn rule in `review-orchestration.md` still applies when both run; when only one runs, issue just that one call.

### 7. Classify public-API surface changes

Per `review-orchestration.md` → **Classifying public-API surface changes**.

`code-reviewer` already verifies its own line numbers (see its spec's **Line-number verification** section). Trust that output — do not re-run verification here, and in particular do not `git show origin/pr/<n>:path` to spot-check snippets. The subagent's first `diff-reader.ps1` call with `writeDir: .build/.claude/pr<n>` persisted every changed file's full HEAD body, base-ref body, and per-file diff to disk — if parent-skill reasoning ever needs to look at a file, `Read` / `Grep` it directly at the paths listed in `.claude/docs/pr-context-prep.md` → **`writeDir` directory layout**. Do **not** `ls` the directory to discover the shape; the layout is fixed and documented. Re-fetching via `git show ref:path | sed -n` pipes costs a permission prompt each and is forbidden. Post-subagent sanity is limited to: each `line` is a positive integer, `line_end >= line` when present, and `file` points to a path that actually appears in the PR's changed-file list from step 2. Findings that fail those lightweight checks go straight to body-section — no disk caching, no second pass.

### 8. Assemble the review body

Use the body structure defined in `.claude/docs/review-conventions.md` → **Output body structure**. No legend table — reviewers who need abbreviation meanings consult the conventions doc.

Classify each `code-reviewer` finding into one of three review output locations:

| Finding has | Posted as | Shape |
|---|---|---|
| `file` **and** `line` | Line review comment in the review's `comments[]` | `{path, line, side: "RIGHT", start_line?, body}` |
| `file` but no `line` | File-level thread via GraphQL `addPullRequestReviewThread`, posted **after** the REST review create (step 9) — **not** in `comments[]` | n/a in REST bulk POST |
| Neither | Body-section entry under the severity heading | checkbox `[ ]`, `**<ID>** — <title>`, `Why: …`, `Fix: …` |

**No duplication across locations.** Each finding appears in **exactly one** of the three rows above — never in two. In particular, do **not** also render line-level findings as body-section bullets under their severity heading (e.g. a `- [ ] **NIT004** — … (see inline thread)` row when NIT004 is already posted as a line comment). The severity sections in the body are for findings that have no line anchor; populate them only from findings that fall into the "Neither" row. Before writing the body, filter `findings[]` to the "Neither" set, then group by severity — don't iterate the whole list. Empty severity sections are omitted entirely (no `## Minor` heading when every minor is line-level).

**Out-of-scope observations.** If `code-reviewer` returns a non-empty `out_of_scope_observations[]`, render them as a dedicated section near the end of the body, between the body-section findings and the `## Baselines` section:

    ## Out-of-scope observations

    Surfaced during review but fall outside this PR's scope. Not findings on this PR — included as FYI.

    - **<title>** — <description>

Omit the section entirely when the array is empty. Do not classify out-of-scope observations by severity and do not convert them to line/file comments — they are not findings.

For line/file comments, build the `body` field as plain markdown with the shape below. The leading `<Severity>` is the spelled-out name (`Blocker`, `Major`, `Minor`, `Suggestion`, `Nit`) so a human reader seeing an isolated comment on a file line decodes the ID without context. (Shown as an indented block so the inner suggestion fence renders correctly in this doc — the actual `body` string contains the literal backticks.)

    **<Severity> · <ID>** — <why>

    Fix: <fix>

    ```suggestion
    <replacement code — only when the finding has a concrete `suggestion` value>
    ```

Append the suggestion fence only when `suggestion` is set. GitHub requires the fenced block body to be the exact replacement for the commented-on line range, preserving indentation.

**Suggestion-block audit.** Per `code-reviewer.md` → output rules, every **line-level** finding whose fix is expressible as a textual replacement must carry `suggestion`. Run this audit explicitly as a distinct step before building the manifest — don't fold it into general reasoning, or it will be skipped.

1. Enumerate every line-level finding returned by `code-reviewer` (has both `file` and `line`). Count them.
2. For each finding without `suggestion`, classify as one of:
   - **Structural omission (OK).** Fix affects lines outside the commented range, requires a new method / type / file, moves code across files, spans disjoint spots, or describes a design change the human must apply. Examples from prior runs: "move class to Internal.SqlQuery", "split into a separate PR", "add new method elsewhere".
   - **Textual replacement (not OK — must synthesize).** Single-line rewrite, whitespace or indent fix, blank-line removal (use empty suggestion), column realignment over a range (compute aligned form), XML-doc edit, exception-message change, boolean / field flip, or one option of a multi-option fix that's expressible as a replacement.
3. For every "textual replacement (not OK)" case, synthesize the `suggestion` field yourself from the prose `fix` and the cached HEAD file content under `.build/.claude/pr<n>/<path>` (use `Read` / `Grep` — the file is already on disk from the subagent's first `diff-reader.ps1` call). Only drop to file-level (remove `line`) if you genuinely cannot compute the replacement.
4. Report the audit tally to the user as part of the pre-post summary (step 9) in the form: `audited N line-level findings → K with suggestions, M structural omissions, P synthesized here`. This makes the audit a visible user-facing step, not a silent pass-through.

Do not post a line-level finding with a replaceable fix but no suggestion block.

**Baselines section rendering.** Use the subagent's structured output to compose the `## Baselines` section with these rules:

1. **Section header.** Lead with one sentence citing the baselines review anchor:

       ## Baselines
       Delta: [linq2db.baselines PR #<baselineReview.number>](<baselineReview.url>) (<baselineReview.state>) · [compare view](<baselineCompareUrl>)

   If `baselineReview` is null, drop the PR link and keep the compare link only. If `status == "no_baselines"`, emit `No baseline changes.` and skip the rest. **If the user opted out of baselines review in step 4**, render the section as a single line — `Baselines review skipped per user request.` — and skip every rule below.

2. **Per-group heading.** One `###` heading per `groups[].heading`, optionally followed by the group's `summary`.

3. **Per-subgroup rendering.** One `-` bullet per subgroup, prefixed with `**<reason>** — <subgroup.summary>`. Then render its entries as a nested list:

   | entry `sampleStatus` | Rendering |
   |---|---|
   | `A` (added)    | `- [<test>](<sampleUrl>) — added (<providerCount> providers: <comma list>)` |
   | `M` (modified) | `- [<test>](<sampleUrl>) — modified (<providerCount> providers: …)` followed by a collapsed `<details><summary>sample diff</summary>` block containing the `sampleDiff` inside a ```diff fence. |
   | `D` (deleted)  | `- <test> — deleted (<providerCount> providers: …)` (plain text, no link) |

   Provider lists longer than ~8 items get compacted to `<first 5>, … (N providers total)`. Entry `note` fields go after the parenthetical on the same line.

4. **Cross-provider anomalies** under `### Cross-provider anomalies`, one bullet per entry.

5. **Compression feedback.** Do NOT render `compressionFeedback[]` in the review body — that surfaces separately in step 9 as proposed follow-up improvements.

Entries with empty `sampleUrl` / `samplePath` (rollup entries not tied to a specific pattern) render as plain `- <test> — <providers…>` with no link and no diff block.

### 9. Confirm with user, then post

Show the user:

- The assembled review body
- Summary counts: N per-line comments, M file-level comments, K body-section findings by severity, O out-of-scope observations, baselines status
- Any `compressionFeedback[]` entries from `baselines-reviewer` — present these as **"Proposed follow-up improvements to `baselines-diff.ps1`'s normaliser"**, one short bullet per entry. These are not part of the review itself; the point is to let the user decide whether to act on them in a separate change after the review is posted.

Wait for an explicit "post" / "yes".

On approval, post the pending review via the `post-pr-review.ps1` wrapper. **Posting mechanics — manifest-script format, invocation, manifest-to-finding mapping, verify semantics, heredoc caveats, and the stdout reporting shape — are defined in [`.claude/docs/review-posting.md`](../../docs/review-posting.md)**. The skill's job here is to supply the per-review content that fills the manifest template.

Per-review content for this skill:

- **Manifest path:** `.build/.claude/pr<n>-manifest.ps1`.
- **`body` here-string:** the assembled review body from step 8, opened by the agentic-review disclaimer and containing the review-notes section, the body-section findings grouped by severity, the `## Out-of-scope observations` section (when non-empty), and the baselines section.
- **`lineComments[]`:** every finding with both `file` and `line`. Rebuild per finding per the line-comment body shape in `.claude/docs/review-conventions.md` → **Output body structure** (so each comment leads with `**<Severity> · <ID>**`). Include a `suggestion` fenced block when the finding has one, per the **Suggestion-block audit** above.
- **`fileComments[]`:** every finding with `file` but no `line`.
- **`replyComments[]`:** empty on initial `/review-pr` runs. Reserved for `/verify-review` follow-ups and for retractions of previously-posted findings (see `.claude/docs/review-posting.md` → **Retracting a posted finding**).

### 10. Offer command-usage audit

Per `review-orchestration.md` → **Command-usage audit (closing step)**.

## Don'ts

- **Do not submit** the review. Omit `event` — this is what creates a PENDING draft.
- Do not edit any source file.
- Do not post individual comments with `POST /pulls/<n>/comments` — always go through the reviews endpoint so all findings land inside one draft.
- Do not continue to posting if the user hasn't explicitly approved.
- Do not flag the repo's column-aligned formatting — see `.claude/docs/code-design.md` → **Column-aligned formatting is intentional**.
- Do not embed a severity legend in the review body; the conventions doc is the single source of truth.
