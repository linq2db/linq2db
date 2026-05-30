# Auditing prior reviewer claims (bots + humans)

Re-verification procedure for `/review-pr` step 2b and `/verify-review` follow-up passes. The skill loads `pr-context.ps1`'s `reviewThreads[]`; this doc covers what to do with the unresolved-or-other-resolved threads.

## Scope

Re-verify every review thread that is **not** `resolvedBy == currentUser`:

- **Open threads from bot reviewers** (`copilot-pull-request-reviewer[bot]`, Codex, other LLM reviewers) — may have run against an older commit or hallucinated a concern.
- **Open threads from human reviewers** — may have been addressed without anyone closing them.
- **Closed threads `resolvedBy != currentUser`** — the closure may have been premature; the original concern needs re-verification before we accept it.

Skip threads with `resolvedBy == currentUser` — those were deliberate, our own past action.

For each in-scope thread, classify as **Fixed at HEAD** / **Inaccurate at HEAD** / **Still actual** by reading current PR HEAD content. Surface the audit verdict in the review's notes section so the human reviewer can see which prior threads are stale, incorrectly closed, or still actionable.

## Apply `code-reviewer.md`'s rubric when classifying

Each bot claim must clear the same suppression list the subagent enforces — e.g. *"Do not flag `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` drift"* at `code-reviewer.md:46`. Hand-classifying against raw source without consulting the rubric drifts from the subagent's verdict on the same claim. (Surfaced 2026-05-10 on PR #5503: a Copilot PublicAPI claim was initially hand-classified as Still-actual; the documented release-cycle workflow puts it firmly in Inaccurate.)

When classifying Copilot / Codex / other LLM-reviewer threads, apply `code-reviewer.md` rules 1, 4, 5, and 6 explicitly — these are the rules external bots most often outpace the subagent on:

- **Rule 1 / Predicate broadening.** When the bot flags an `is X or Y` / `NeedsConversion`-style condition that catches more inputs than the bug requires, the verdict is **Still actual** unless the code at HEAD genuinely narrows back. Don't dismiss as "preserves intent" or "matches description". Motivating case: PR #5506 (cid 3215654319), a Copilot finding the subagent missed.
- **Rule 4 / Per-provider fan-out.** When the bot flags a translator override missing on one provider (DuckDB / SqlServer2008 / Sybase / Access etc.), check the actual translator file at HEAD against the rubric's fan-out walk before classifying. A missing `Translate<Member>` override on a provider that inherits from a base with a non-trivial registration is **Still actual**.
- **Rule 5 / `PublicAPI.Unshipped.txt` drift.** Always **Inaccurate** per the canonical scope rule.
- **Rule 6 / Substring SQL assertion.** When the bot flags an `assertion contains "X"` that also matches the buggy form, verify the buggy SQL was actually emitted before the PR; if yes, **Still actual**.
- **Framework / stdlib API "removed" claims.** When a bot claims a .NET / pwsh API was removed, changed signature, or no longer works (`-UseBasicParsing`, `File.ReadAllText` BOM, Core-vs-Windows-PowerShell differences), verify empirically (`(Get-Command <cmdlet>).Parameters.ContainsKey('<param>')` + a smoke call) before accepting **Still actual**. Bots are systematically overconfident on Core-vs-WindowsPS API drift and on .NET stdlib auto-conversion behaviors. Surfaced 2026-05-14 on PR #5521: Copilot claimed `-UseBasicParsing` was "removed in PowerShell Core" (still accepted in pwsh 7.6.1; verdict **Inaccurate**); claimed `File.ReadAllText` doesn't strip U+FEFF (does, via auto-detect-BOM default; verdict **Inaccurate**).

- **Source-file encoding / BOM claims.** When a bot claims the PR added or removed a file's UTF-8 BOM (or changed its `charset`), read the cached `_diff/<path>.diff` — the per-file diff preserves the BOM marker in its context lines (a removed BOM shows as `-﻿using …` → `+using …`), so it answers the claim directly. Do **not** `xxd` / `head -c` the cached HEAD body: the cache writer may normalize encoding (stripping the very BOM you're checking for), and the raw byte-pipe prompts on the allowlist. Surfaced 2026-05-31 on PR #5542 (a Copilot BOM-removal claim confirmed Still-actual from the `_diff`, after a redundant `xxd` pass).

The rubric-calibration rules (1, 4, 5, 6) above apply specifically to LLM-reviewer threads (Copilot, Codex, etc.). For human-reviewer threads, re-verify the concern factually against HEAD without applying those bot-specific patterns.

## Verify uncertain claims empirically by playground test

When the static classification is genuinely uncertain — typically when a predicate-broadening / per-provider-coverage / SQL-correctness claim depends on AST behavior that's hard to evaluate by reading the diff — verify the claim by building a minimal repro under `Tests/Tests.Playground/TestTemplate.cs`:

1. Switch to the PR branch (or worktree from it).
2. Replace `TestTemplate.cs` with a self-contained test exercising the claimed shape — define converters / tables / `[Sql.Expression]` helpers inline so the test doesn't depend on `Tests/Linq/*` types.
3. Start the required provider container if needed (`docker start <name>` — per CLAUDE.md scope rules; track via the docker-session-started hook). **Pick the provider by the claim's domain, not by what's already enabled.** A PostgreSQL JSONB claim is verified against PostgreSQL, an Oracle row-predicate claim against Oracle, etc. — using whatever happens to be enabled in `UserDataProviders.json` (e.g. DuckDB or SQLite) is a category error: the verification only carries weight against the provider the bug lives on. Enable the right provider via `/test-providers` first; the extra container-start cost is cheaper than running an irrelevant test and defending it. Provider-agnostic claims (translator heuristics that affect all providers) can still pick the simplest provider, but provider-specific claims must match the domain.
4. `dotnet build Tests/Tests.Playground/Tests.Playground.csproj -c Testing` then `dotnet test … --filter FullyQualifiedName~<TestName>`.
5. Treat the test output (exception type/message, emitted SQL via `((DataConnection)db).LastQuery`, stored vs expected values) as ground truth. The verdict moves from "Still actual per heuristic" to "Still actual confirmed" — or to "Inaccurate, bug doesn't reproduce" if the test passes.
6. If the bug reproduces and a fix lands in the same PR, promote the playground test to a real regression test under `Tests/Linq/...` per the PR's conventions before restoring `TestTemplate.cs` to its template state. Per the playground rule in `agent-rules.md` → *Git commit rules*, playground scratch must not be committed.

This procedure applies to any bot claim that would otherwise be classified as "Still actual but maintainer-acknowledged" or "deferred follow-up" — the empirical confirmation distinguishes "real bug deferred" from "speculative concern dismissed". PR #5506 (2026-05-11) is the motivating case: Copilot flagged a CLR-bool RHS bypassing the converter on a CHAR(1) column; a playground test emitted `SET "Test1" = ("Id" > 0)` and PG returned `22001: value too long for type character(1)`, confirming the bug and producing the fix queued as commit 035474c1.

## Capture Still-actual bot claims the subagent missed

When the audit classifies a thread as Still actual AND the corresponding concern is **not** present in `code-reviewer`'s `findings[]` / `out_of_scope_observations[]` for the same area, append one JSON-line entry to `.build/.claude/review-quality-signal.jsonl` (create the file if absent):

    {"date":"<YYYY-MM-DD>","pr":<n>,"cid":<comment-id>,"author":"<bot-login>","path":"<file>","category":"<B|L|A|D|S|T>","excerpt":"<≤200-char body excerpt>","why_subagent_missed":"<one-line guess: rule-gap / not-yet-codified / call-budget / scope-discipline / other>"}

The file is gitignored (`.build/` is) and serves as the input corpus for periodic rubric tuning — the same shape as `.build/.claude/copilot-vs-review-gap-report.md` produced 2026-05-11. Do **not** surface these entries in the review body or to the user during this run; they are passive accumulation. The `chores` / `audit-claude` skills harvest the log on a separate schedule. (When neither Copilot nor Codex thread on a PR is Still-actual, no entry is written — the absence of the log line is the "review skill caught everything" signal.)

## Thread dispositions

Batched through `post-pr-thread-replies.ps1 -ManifestFile .build/.claude/pr<n>-thread-replies.json`, one allowlisted call:

| Verdict | Thread state | Action | Manifest entry |
|---|---|---|---|
| Fixed at HEAD | open or resolved-by-other | reply `"Fixed in <sha> — <reason>."` + resolve | `{ resolve: true }` |
| Inaccurate at HEAD | open or resolved-by-other | reply `"Inaccurate at HEAD. <correct reading>."` + resolve | `{ resolve: true }` |
| Still actual | **resolved-by-other** | reply `"Reopening — still actual at HEAD. <reason>."` + **unresolve** | `{ unresolve: true }` |
| Still actual | open | leave as-is; feed into the regular finding stream | omit |

The reply+resolve / reply+unresolve happens regardless of whether the parent skill ends up posting a new review draft — the audit can stand alone when there are no fresh findings.

**Ordering when a fresh draft review is also posted.** Run `post-pr-thread-replies.ps1` **before** `post-pr-review.ps1`. The thread-reply REST endpoint (`POST …/comments/{id}/replies`) collides with a freshly-created PENDING draft — GitHub allows one pending review per user per PR, so the reply 422s `user_id can only have one pending review per pull request`. See `review-orchestration.md` → **submit-all mode** for the full rule.

## Re-run on new bot reviews mid-session

Author-pushed CR commits commonly trigger a second Copilot review; treat its claims with the same rigor (classify, reply, resolve) rather than ignoring or batch-dismissing. The same `post-pr-thread-replies.ps1` call handles both passes.

For 2026-05-09 PR #5451: 5 of 7 stale Copilot claims were addressed at HEAD; 2 (the Trim ones) had been inaccurate even when posted (they referenced an intermediate commit later rebased away).

For 2026-05-10 PR #5503: initial pass found 1 Still-actual + 1 Inaccurate-but-pre-existing + 1 partially-actual thread; second-pass review (after the author's CR commit) had 6 more threads — final disposition was 2 Fixed (Precedence + flagSql guard, both landed in the same CR commit), 3 Inaccurate (PublicAPI workflow + 2 Shouldly preference-vs-actual-convention), and 1 Dismissed-as-contrived.
