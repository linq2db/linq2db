# Issue typing & label taxonomy

How `linq2db/linq2db` GitHub issues are typed and labelled, and how to triage them. Read this before bulk-labelling, triaging, or filing issues (pairs with the `create-issue` / `find-issues` / `merge-duplicates` skills). Established during the 2026-06 tracker-cleanup pass.

## Native issue types replaced the `type:` labels

The repo uses **GitHub native issue types** — `Bug`, `Feature`, `Task` (org-level, listed via `gh api orgs/linq2db/issue-types`). The legacy `type: bug` / `type: feature` / `type: improvement` / `type: refactoring` / `type: discussion` / `type: question` **label family was deleted** — do **not** re-add a `type:` label; set the native type instead.

- Set a type: `gh api -X PATCH repos/linq2db/linq2db/issues/<n> -f type=Bug` (or `Feature` / `Task`). Verify with `--jq '.type.name'` (note: raw REST exposes it as `.type`; `gh issue list --json issueType` exposes it as `.issueType`).
- Mapping when migrating old intent → type: defect → **Bug**; new capability / enhancement / "improvement" → **Feature**; internal/maintenance (refactor, test coverage, CI, docs, tech-debt) → **Task**.
- A genuine question / open-ended discussion is **none of these** — see *Triage* below (close or convert to a Discussion, don't force a type).

## Controlled label vocabulary

Only these topic-label families exist; propose labels **only** from this vocabulary (a bulk/LLM step that invents a value like `area: insert` or `area: eager-load` must be caught — see *Bulk apply* below).

- **`area:`** — AOT, async, bulk-copy, C# semantics, compatibility, compiled-query, configuration, data-context, DDL, documentation, efcore, extensions, fluent-mapping, fsharp, hints, infrastructure, inheritance, linq, linqpad, mapping, nuget, performance, remote-context, scaffold, schema, set, sql, T4, tests, types, vb.net
- **`provider:`** — access, clickhouse, db2, duckdb, firebird, informix, mssql, mysql, oracle, postgresql, sap-hana, sqlce, sqlite, sybase, ydb
- **`epic:`** — code-generator, configuration, DDL, eager-load, insert, json_sql, merge, output, **new-provider**
- **workflow families** (managed by maintainers, don't add/remove during topic-labelling): `status: *`, `severity: critical`, `resolution: external`.

### Conventions

- **Format is `prefix: value`** with a space after the colon. Multi-word `epic:`/`area:` values are hyphenated (`epic: code-generator`, `epic: new-provider`), matching the existing scheme.
- **`provider:` is only for *supported* providers.** A request to add support for an unsupported/different DB (e.g. SAP SQL Anywhere ≠ the supported Sybase ASE; Redshift; Ingres) gets **`epic: new-provider`**, *not* a `provider:` label — applying `provider: sybase` to a SQL-Anywhere request wrongly implies it concerns the shipped provider.
- **`epic: new-provider`** groups all "add support for DB X" requests; the long-running RDBMS-request poll (#1014) is its anchor.
- Do **not** create new colors off-palette: providers `#336791`, epics `#0010c9`, areas `#bfdadc`.

## Triage — reclassify over close

When triaging issues that look like questions / "not actionable", **prefer reclassifying to a native type over closing**:

- A latent capability gap behind a how-to question → keep open as **Feature** (a usage question can still be a real feature ask; e.g. "how do I X?" where X isn't supported).
- A real defect surfaced in the thread (even if the reporter found a workaround, or it's labelled "answered") → keep open as **Bug** — a workaround fixes the user, not the bug.
- **Close only** when: confirmed **external** (not a linq2db defect — e.g. a `Microsoft.Data.SqlClient` bug → `not planned`), **verified fixed in a shipped release** (cite the PR/commit → `completed`), or a **duplicate** (→ `duplicate`, via `merge-duplicates`).
- A genuine open-ended discussion / poll / idea → **convert to a GitHub Discussion** (UI-only — no API) rather than closing; leave untyped.

"Already-fixed" verdicts get extra scrutiny — see [`bug-investigation.md`](bug-investigation.md) → *"Already-fixed" close-candidates*.

## Bulk apply

When applying labels/types across many issues from a fan-out/LLM proposal:

- **Validate every proposed value against the live vocabulary before writing.** LLM classification steps hallucinate plausible-but-nonexistent labels (`area: insert`, `area: eager-load` both occurred); `gh issue edit --add-label` on a nonexistent label errors. Build a valid-set guard and drop + report anything outside it.
- **A proposal of "missing X" may already be present** — the add-only set excludes existing labels, so verify current labels before flagging a gap (several "missing provider" flags were already-present).
- **Re-verify open/closed state immediately before acting on a stale candidate list** — issues get closed mid-session; a list built minutes ago goes stale (#1480/#2528 were closed by someone else between building the list and reviewing it).
- Set types via `gh api -X PATCH … -f type=…`; add/remove labels via `gh issue edit <n> --add-label … --remove-label …`; manage label *definitions* via `gh label create/edit/delete --yes`. Wrap multi-issue loops in a `.build/.claude/*.ps1` script (one allowlist match, not N).
