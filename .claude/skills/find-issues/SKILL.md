---
name: find-issues
description: Search linq2db/linq2db for issues and PRs matching a topic or the content of an existing ticket. Read-only. Topic mode takes a free-form query and returns ranked candidates. Ticket mode takes an issue/PR number or URL, derives search terms from its title/body/labels, excludes self, and returns candidates with a duplicate verdict. In ticket mode when the verdict is "likely duplicate", suggests /merge-duplicates as follow-up.
---

# find-issues

User-triggered read-only lookup of existing issues and PRs on `linq2db/linq2db`.

Two modes:
- **Topic mode** — user provides a free-form description (e.g. "FB5 IF EXISTS support", "DuckDB provider").
- **Ticket mode** — user provides an issue or PR number/URL; skill derives search context from its title, body, and labels, then excludes the target from results.

Write actions (closing, merging, commenting) are **out of scope** — see `/merge-duplicates` for that.

## When to run

Only when the user explicitly invokes this skill. Typical prompts:
- "search for issues about X"
- "find duplicates of #N"
- "is this a dupe of anything?"

## Steps

### 1. Classify input

| Input | Mode |
|---|---|
| All digits (`5483`) or `#5483` | ticket (issue number) |
| `https://github.com/<owner>/<repo>/(issues\|pull)/<n>` | ticket (URL — parse owner/repo/n) |
| Anything else | topic |

If a URL's `<owner>/<repo>` ≠ `linq2db/linq2db`, flag the mismatch and confirm with user before proceeding.

Ambiguity (input is a single word that could be a topic or a typoed number): ask.

### 2. Ticket mode — fetch target

```
gh issue view <n> --repo linq2db/linq2db \
  --json number,title,body,labels,state,author,url,closedAt
```

Note: this endpoint works for PRs too (GitHub models PRs as issues at the API level), but if the URL is `/pull/<n>`, use `gh pr view <n>` instead to also capture `isDraft`, `baseRefName`, `headRefName`, and closing-issue references (`Fixes #…`, `Closes #…`). Surface any closing references in the output — a PR with `Fixes #5412` isn't a dupe of #5412, it's the fix.

Truncate body to ~1000 chars for term extraction.

### 3. Extract search terms and run searches

Follow the discipline in [`.claude/docs/issue-search.md`](../../docs/issue-search.md):
- Term extraction (step 1 of that doc)
- Confirm terms with user
- Parallel search strategies A–C (topic mode) or A–D (ticket mode with labels)
- Dedup + rank (cap 10)
- Self-exclude (ticket mode)

Use parallel `Bash` calls for the independent `gh` invocations.

### 4. Present results

Compact table per `issue-search.md` step 5.

Then, depending on mode:

**Topic mode** — just the table plus:

> Want to file a new issue covering this? → `/create-issue`

**Ticket mode** — the table plus a verdict:

| Verdict | When |
|---|---|
| **likely duplicate** | At least one open issue with ≥2 matching terms in title, same provider/area labels if present, and substantive body |
| **related but distinct** | Candidates share keywords but title/scope diverges (e.g. "SQL Server IDENTITY bug" vs "SQL Server IDENTITY in TPT mapping") |
| **no duplicates found** | No candidates survive ranking, or all candidates are clearly unrelated |

If verdict is **likely duplicate**:

> Looks like duplicate of #<canonical>. Run `/merge-duplicates <canonical> <target>` to consolidate and close.

The skill does NOT invoke `/merge-duplicates` itself — the user does.

### 5. Interactive follow-ups

After presenting results, accept:
- `show <n>` — fetch full body and comments of candidate `<n>`:
  ```
  gh issue view <n> --repo linq2db/linq2db --json body,comments
  ```
- `refine <new terms>` — re-run step 3 with adjusted terms
- `done` — exit

### 6. Do not

- Post, edit, or close any issue or comment. This skill is read-only.
- Search repos other than `linq2db/linq2db` unless the user's ticket URL explicitly names a different one and they've confirmed.
- Invent a verdict when candidates are ambiguous — say "uncertain" and list evidence.
