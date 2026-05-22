# issue-search.md

Shared discipline for searching `linq2db/linq2db` issues and PRs. Referenced by
`/find-issues`, `/merge-duplicates`, and `/create-issue` (pre-file duplicate check).

## Input

One of:
- **Topic text** — free-form description.
- **Ticket content** — title + first ~1000 chars of an issue or PR body, obtained via
  `gh issue view <n> --repo linq2db/linq2db --json number,title,body,labels,author`.

## Step 1 — Extract search terms

Pull 2–4 distinctive keywords from the input. Prefer:

- **Specific identifiers** — type/method names (`SqlServerSqlBuilder`,
  `BuildDropTableStatement`), error codes (`ORA-00942`, `SQLSTATE 42S02`), feature flags
  (`HasDropIfExists`).
- **Version-specific dialect terms** — `Firebird 5`, `Oracle 23c`, `SQL Server 2016`.
- **Feature keywords** — `IF EXISTS`, `DROP TABLE`, `MERGE`, `OUTPUT`, `CTE`.

Avoid:
- Generic words: `issue`, `problem`, `doesn't work`, `bug`.
- Stop words: `the`, `a`, `in`, `with`.
- Vendor names alone (`oracle`) without a distinctive companion term — too many matches.

Present the extracted set to the user before searching:

> Search terms: `<t1>`, `<t2>`, `<t3>` — adjust or go?

## Step 2 — Run parallel search strategies

Fire in parallel (one `gh` call each). `--repo linq2db/linq2db` is always set.

### A — Issues, title+body (broad)

```
gh issue list --repo linq2db/linq2db --search "<terms>" \
  --state all --limit 15 \
  --json number,title,state,updatedAt,url,labels,author,comments
```

### B — Issues, title-only (tight)

```
gh issue list --repo linq2db/linq2db --search "<terms> in:title" \
  --state all --limit 10 \
  --json number,title,state,updatedAt,url,labels,comments
```

### C — PRs, title+body

```
gh pr list --repo linq2db/linq2db --search "<terms>" \
  --state all --limit 10 \
  --json number,title,state,updatedAt,url,labels
```

### D — Label-filtered (ticket mode only, if target has relevant labels)

When the input is a ticket carrying `provider: *` or `area: *` labels, run one extra
pass per label to narrow the topic space:

```
gh issue list --repo linq2db/linq2db \
  --search "<terms> label:\"provider: firebird\"" \
  --state all --limit 10 \
  --json number,title,state,updatedAt,url,labels,comments
```

Skip `type: *`, `status: *`, `severity: *`, `epic: *`, `resolution: *` — those don't
narrow the topic space and bury results.

## Step 3 — Deduplicate and rank

Merge results from A–D (the same issue may appear in multiple). Rank by:

1. **Title match strength** — title contains all terms > some > body-only hit.
2. **State** — `open` > `closed`.
3. **Recency** — higher `updatedAt` wins.
4. **Discussion volume** — higher `comments` count wins on ties.

Cap at top 10.

## Step 4 — Exclude self (ticket mode)

If the input was a ticket with number `N`, drop any result where `number == N` via
post-filter. GitHub search's `-<n>` exclusion is unreliable for issue numbers.

## Step 5 — Present

Compact table in rank order:

```
kind | #     | state  | title (≤50 chars)                          | labels                       | updated     | cmts | URL
-----+-------+--------+--------------------------------------------+------------------------------+-------------+------+----------------------------
iss  | 5483  | open   | Firebird 5: use native DROP TABLE IF …     | provider: firebird, area:DDL | 2026-04-22  | 0    | https://.../issues/5483
pr   | 5479  | open   | Review tooling: further improvements       | —                            | 2026-04-22  | 2    | https://.../pull/5479
```

Keep columns narrow. Truncate title at ~50 chars. `kind` column distinguishes issues
(`iss`) from PRs (`pr`).

## Known limitation

GitHub's **"similar issues" ML feature** (the UI's new-issue auto-suggestions) is not
exposed via API. Search uses GitHub's keyword syntax (`repo:`, `in:title`, `label:`,
etc.) — lenient but not semantic. Bad term extraction → bad results. Step 1 is
load-bearing.
