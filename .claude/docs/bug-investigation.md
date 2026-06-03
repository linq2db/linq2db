# Bug investigation & fixing

Situational rules for reproducing, scoping, and fixing reported bugs / regressions. The one-line triggers live in [`agent-rules.md`](agent-rules.md) → *Investigating & fixing bugs*; this doc holds the detail and the war-stories that justify each rule. Read it when one of those triggers fires.

## "Fix or disable" cleanup issues — attempt the fix first

When an issue offers a choice — *fix the sites, or disable / suppress the rule* (analyzer-rule cleanups, lint-debt batches like #5532) — do **not** open with a disable-everything plan. Work rule-by-rule: attempt the genuine fix, and reach disable/suppress only after *demonstrating* the fix is infeasible — breaking public/interface API, the needed API absent on a target TFM, the only available fix a behavioral no-op / analyzer appeasement, or every current site a false positive. Put that evidence in the per-rule proposal. Disable is the documented fallback, not the lead. (Corrected mid-#5532, after an opening plan proposed disabling 9 of 10 rules.)

Analyzer-rule mechanics — `.editorconfig` layout, full-solution verification, site enumeration, bulk code-fix application — live in [`analyzer-rules.md`](analyzer-rules.md).

## "Regression after switching package X→Y" reports

When a bug report blames a package swap ("worked with package X, broke after switching to Y"), verify the named package actually contains the relevant code before treating the swap as the cause. Many linq2db packages are thin satellites — e.g. `linq2db.Extensions` and the older `linq2db.AspNet` are DI / logging only, with zero query-translation code. A translation / SQL-generation "regression" that coincides with such a swap is almost always a **core `linq2db` version change** that rode along with it (often a major-version upgrade), not the package itself. Pin the actual core version on both sides before reproducing — and reproduce against that core version, not just current master (#5560 was reported on a swap to `linq2db.Extensions` but the real variable was the v5→v6 core upgrade; it didn't reproduce on 6.3.0 or master at all).

## Reproducing a reported regression — confirm HEAD contains the change first

Before building a repro for a bug attributed to a specific merged PR/commit, confirm the working tree actually contains that change. A long-lived branch (e.g. `infra/*`) can sit weeks behind `origin/master`, so a regression introduced by a recent PR won't reproduce locally and you'll burn time chasing a ghost — building elaborate offline repros against code that predates the bug. Check with `git merge-base --is-ancestor <pr-merge-sha> HEAD` (exit 0 = present), or compare `git log -1 --format=%ci HEAD` against the PR's merge date. If HEAD predates the change, create the fix branch from fresh `origin/master` first (per [`agent-rules.md`](agent-rules.md) → *Creating a new branch*), then reproduce there. #5528 (a regression from #5504) was invisible for a long stretch because the working branch's base predated #5504 entirely.

## Enabling an `[ActiveIssue]` test after the issue closes

The `/enable-disabled-test` skill drives this end to end; this section is the rationale and the manual fallback.

When a closed issue's `[ActiveIssue]`-gated regression test is being enabled, do **not** trust the close-comment attribution alone (e.g. *"fixed by #X and #Y"*). Bisect to identify the PR that actually flipped the test from fail to pass, and cite that PR in the enable-PR's body. Reporter comments often cite *issues* that share the symptom, not the *PR* that fixed the specific test scenario — the load-bearing fix may be a different PR that landed earlier or later and addressed a broader code path. Concretely:

1. Verify the cited PRs exist (close-comment numbers may be issues, not PRs) and resolve issue→PR via `gh api repos/<o>/<r>/issues/<n>/timeline --jq '.[] | select(.event == "cross-referenced") | .source'` or the GraphQL form in [`pr-resolver.md`](pr-resolver.md).
2. In a worktree (`git worktree add ../<repo>.bisect-<issue> --detach <ref>`), bisect with the test's `[ActiveIssue]` removed locally — typically `git switch --detach <ref>` → edit out the attribute → `dotnet test ... --filter <name> -c Debug` → record pass/fail → repeat.
3. Bound the range first (head/master = pass, last release before close = fail), then halve. Each step is a few seconds of build + test.
4. The PR whose merge commit transitions fail → pass is the citation. Mention any reporter-cited related PRs as context, not as the cause.

Bisecting a small range (≤ 50 commits) costs a minute or two and saves re-reviewing a wrong attribution.
