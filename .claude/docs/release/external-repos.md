# External-repo paths and anchors

Paths and references used by the release skills. Pre-seeded with known defaults; updated as the skill learns user-specific values.

## Sibling clones

| Repo | Path | Purpose | Verified |
|------|------|---------|----------|
| `linq2db` | `C:\GitHub\linq2db` | primary product repo (this curation workspace is `linq2db.claude`, a separate clone) | conventional |
| `linq2db.baselines` | `C:\GitHub\linq2db.baselines` | test result baselines; read by `/review-pr`, reset by `/release-publish` step 2 | conventional |
| `linq2db.docs` | `C:\GitHub\linq2db.docs` | documentation source repo; docs PR opened here by `/release-postpublish` step 2. **Not** `linq2db.github.io` — that's the published site, updated by CI from this repo | conventional |
| `linq2db.wiki` | _(no local clone required — accessed via `https://raw.githubusercontent.com/wiki/linq2db/linq2db/<page>.md` on demand; the wiki is not exposed under the GitHub REST `/contents/` endpoint)_ | hosts the release notes draft pages | — |

If a recorded path doesn't exist on disk, the skill asks the user once and updates this table.

## Release notes location

Maintained on the GitHub wiki:

- **Landing page:** `Releases-and-Roadmap.md` (linked from NuGet `<PackageReleaseNotes>` URL template `https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-<version-no-dots>`).
- **Per-version page:** `Release-Notes-<version>.md` (e.g. `Release-Notes-6.3.0.md`).

`/release-notes-validate` reads both via `https://raw.githubusercontent.com/wiki/linq2db/linq2db/<page>.md` (the wiki is a separate GitHub repo `linq2db.wiki`, but its contents are not exposed under the GitHub REST `/contents/` endpoint — raw GitHub URLs are the documented access path).

## GitHub release template anchor

Use `v6.0.0` as the canonical template for GitHub release creation:

- URL: `https://github.com/linq2db/linq2db/releases/tag/v6.0.0`
- Why: preserves the auto-generated **New Contributors** section. That section is produced by `gh release create --generate-notes`; releases created without the flag are missing it.
- When creating a release in `/release-postpublish` step 3, pass `--generate-notes` to ensure the contributors section appears.

## User-specific paths

Filled on first run. Not committed if user-private (mirror to user auto-memory if necessary).

- **Local NuGet server folder (`user-local.nuget-server`):** _(empty — `/release-test-matrix` 4.4 asks on first run and records here)_
