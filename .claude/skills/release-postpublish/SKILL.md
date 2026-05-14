---
name: release-postpublish
description: Post-release-merge tasks. Verifies every expected linq2db nuget published to nuget.org at the release version, opens the docs PR in linq2db.docs (submodule sync to the release tag), creates the GitHub release with the .lpx artifact and auto-generated New Contributors section, then opens the next-version bump PR with the new milestone and the `linq2db.t4models` self-reference re-pinned to the just-released version. Each step is its own explicit user-confirmed action. Invoked by `/release` after the release PR merges to the `release` branch.
---

# /release-postpublish

## What this skill is (and isn't)

**Is:** the post-publish wrap-up. Runs after the release PR merges and CI tag-and-publishes nugets. Verifies the publish succeeded, propagates the release to docs + GitHub release page, then bootstraps the next development cycle (new milestone + version bump + `linq2db.t4models` re-pin).

**Isn't:**

- Not for the release-PR merge or the prerelease nuget team-test gate. Those live in [`/release-publish`](../release-publish/SKILL.md).
- Not for editing the release-notes content. That happened in `/release-notes-validate` during prep; this skill consumes the finalized notes for the GitHub release body.
- Doesn't republish nugets — that's CI's job. The skill only **verifies** what CI shipped.

## When to run

- Automatically by `/release` orchestrator when `/release-publish` confirms the release PR merged.
- Manually when resuming a release mid-postpublish phase.

## Required reading

- [`.claude/docs/release/external-repos.md`](../../docs/release/external-repos.md) — linq2db.docs path + GitHub release template anchor (`v6.0.0`).
- [`.claude/docs/release/first-run-todos.md`](../../docs/release/first-run-todos.md) — exact `gh release create` invocation, exact docs PR submodule sync command (first-run-confirmable).

## Phase state

Four ordered steps, recorded in `state.postpublish.steps.<key>`:

| Step | Key | Status flow |
|------|-----|-------------|
| 1. NuGet publish verify | `nuget-verify` | open → done |
| 2. Docs PR | `docs-pr` | open → opened → merged → done |
| 3. GitHub release | `gh-release` | open → done |
| 4. Next-version bump PR | `next-bump` | open → done |

## Procedure

### 1. NuGet publish verification

CI builds and publishes nugets on the release-branch merge. Packages can take **minutes** to appear on nuget.org — re-run as needed; this skill doesn't poll-wait.

Action:
1. Run the verifier:
   ```
   pwsh -NoProfile -File .claude/scripts/release-nuget-verify.ps1 -Action verify -Version <ver>
   ```
   The script:
   - Discovers packable projects under `Source/` and `NuGet/` — every csproj that isn't `<IsPackable>false</IsPackable>`. Dedupes by `<PackageId>` (or filename fallback) — EF variants share an ID.
   - Parallel-queries `https://api.nuget.org/v3-flatcontainer/<id>/index.json` (listed versions only).
   - Reports per-package: `published` (target version found), `latestListed` (most recent listed version), `fetchStatus`.
2. Render results:
   ```
   #  Package                              Target   Status     Latest listed
   1  linq2db                              6.3.0    ✓ published 6.3.0
   2  linq2db.SqlServer                    6.3.0    ✓ published 6.3.0
   3  linq2db.t4models                     6.3.0    ✗ missing   6.2.0
   ...
   ```
3. **Missing rows:** ask user: `re-run` (after waiting, packages may now be live) / `wait` (postpublish stays open) / `escalate` (something's broken — CI may have failed publishing; investigate before continuing).
4. Mark `done` only when **every** expected package is `✓ published`. Don't tick step 1 with missing rows.

**If the discovery list looks wrong** (a package the user expects is missing from the table, or an unpackable project is included): the script's heuristic (csproj `<IsPackable>` + `<PackageId>` element) can be off for niche cases. Add the missing id via `-ExtraIds linq2db.foo` and re-run; or fix the csproj heuristic detection in the script if the case is general.

### 2. Docs PR (linq2db.docs)

The docs site is built from a separate repo `linq2db.docs` (not `linq2db.github.io` — that's the published site, CI-updated from `linq2db.docs`).

Action:
1. Verify clone exists at the path recorded in `external-repos.md` (default `C:\GitHub\linq2db.docs`). If missing, ask user for the correct path; record + session-reload.
2. **First run:** ask user for the exact submodule sync command. Most likely:
   ```
   git -C <docs-path> submodule update --init --remote
   ```
   or with `--recursive` if submodules nest. Record in `first-run-todos.md` → resolved section under **Docs PR submodule sync step**.
3. Create branch + sync + commit:
   ```
   git -C <docs-path> fetch origin
   git -C <docs-path> switch -c docs/release-<ver> origin/master
   git -C <docs-path> submodule update --remote      # or the confirmed variant
   git -C <docs-path> add .            # only after surfacing the diff for user review
   git -C <docs-path> commit -m "Sync to linq2db <ver>"
   git -C <docs-path> push -u origin docs/release-<ver>
   ```
   Each git mutation is confirmed before run (per `agent-rules.md` → Git commit rules / Push to remote rules). No other repo changes besides submodule sync.
4. Create the PR (target: `master` on the docs repo). Title: `Docs: linq2db <ver>`. Body: bullet list of new public-API doc URLs that should be live post-merge.
5. Wait for CI to pass. Ask user to merge (or do `gh pr merge --merge` themselves).
6. After merge, verify a known new API doc URL resolves on the published site. **First run:** ask the user for a known-good URL pattern (e.g. `https://linq2db.github.io/api/LinqToDB.<new-type>.html`); record in `external-repos.md` → docs-site verification.
7. Mark step `done`.

### 3. GitHub release

Use the v6.0.0 template anchor (from `external-repos.md`). The `--generate-notes` flag is critical for the "New Contributors" auto-section — releases without it are missing that section.

Action:
1. Pull the `.lpx` artifact from the release-build pipeline.
   - **First run:** ask the user for the exact artifact path / download command (CI build URL → Artifacts → linq2db.LINQPad.lpx). Record in `first-run-todos.md` → resolved section under **GitHub release artifact path**.
   - Download to `.build/.claude/release-<ver>-artifacts/linq2db.LINQPad.lpx`.
2. Draft the release body. Short terse version of the wiki release notes (the wiki has the full version). On first run, look at the v6.0.0 release page (`gh release view v6.0.0 --repo linq2db/linq2db`) to see the body template, then craft a parallel body for the current release. Surface the draft to user for review.
3. Create the release — **after** user confirms title + body + artifact list:
   ```
   gh release create <tag> --repo linq2db/linq2db --title "<ver>" --notes-file <body-md> --generate-notes <lpx-path>
   ```
   - `<tag>` is the same as `<ver>` unless the project uses `v`-prefix tags — confirm on first run (`gh release list --repo linq2db/linq2db --limit 5` shows existing tag conventions).
   - Attach **only** the `.lpx`. Do **not** attach `.lpx6` or `.nupkg` (per user rule — those aren't useful as release attachments).
   - `--generate-notes` ensures the auto-generated "New Contributors" + "Full Changelog" sections appear underneath the user-authored body.
4. Mark step `done`. Record release URL in `state.postpublish.steps.gh-release.url`.

**Optional early draft:** During release prep (`/release-deps`/`/release-test-matrix` phase), the user may draft the GH release body early. If they do, this step picks up the draft from `state.postpublish.releaseDraft` (free-form storage, not strongly typed).

### 4. Next-version bump PR (+ new milestone + `linq2db.t4models` re-pin) [R2-H]

Bootstrap the next development cycle.

Action:
1. **Compute next version.** Default: increment minor, reset patch (`6.3.0` → `6.4.0`). Show user:
   > _"Next version: `6.4.0` (default minor bump). Confirm or override: `6.4.0` / `6.3.1` / `7.0.0` / custom."_
2. **Milestone check.** Read open milestones; if `<next-ver>` isn't there:
   > _"Milestone `<next-ver>` doesn't exist. Create it? (`gh api repos/linq2db/linq2db/milestones --method POST -f title=<next-ver>`)"_
   On `yes`, create. Don't auto-create — explicit confirmation.
3. **Dispatch `/version-bump`.** The existing skill edits `Directory.Build.props` (Version + EFxVersion). Per its contract it creates branch `infra/bump-versions` from `origin/master` and stops at "ready for user to confirm + commit + push + PR".
4. **Additionally re-pin `linq2db.t4models`.** Open `Directory.Packages.props`, find the `<PackageVersion Include="linq2db.t4models">` entry, update its `Version=` to the **just-released** version (the `<ver>` we just published, e.g. `6.3.0` — not the new `<next-ver>`). Show diff. This goes on the same `infra/bump-versions` branch as the version edits.
   - Grep for any other linq2db-published self-references (`<PackageVersion Include="linq2db..." />`) and flag for the user — there should only be `linq2db.t4models` today, but surface anything else found.
5. **User-driven commit + push + PR.** Per `/version-bump`'s contract, the skill does not commit / push / open PR automatically. Surface the full diff for confirmation. On user `go`:
   - `git add Directory.Build.props Directory.Packages.props`
   - `git commit -m "Bump versions for <next-ver>; pin linq2db.t4models to <ver>"`
   - `git push -u origin infra/bump-versions`
   - `gh pr create --base master --head infra/bump-versions --title "Bump versions for <next-ver>" --body-file <body> --milestone <next-ver> --assignee @me --draft`
6. Mark step `done`. Record bump PR number in `state.postpublish.steps.next-bump.pr`.

### Wrap-up

After all 4 steps `done`:
1. Print a summary: "Release `<ver>` complete. Bump PR `#<n>` is open for `<next-ver>`."
2. Suggest the user runs `/session-reflect` to capture any first-run learnings into the long-term docs.
3. Suggest archiving the release state file (`.build/.claude/release-<ver>.json`) — gitignored; can stay as historical record.

## Don'ts

- Do **not** auto-merge the docs PR or auto-publish the GH release. Both are user-driven mutations on shared / public surface.
- Do **not** skip `--generate-notes` on `gh release create`. The "New Contributors" auto-section is the user-visible artefact that gets missed when the flag is absent.
- Do **not** attach `.lpx6` or `.nupkg` to the GitHub release. Per user rule — those aren't useful and the practice is being dropped.
- Do **not** tick step 1 (nuget verify) with missing rows. Re-run after waiting; if persistently missing, treat as a CI publish failure and escalate.
- Do **not** bump `linq2db.t4models` to anything other than the **just-released** version. The point of this re-pin is to have the next release cycle's T4 tests consume the freshly-published nuget; bumping to the next-version-being-prepped would re-introduce the chicken-and-egg cycle the re-pin was designed to break.
- Do **not** include unrelated changes in the next-version bump PR. It's a single-purpose commit: Version + EFxVersion + `linq2db.t4models` re-pin.
