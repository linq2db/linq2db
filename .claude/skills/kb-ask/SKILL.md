---
name: kb-ask
description: Free-form Q&A grounded in the linq2db knowledge base. Spawns the kb-research subagent with a curated KB-doc shortlist; the agent answers from KB only and returns a synthesized answer plus citations. Optionally fetches full GitHub issue/PR bodies on demand. Read-only; never modifies the repo or the KB.
---

# /kb-ask

User-triggered Q&A over the knowledge base. The skill builds a doc shortlist, hands the question to `kb-research`, and prints the synthesized answer.

## Shared reference material

- [`../../agents/kb-research.md`](../../agents/kb-research.md) — query agent contract
- [`../../docs/kb-architecture.md`](../../docs/kb-architecture.md) — KB layout (used to scope shortlist)
- [`../../docs/kb-areas.md`](../../docs/kb-areas.md) — area registry (for keyword → area inference)

## When to run

- Any KB-grounded question: "How does X work?", "Why is Y this way?", "What's the convention for Z?", "What's tracked in the GH wiki about provider P?"
- Inside other skills (e.g. `/fix-issue` step 1, `/review-pr` scope confirmation): callers can spawn `kb-research` directly with the same contract; `/kb-ask` is just the user-facing entry point.

The skill is **not** for:
- Live source-code questions where the KB is stale ("what does file X line 42 do right now?"). Use `Read` directly.
- Operations that need writing (creating issues, fixing bugs). Use `/create-issue`, `/fix-issue`.

## Pre-conditions

- `.claude/knowledge-base/` exists with at least step 0 + step 2 done (architecture overview present). Otherwise:
  ```
  KB is not built yet. Run /kb-build first.
  ```

## Steps

### 1. Parse the question

The question is the skill's argument: `/kb-ask <question>`. If empty, prompt:

```
What would you like to know? (free-form):
```

### 2. Build the shortlist

Heuristics for which KB files to include in the agent's `scope`:

1. **Glossary first.** Always include `glossary.md` if it exists.
2. **Area inference.** Match question keywords against `kb-areas.md` Tier-1 file names + area codes (`oracle`, `firebird`, `expression`, `translator`, `bulk`). Hit areas → include their `INDEX.md`, `issues.md`, `tech-debt.md`, `patterns.md`.
3. **Topic inference.** Keywords like `convention`, `style`, `naming`, `column-aligned` → include `conventions/*.md`. Keywords like `decision`, `breaking`, `release` → include `history/decisions/`. Keywords like `bug`, `tech debt`, `legacy` → include `conventions/legacy-patterns.md` and `detected-issues/index.json`.
4. **Always include** `architecture/overview.md` and `architecture/public-api.md` as orientation.
5. **Cap the shortlist** at 12 file paths. If more match, prefer files with higher `confidence` and more recent `last_verified`.

For a question that mentions a specific issue/PR number (`#5414`), include `github/issues-index.json` and `github/prs-index.json` in scope, and set `allowGhFetch: true` so the agent can pull full body.

### 3. Spawn kb-research

Use the `Agent` tool with `subagent_type: "kb-research"`:

```
question: <user's question, verbatim>
scope: [<shortlist paths>]
maxBudget: 30
allowSourceRead: false
allowGhFetch: true
```

Set `allowSourceRead: true` only if:
- The user explicitly said "and verify against current code", or
- The KB's relevant files are all `confidence: low` or > 180 days stale (the agent can't be expected to answer reliably without source).

### 4. Receive answer

The agent returns a markdown answer per its output template (Answer / Detail / Pointers / Caveats).

### 5. Print the answer

Print the agent's answer verbatim. Do not paraphrase or compress.

If the answer's Caveats section flags staleness, append a one-line suggestion at the end:

```
> Tip: KB last refreshed <date>; run /kb-refresh if you need fresher data.
```

### 6. Follow-up loop (optional)

After printing, accept follow-up:

```
> Follow-up question, or 'q' to quit:
```

For follow-ups, re-run from step 2 with the new question (the shortlist is rebuilt — questions can shift area). The follow-up does **not** carry conversational context into the agent — each call is a fresh `kb-research` invocation. The skill can include the previous question + answer summary in the `scope` field as a synthetic `[prior-context]` doc if the user asks something explicitly relative ("and what about X then?").

`q` exits.

## Output discipline

- Never paraphrase the agent's citations. The agent is the source of truth for what the KB says; the skill is a thin pass-through.
- If `kb-research` returns the "I cannot fully answer from the KB" template, surface it directly and offer the user the suggested escape hatches (`/kb-refresh`, `allowSourceRead: true`, narrower scope).
- If multiple follow-ups in a row hit the same "what's missing" gap, suggest the user run `/kb-build --force <step>` to regenerate the relevant artifacts.

## Do not

- Modify any file. Read-only end-to-end.
- Run `/kb-refresh` automatically. The user decides when to refresh.
- Override `allowSourceRead` without the user's go-ahead. The flag is opt-in for a reason.
- Cache answers between runs. Each call is independent — the KB on disk is the cache.
