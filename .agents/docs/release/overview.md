# Release workflow — overview

linq2db's release procedure, codified across a set of cooperating skills under `.agents/skills/release-*`.

## Phases

```
prep                publish              postpublish
─────               ───────              ───────────
0 Branch +          1 Open release PR    1 Verify nuget publish
  version bump        (master → release)
1 Deps              2 Triage stale       2 Docs PR (linq2db.docs)
                      baselines PRs
2 PublicAPI         3 Reset baselines    3 GitHub release + tag
                      master               (.lpx artifact)
3 Milestone         4 Prerelease nuget   4 Next-version bump PR
  check               team-test gate       + new milestone
                                           + linq2db.t4models
                                             re-pin
4 Test matrix       5 Merge release PR
5 Release notes     6 Merge baselines
                      PR (CI-generated)
6 Final verify      7 Copy + tag
  (build + analyzer    baselines on
   catch-up +          releases branch
   profile-analyzers
   + api-baselines)
+ ad-hoc 7.x
```

## Skills

- [`release`](../../skills/release/SKILL.md) — orchestrator.
- [`release-deps`](../../skills/release-deps/SKILL.md) — NuGet dependency update.
- [`release-publicapi`](../../skills/release-publicapi/SKILL.md) — PublicAPI.Shipped/Unshipped reconciliation.
- [`release-milestone-check`](../../skills/release-milestone-check/SKILL.md) — open issues/PRs/baselines audit.
- [`release-test-matrix`](../../skills/release-test-matrix/SKILL.md) — LINQPad / T4 / NuGet-T4 / CLI testing guide.
- [`release-notes-validate`](../../skills/release-notes-validate/SKILL.md) — release-notes coverage diff.
- [`release-verify`](../../skills/release-verify/SKILL.md) — final verification (build + analyzer catch-up + profile-analyzers + api-baselines refresh).
- [`release-publish`](../../skills/release-publish/SKILL.md) — post-prep-merge publish flow.
- [`release-postpublish`](../../skills/release-postpublish/SKILL.md) — post-tag verification + next-version bump.

Reused as-is from outside this set:

- [`version-bump`](../../skills/version-bump/SKILL.md) — Directory.Build.props version edits.
- [`api-baselines`](../../skills/api-baselines/SKILL.md) — CompatibilitySuppressions.xml regeneration (**distinct** from PublicAPI.Shipped/Unshipped — different file family, different tool). Invoked by `/release-verify` step 4.
- [`profile-analyzers`](../../skills/profile-analyzers/SKILL.md) — Roslyn analyzer build-time profiling. Invoked by `/release-verify` step 1 when an analyzer package was bumped this release.
- [`test`](../../skills/test/SKILL.md), [`test-providers`](../../skills/test-providers/SKILL.md), [`review-pr`](../../skills/review-pr/SKILL.md).

## State

Two stores, PR body wins on conflict:

- **Canonical:** the release prep PR body — two-level markdown checklist.
- **Session cache:** `.build/.agents/release-<version>.json` — gitignored.

State script: [`release-state.ps1`](../../scripts/release-state.ps1).

### `PublicAPI.Shipped` / `PublicAPI.Unshipped` state caveat

`Source/**/PublicAPI.Shipped.txt` reflects the **last released** public API surface; `Source/**/PublicAPI.Unshipped.txt` accumulates pending additions from PRs merged during the current dev cycle. The Unshipped file is only **complete** after `/release-publicapi` (task 2) runs its Release build — that's when `RS0016` / `RS0017` diagnostics surface any public API not yet registered, and the user fixes them via IDE quick-fix.

Implication: any audit that needs to compare "previous-release surface" vs "about-to-ship surface" (e.g. `linq2db4iSeries` provider-API drift check; downstream-consumer breaking-change report) must run **after task 2**, not in parallel with deps. Sub-skills that produce such audits should defer them to `/release-verify` (task 6) or invoke them only after task 2 has closed.

Surfaced 2026-05-15 on /release-deps for 6.3.0: an iSeries drift check based on the in-cycle Unshipped content was challenged by the user with "but publicapi files were not updated yet — how can you judge?" — verdict was correct (audit data was incomplete), commit deferred until task 2 settles the file.

## Topic docs (accrued across releases)

- [`branch-and-pr.md`](./branch-and-pr.md) — branch name + PR body conventions.
- [`nuget-package-notes.md`](./nuget-package-notes.md) — per-package update rules + release-notes URLs.
- [`provider-db-init.md`](./provider-db-init.md) — per-provider container/DB-init script invocations.
- [`linqpad-test-checklist.md`](./linqpad-test-checklist.md) — LINQPad smoke + targeted-change rows.
- [`external-repos.md`](./external-repos.md) — sibling clone paths + wiki/template anchors.
- [`first-run-todos.md`](./first-run-todos.md) — known unknowns to ask user on first run.
