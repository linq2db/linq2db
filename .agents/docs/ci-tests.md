# Running tests on CI (Azure Pipelines)

linq2db's CI is Azure Pipelines, triggered from a PR via `/azp` comments posted on the PR itself. A CI run gives you two things a local run can't:

1. **Coverage across providers / platforms the developer doesn't have locally** — the CI matrix includes DB2, Informix, SAP HANA, SAP ASE, and other heavy providers most contributors can't stand up.
2. **Baselines regeneration in the remote `linq2db.baselines` repo** — successful CI runs commit regenerated baselines back to the upstream baselines repo, so later PRs diff against an up-to-date "before" state. A good reason to run CI even when the local run already passed.

## Commands

Posted as a plain comment on the PR (not a review comment):

| Comment | Effect |
|---|---|
| `/azp list` | Lists every pipeline registered on the repo (workflow names, status). Use first if you don't remember the exact `test-<name>` you want. |
| `/azp run test-all` | Runs the full provider matrix. Usually the right call after a PR first opens. |
| `/azp run test-<dbname>` | Runs a single-provider pipeline (e.g. `test-sqlite`, `test-sqlserver`, `test-firebird`). `/azp list` has the canonical names. |

The canonical `test-<name>` pipeline names are also enumerated in [`Build/Azure/pipelines/testing.yml`](../../Build/Azure/pipelines/testing.yml) — its `Build.DefinitionName` switch maps each to a `db_filter` (`test-access`, `test-db2`, `test-firebird`, `test-informix`, `test-mysql`, `test-oracle`, `test-postgresql`, `test-saphana`, `test-sqlce`, `test-sqlite`, `test-sqlserver`, `test-sqlserver-2019`, `test-sqlserver-2022`, `test-sybase`, `test-clickhouse`, `test-duckdb`, `test-metrics`). Read it to resolve a provider's pipeline name without spending a `/azp list` round-trip.

Posting the comment requires write access to the repo; for contributors without write access, a maintainer posts on their behalf.

## When to propose a CI run

- **After `gh pr create` (new PR)** — propose once, default to `/azp run test-all`. Skip if the PR is draft and the user has said they want more local iteration before inviting CI attention.
- **After pushing new commits that move SQL emission on an active PR** — the prior CI baselines are stale; offer a follow-up run (`/azp run test-<affected-provider>` is usually enough). **Note that baseline updates land incrementally per provider config** — a recent commit on `baselines/pr_<n>` doesn't mean every file is current. To check whether a specific baseline file reflects the latest fix, `git -C ../linq2db.baselines log -1 --format='%H %ai %s' -- <path-to-baseline>`; if the timestamp predates the fix push, that file is stale and the corresponding CI provider job hasn't run yet.
- **When the user asks "does this pass on X?"** for a provider that isn't set up locally — propose `/azp run test-<X>` instead of spinning the provider up.

One `/azp run` per meaningful change. Do not spam — each run consumes CI capacity.

## Posting the comment

`/azp` trigger lines start with `/`, which Git Bash silently path-mangles when passed via `gh … --body "/…"` — the comment posts successfully with a `C:/Program Files/Git/azp …` body and no error from `gh`. See [`agent-rules.md`](agent-rules.md) → **Windows Git Bash gotchas** for the full gotcha. Use `--body-file -` with a stdin heredoc so the leading slash survives:

```
gh pr comment <N> --repo linq2db/linq2db --body-file - <<'BODY'
/azp run test-all
BODY
```

Keep the body to the `/azp …` line alone — Azure Pipelines only parses that line, and extra text can suppress the trigger. After posting, verify with `gh api repos/linq2db/linq2db/issues/comments/<id> --jq '.body'` — the mangling is invisible from `gh pr comment`'s stdout, so the verify is the only way to catch it.

Posting is publicly visible and incurs CI cost, so follow the standard confirmation rules in [`agent-rules.md`](agent-rules.md): propose the comment, wait for explicit user approval, then post. For new PRs, the approval can come bundled with the `gh pr create` approval — e.g. "create the PR and run test-all".

## Reading failed CI test runs

**Find *which* pipeline failed with `gh pr checks <n>`, not `gh api commits/<headSha>/check-runs`.** The auto-triggered `build` pipeline (and any pipeline Azure runs against the PR *merge* commit) attaches its check to that merge SHA — a different commit than the PR head — so `gh api repos/linq2db/linq2db/commits/<headSha>/check-runs` can silently omit it. Seen on #5703: the head's `check-runs` listed only `test-all` (all green) while `gh pr checks 5703` showed `build	fail` (buildId 22173, sourceVersion = the Azure merge commit). Treat `gh pr checks <n>` as the authoritative "is CI failing?" list; use the `check-runs` API only to resolve a *specific* named check's `buildId`.

When a CI build fails, the per-task error messages aren't in the GitHub check-runs annotations — they're inside the Azure DevOps build logs. The `dev.azure.com/linq2db` build API is publicly readable (no auth), but the hand-flow is fiddly: hit `/timeline?api-version=7.0` for the JSON list of failed `Task` records, then `/logs/<id>` for each one's raw log, then regex for `Failed <TestName>... Error Message:` blocks.

Use [`.agents/scripts/azp-build-failures.ps1`](../scripts/azp-build-failures.ps1) instead — it does the timeline + parallel log fetch + per-failure parse in one call:

```
pwsh -NoProfile -File .agents/scripts/azp-build-failures.ps1 -BuildId <n>
```

Output: JSON with `{ buildId, logsDir, failedTaskCount, tasks: [{ name, logUrl, logPath, failures: [{ test, errorMessage }] }] }`. Logs persist under `.build/.agents/azp-<n>/` for follow-up `Read` / `Grep`. **Prefer the script's parsed `failures[]` over hand-grepping those raw logs** — a bare `TestName("Provider…")` grep also matches the log's diagnostic / progress lines (lane diagnostics, timing, retry echoes), inflating the failing set (once turned PG failures on 9.2–10 into a false "all 9.2–12 fail"). If you must grep the raw log, anchor on the runner's `failed` marker (`failed.*TestName`) and dedupe. When the build is red for a **non-test** reason (compile error in a `Build …` step, restore failure, or a `Command line` step wrapping `dotnet build`/`publish`), `failedTaskCount` is `0` and a `buildFailures: [{ name, issues, logPath, errors }]` array carries the failure — don't read `failedTaskCount: 0` as "nothing failed". Note the timeline `issues` are often only a generic wrapper (`Cmd.exe exited with code '1'`) for `Command line` steps; the real `CSxxxx`/`MAxxxx`/`MSBxxxx` message is in the fetched task log (`logPath`) and parsed into `errors[]`, so read those, not just `issues`. One recurring non-test red is the **`Publish to Azure Artifacts feed`** step failing with HTTP **402 (Payment Required — artifact quota / billing)**: it's pure infra, unrelated to the code, and shows up in `buildFailures[]` — don't read a build that's red *only* for this as broken tests (it bit a green master build at build 21987).

To read a **specific step's log by name** — including a *succeeded* diagnostic step whose value is its stdout (a CI probe printing a summary block, a setup step's timing), which `azp-build-failures.ps1` never surfaces — use [`.agents/scripts/azp-step-log.ps1`](../scripts/azp-step-log.ps1) rather than hand-running the `curl …/timeline | python`-to-find-the-log-id-then-`curl …/logs/<id>` dance:

```
pwsh -NoProfile -File .agents/scripts/azp-step-log.ps1 -BuildId <n> -StepName '<name-substring>'
```

Output: JSON `{ buildId, stepName, logsDir, steps: [{ name, state, result, logPath }] }`; `-StepName` is a case-insensitive substring (matches all Task records containing it). A matched-but-pending step reports `logPath: null` (no log until it starts) rather than failing. `Read` / `Grep` the persisted `logPath`.

## Is a CI failure PR-introduced or pre-existing?

To attribute a PR's failing jobs without a local build, compare the **master** Azure build whose `sourceVersion` equals the PR's merge-base. Get the merge-base with `git merge-base origin/master origin/<branch>`, list recent master builds (`…/_apis/build/builds?branchName=refs/heads/master&$top=8`), find the build whose `sourceVersion` matches, and run `azp-build-failures.ps1` on it. If master-at-merge-base is test-green, every failure on the PR is PR-introduced. This is the CI-side analogue of the local-worktree merge-base comparison in [`bug-investigation.md`](bug-investigation.md) → *Behavior-preserving refactor* — cheaper (no checkout/build, just two parsed build results), and it settled that all of PR #5485's Firebird failures were the PR's own (master build 21987 at the merge-base was clean).

**Caveat — confirm the comparison build actually ran the test matrix, and compare like-for-like.** Master-*push* builds run the `build`+publish pipeline only, **not** the provider test matrix (`azp-build-failures.ps1` reports `failedTaskCount: 0`, `tasks: []`, red only on the `Publish to Azure Artifacts feed` 402 step). Such a build carries **no** test signal — you cannot read master's test health from it, and a red master push build is usually just the 402, not broken tests. `test-all` runs only when triggered by `/azp run test-all` (a PR comment). So: to attribute a PR's `test-all` failure, compare against **another PR's `test-all` run whose merge commit shares the same master base** (check the `refs/pull/<n>/merge` parent SHA — `git log --format=%p -1 <mergeSha>`), not against a master push build and **never** against a PR that only ran `build` (its `gh pr checks` shows no `Tests:` legs — a build-only run says nothing about tests). Comparing a `test-all` failure to a build-only run is invalid. (Corrected after a session mis-attributed #5704's all-provider cache failures to a master commit by diffing against #5708, which had run build-only; the real cause was in #5704 itself.)

Resolve `<n>` (the Azure DevOps build ID) from the PR's check-runs:

```
gh api repos/linq2db/linq2db/commits/<headSha>/check-runs --jq '.check_runs[] | select(.name == "test-all") | .details_url'
```

The `details_url` ends in `buildId=<n>`.
