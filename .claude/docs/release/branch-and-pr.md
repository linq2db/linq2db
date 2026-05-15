# Release branch and PR conventions

## Branch

- **Prep branch:** `release/prepare-<version>` (e.g. `release/prepare-6.3.0`). Branched from `origin/master`. Owns all prep-phase commits (version bump, deps, PublicAPI, ad-hoc tasks).
- **Long-lived release branch:** `release` (not created per release — it's the staging branch that the prep PR's merge eventually flows into via a second PR `master → release` opened in `/release-publish`).
- **Post-tag next-version bump:** `infra/bump-versions` (existing `/version-bump` skill's default). Opened by `/release-postpublish` step 4 with the freshly-released version pinned for `linq2db.t4models`.

Branch naming rules in [`agent-rules.md`](../agent-rules.md) → **Creating a new branch** apply (base from `origin/master`, dirty-tree gate, no silent worktrees).

## PR body checklist

The release prep PR body is the canonical state store. Maintained in-place by [`release-state.ps1`](../../scripts/release-state.ps1) via `gh pr edit <n> --body-file <path>`.

### Structure

```markdown
# Release prep — <version>

<!-- release-state:checklist:start -->
- [ ] 0. Branch + version bump
- [ ] 1. Dependencies update
- [ ] 2. PublicAPI reconciliation
- [ ] 3. Milestone check
- [ ] 4. Test matrix
  - [ ] 4.0 T4 binary prerequisite
  - [ ] 4.1 Provider container + DB init
  - [ ] 4.2 DB2 iSeries decision
  - [ ] 4.3 LINQPad 5 (.lpx)
  - [ ] 4.4 LINQPad 7+ (nugets)
  - [ ] 4.5 NuGet T4 scaffold
  - [ ] 4.6 T4 templates (Tests.T4)
  - [ ] 4.7 CLI scaffold
  - [ ] 4.8 Targeted-change retest
- [ ] 5. Release-notes validation
- [ ] 6. Final verification (build + analyzer catch-up + profile-analyzers + api-baselines)
<!-- release-state:checklist:end -->

## Notes
<free-form text — not parsed by release-state.ps1>
```

Ad-hoc tasks are inserted as `7.1`, `7.2`, … inside the marker block before `<!-- release-state:checklist:end -->`. Task 6 (final verification) must remain the last standard task — `/release` orchestrator gates picking it on every other standard + ad-hoc task being closed.

### Markers

The marker comments `<!-- release-state:checklist:start -->` / `<!-- release-state:checklist:end -->` delimit the auto-managed region. `release-state.ps1 sync-to-pr` only rewrites text **between** the markers; everything else (intro paragraph, Notes section, manual edits) is preserved.

### Status tokens

| Token | Meaning |
|-------|---------|
| `[ ]` | open / not started |
| `[~]` | in progress (sub-skill is mid-flow) |
| `[x]` | done |
| `[-]` | user-skipped |

### Sub-task lines

Sub-tasks (e.g. `4.0`, `4.1`) are nested under their parent with two-space indent.

A parent task is `[x]` when **all** its sub-tasks are `[x]` or `[-]`. A parent task is `[~]` when at least one sub-task is `[x]` and at least one is `[ ]`.

### Sub-line annotations

A sub-line annotation is rendered in parentheses after the label, e.g. `(5 updates, 2 skipped)`. Rendered by `release-state.ps1` from per-task notes in the state JSON.

## PR metadata

- **Title:** `Release prep <version>` (e.g. `Release prep 6.3.0`).
- **Assignee:** `@me`.
- **Milestone:** the release version.
- **Labels:** none required by this skill; project conventions apply.
- **Linked issue:** none required.
- **Draft:** yes, until ready-to-merge gate fires in `/release` step 5.
