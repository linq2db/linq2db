# Known unknowns to resolve on first release run

The release skill set was built from documentation, not from running an actual release end-to-end. This file lists every spot where a sub-skill is going to ask the user for first-run details. Each item lists the **owning skill / step**, the **question to ask**, and the **doc to update** when answered.

When the first real release surfaces an answer, update the corresponding doc and tick the row here. A `/session-reflect` pass at end-of-release should drain this list.

## Open questions

### T4 build prerequisite (skill: /release-test-matrix, step 4.0)

- **Question:** exact command to build the solution in Debug targeting net462 so `.build/` contains the binaries T4 templates consume.
- **Candidate:** `dotnet build linq2db.slnx -c Debug` (slnx-level multi-TFM build emits net462 outputs alongside others). `dotnet build … -f net462` is rejected by slnx; per-project would be needed if a single-TFM build is wanted.
- **Doc to update:** [`linqpad-test-checklist.md`](./linqpad-test-checklist.md) → **T4 build prerequisite** section.

### Per-provider DB init invocations (skill: /release-test-matrix, step 4.1)

- **Question:** for each provider being tested, the exact `Data\Setup Scripts\` invocation (file + args + env vars).
- **Doc to update:** [`provider-db-init.md`](./provider-db-init.md), one entry per provider.

### Local NuGet test feed (skill: /release-test-matrix, step 4.4)

- **Question:** path to the user's local NuGet server folder (where copying `.nupkg` files makes them available for LINQPad 7+ install). Per-user; not committed.
- **Doc to update:** [`external-repos.md`](./external-repos.md) → **User-specific paths**.

### Docs PR submodule sync step (skill: /release-postpublish, step 2)

- **Question:** in the `linq2db.docs` repo, what's the exact step to "synchronize submodules" — `git submodule update --remote`? With or without `--recursive`? Which submodule pins to the release tag — the `linq2db` submodule itself, others?
- **Doc to update:** new section in [`overview.md`](./overview.md) (or carve out `docs-pr-flow.md` once steps stabilize across multiple releases).

### GitHub release `gh release create` exact form (skill: /release-postpublish, step 3)

- **Question:** exact `gh release create` invocation that:
  - Tags the release commit on `release` branch (tag form `<ver>` or `v<ver>` — confirm on first run via `gh release list --repo linq2db/linq2db --limit 5`, per the skill).
  - Uses `--generate-notes` so the **New Contributors** auto-section appears.
  - Attaches only the `.lpx` artifact (no `.lpx6`, no `.nupkg`).
  - Body is a terse summary keyed off the full release notes on wiki.
- **Doc to update:** new section in [`overview.md`](./overview.md), or carve out `github-release.md`.

### Baselines repo anchor commit verification (skill: /release-publish, step 3)

- **Question:** is `f6b4f6278e5e53f38b6a26350f80b0609b37e86e` still the right reset target for the baselines repo before each release, or does this evolve as the baselines repo gets re-anchored?
- **Doc to update:** [`overview.md`](./overview.md) (Phases diagram references this anchor) and the publish skill itself if the value moves.

### Ad-hoc task dispatch (skill: /release, step 4 task type 6.x)

- **Question:** what's the right pattern for executing a user-supplied ad-hoc task (e.g. "audit IDataProvider surface")? Direct conversation, dispatch to `/review-pr` against a specific file, dispatch to `find-issues`, ...
- **Doc to update:** new section in `/release` skill once a pattern stabilizes.

## Resolved

<!-- moved from "Open questions" above as each first-run answer comes in -->
