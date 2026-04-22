---
name: merge-duplicates
description: Merge duplicate linq2db/linq2db issues into a canonical one. Consolidates non-overlapping details from dupes into a new comment on the canonical (with attribution, preserving code fences), propagates dupe-only labels onto canonical, posts a closing comment on each dupe, and closes each dupe with GitHub's `duplicate` state reason. Never edits any body or comment — always adds new content. Always gated by explicit user confirmation of the full action bundle. Closes dupes last so no rollback is ever needed.
---

# merge-duplicates

User-triggered write workflow to consolidate duplicate issues on `linq2db/linq2db`.

## When to run

Only when the user explicitly invokes this skill, typically after `/find-issues` in ticket mode has flagged a likely duplicate. Never proposes itself.

## Input

Accepted forms:
- Prose: "merge #5484 and #5485 into #5483"
- Positional: `/merge-duplicates <canonical> <dupe1> [<dupe2> ...]`
- Number-only first arg (rest derived by asking): `/merge-duplicates 5483` → prompt for dupes

The **first** number is always the canonical (issue to keep). Remaining numbers are dupes (to close in favor of canonical).

Accepts issue numbers only — no URLs, no PRs. If the user passes a PR number, stop and tell them `/merge-duplicates` is for issues; PRs don't get "merged as duplicate" the same way.

## Steps

### 1. Fetch all involved tickets

For canonical and each dupe, in parallel:

```
gh issue view <n> --repo linq2db/linq2db \
  --json number,title,body,labels,state,author,url,closedAt,comments
```

Reject the bundle if:
- Any ticket is already `closed` (flag and ask — merging into a closed canonical is sometimes intentional, but dupes must be open).
- Any ticket doesn't exist (404 → abort).
- The user passed a PR number (check via `gh pr view` silently; if it succeeds, abort).
- Canonical number appears in the dupe list (self-merge).

### 2. Propose canonical (only if computed suggestion differs from user's pick)

Compute a suggested canonical using:

1. Highest `comments` count wins.
2. Tiebreak: oldest `createdAt` among tickets with substantive bodies (>200 chars).
3. Tiebreak: most labels.

If the computed suggestion **differs** from the user's passed canonical, show both and ask — never silently swap. If they match, proceed without mentioning it.

Example prompt when they differ:

> You passed #5483 as canonical, but #5412 has more discussion (14 comments vs 0) and predates it. Swap canonical to #5412?

### 3. Diff content

For each dupe, extract content not already present in the canonical:

- **Body snippets** — reproductions, code samples, stack traces, error messages, version info, specific steps. Compare via substring match against the canonical body (truncated to ~2000 chars). Any paragraph in the dupe's body not substring-matched in the canonical is a consolidation candidate.
- **Comments** — any comment on the dupe not authored by the dupe's original reporter or a bot. Third-party user comments (people adding info) are the highest-value transfer target.
- **Labels** — any `provider: *`, `area: *`, or similar topic-scoped label on the dupe that the canonical lacks. Skip `type: *`, `status: *`, `severity: *`, `epic: *`, `resolution: *` (maintainer-managed, not topic-scoped).

If a dupe has no novel content (empty body, no comments, no extra labels), it contributes only the close action — no consolidation entry.

### 4. Draft the action bundle

Up to four action classes:

**A. Canonical consolidation comment** (only if any dupe contributed novel content)

Use `###` headers per source, not block-quotes. Reason: block-quote + fenced code block renders awkwardly on GitHub (each line needs `>` prefix, which breaks fences). Headers + plain-paragraph excerpts preserve code fences exactly as authored.

```markdown
Consolidated from duplicate issue(s):

### From #<dupe1> (by @<author1>)

<body snippet, copied verbatim — fenced ```code``` blocks preserved as-is>

### From #<dupe2> (comment by @<author2>)

<comment text, copied verbatim>
```

Never paraphrase. Truncate with `[…]` + a link to the source comment only when a single snippet exceeds ~2000 chars.

**B. Label additions on canonical** (one per new label)

```
gh issue edit <canonical> --repo linq2db/linq2db --add-label "<label>"
```

**C. Per-dupe closing comment** (one per dupe)

Text:

> Closing as duplicate of #<canonical>. Any additional details or reproductions — please add them to #<canonical>.

**D. Per-dupe close with duplicate state reason** (one per dupe)

Preferred:

```
gh issue close <dupe> --repo linq2db/linq2db --reason "duplicate"
```

If the installed `gh` doesn't support `--reason duplicate`, fall back to:

```
gh api -X PATCH repos/linq2db/linq2db/issues/<dupe> \
  -f state=closed -f state_reason=duplicate
```

Detect support once per session by running `gh issue close --help` and caching the answer.

### 5. Present the plan and wait

Show the full bundle in a structured form:

```
Canonical:   #5412 "Firebird 5: native IF EXISTS" (14 comments, provider: firebird, area:DDL)
Duplicates:  #5483, #5485

Planned actions:
  [A]  Post consolidation comment on #5412 (from #5483 body + #5485 comment by @user2)
  [B]  Add label "severity: regression" to #5412 (from #5485)
  [C1] Post closing comment on #5483
  [C2] Post closing comment on #5485
  [D1] Close #5483 with state_reason=duplicate
  [D2] Close #5485 with state_reason=duplicate
```

Then display the **full text** of every comment that would be posted (A, C1, C2). No abbreviation — the user must see the literal content before approving.

Wait for one of:
- `all` — execute every action in order
- `skip <N>` (one or more dupe numbers) — drop actions for dupe `<N>` but proceed with the rest, including A if any other dupes still contribute content
- `no` — abort, make no changes

### 6. Execute — closes last, no rollback needed

Only after explicit approval. Three phases:

**Phase 1** (parallel): Action A (consolidation comment on canonical) + Action B (label additions on canonical).

**Phase 2** (parallel across dupes): For each non-skipped dupe, post Action C (closing comment). Track per-dupe success.

**Phase 3** (parallel across dupes): Only for dupes whose Phase-2 comment succeeded, fire Action D (close with `state_reason=duplicate`).

If C fails for a dupe, its D is skipped — dupe stays open, no manual reopen needed. Other dupes proceed independently. Every failure mode leaves the world recoverable without rollback.

Use:

```
gh issue comment <n> --repo linq2db/linq2db --body-file <path>
```

Write each comment body to `.build/.claude/merge-dupes-<n>-<kind>.md` first — `gh` chokes on heredocs for long bodies.

### 7. Report

After execution:

```
✓ Posted consolidation comment on #<canonical>  → <url>
✓ Added label "<label>" to #<canonical>
✓ Closed #<dupe1> as duplicate                    → <url>
✓ Closed #<dupe2> as duplicate                    → <url>
✗ Skipped #<dupe3> — comment post failed: <error>
```

### 8. Do not

- Edit any existing comment, issue body, or PR body. See `agent-rules.md` → **GitHub content authored by others**. Every addition is a new comment.
- Close the canonical.
- Attempt rollback on partial failure — the close-last execution order ensures no rollback is ever needed. Report state and stop.
- Act across repos. `linq2db/linq2db` only.
- Invoke `/find-issues` on the user's behalf — the user chose the dupes.
