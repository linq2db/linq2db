# Release workflow — overview

linq2db's release procedure, codified across a set of cooperating skills under `.claude/skills/release-*`.

## Phases

```
prep            publish              postpublish
─────           ───────              ───────────
0 Branch +      1 Open release PR    1 Verify nuget publish
  version bump    (master → release)
1 Deps          2 Reset baselines    2 Docs PR (linq2db.docs)
                  repo HEAD
2 PublicAPI     3 Merge baselines    3 GitHub release + tag
                  PR (CI-generated)    (.lpx artifact)
3 Milestone     4 Copy + tag         4 Next-version bump PR
  check           release baselines    + new milestone
                                       + linq2db.t4models
                                         re-pin
4 Test matrix   5 Prerelease nuget
                  team-test gate
5 Release       6 Merge release PR
  notes
```

## Skills

- [`release`](../../skills/release/SKILL.md) — orchestrator.
- [`release-deps`](../../skills/release-deps/SKILL.md) — NuGet dependency update.
- [`release-publicapi`](../../skills/release-publicapi/SKILL.md) — PublicAPI.Shipped/Unshipped reconciliation.
- [`release-milestone-check`](../../skills/release-milestone-check/SKILL.md) — open issues/PRs/baselines audit.
- [`release-test-matrix`](../../skills/release-test-matrix/SKILL.md) — LINQPad / T4 / NuGet-T4 / CLI testing guide.
- [`release-notes-validate`](../../skills/release-notes-validate/SKILL.md) — release-notes coverage diff.
- [`release-publish`](../../skills/release-publish/SKILL.md) — post-prep-merge publish flow.
- [`release-postpublish`](../../skills/release-postpublish/SKILL.md) — post-tag verification + next-version bump.

Reused as-is from outside this set:

- [`version-bump`](../../skills/version-bump/SKILL.md) — Directory.Build.props version edits.
- [`api-baselines`](../../skills/api-baselines/SKILL.md) — CompatibilitySuppressions.xml regeneration (**distinct** from PublicAPI.Shipped/Unshipped — different file family, different tool).
- [`test`](../../skills/test/SKILL.md), [`test-providers`](../../skills/test-providers/SKILL.md), [`review-pr`](../../skills/review-pr/SKILL.md).

## State

Two stores, PR body wins on conflict:

- **Canonical:** the release prep PR body — two-level markdown checklist.
- **Session cache:** `.build/.claude/release-<version>.json` — gitignored.

State script: [`release-state.ps1`](../../scripts/release-state.ps1).

## Topic docs (accrued across releases)

- [`branch-and-pr.md`](./branch-and-pr.md) — branch name + PR body conventions.
- [`nuget-package-notes.md`](./nuget-package-notes.md) — per-package update rules + release-notes URLs.
- [`provider-db-init.md`](./provider-db-init.md) — per-provider container/DB-init script invocations.
- [`linqpad-test-checklist.md`](./linqpad-test-checklist.md) — LINQPad smoke + targeted-change rows.
- [`external-repos.md`](./external-repos.md) — sibling clone paths + wiki/template anchors.
- [`first-run-todos.md`](./first-run-todos.md) — known unknowns to ask user on first run.
