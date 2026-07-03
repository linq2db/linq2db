# External-repo paths and anchors

Paths and references used by the release skills. Pre-seeded with known defaults; updated as the skill learns user-specific values.

## Sibling clones

| Repo | Path | Purpose | Verified |
|------|------|---------|----------|
| `linq2db` | `../linq2db` | primary product repo (this curation workspace is a separate clone from it) | conventional |
| `linq2db.baselines` | `../linq2db.baselines` | test result baselines; read by `/review-pr`, reset by `/release-publish` step 3 | conventional |
| `linq2db.docs` | `../linq2db.docs` | documentation source repo; docs PR opened here by `/release-postpublish` step 2. **Not** `linq2db.github.io` — that's the published site, updated by CI from this repo | conventional |
| `linq2db.wiki` | `../linq2db.wiki` | hosts the release-notes page (`Releases-and-Roadmap.md`). **Read** via `https://raw.githubusercontent.com/wiki/linq2db/linq2db/<page>.md` on demand (not exposed under the REST `/contents/` endpoint). **Write** needs the local clone (`git clone https://github.com/linq2db/linq2db.wiki.git`); `/release-notes apply` regenerates the version section there, shows the diff, and pushes on confirm. | — |

If a recorded path doesn't exist on disk, the skill asks the user once and updates this table. `/release-notes apply-wiki` stops and asks the user to clone `linq2db.wiki` once if the path is absent — it never auto-clones.

### Windows: cloning `linq2db.wiki` (colon-in-filename gotcha)

The wiki repo contains a page named `[Internal]-Azure-Pipelines:-Open-Tasks.md`. The `:` is illegal in NTFS filenames, so a plain `git clone` **fails at checkout** ("invalid path … : …") and leaves an empty/inconsistent working tree — do **not** `git add`/commit from that state (every other page shows as a staged deletion). Clone with no checkout, restrict to the release-notes page via sparse-checkout, then check out with NTFS protection disabled (the bad file is `skip-worktree`, so it's never written to disk):

```
git clone --no-checkout https://github.com/linq2db/linq2db.wiki.git ../linq2db.wiki
git -C ../linq2db.wiki sparse-checkout set --no-cone Releases-and-Roadmap.md
git -C ../linq2db.wiki -c core.protectNTFS=false checkout master
```

After this, `git status` is clean and only `Releases-and-Roadmap.md` is materialized; `apply-wiki` + commit + push work normally (they never touch the colon-named blob, which stays in the tree untouched).

## Release-notes wiki-write strategy

`wiki-write-strategy: stage-confirm-push` — on PR merge, `/release-notes apply` regenerates the `### Release <ver>` section in the local clone, emits a git diff, and pushes only after the user confirms the diff. Never auto-pushes. (Per-version `Release-Notes-<version>.md` pages are retired — everything lands on the single `Releases-and-Roadmap.md`.)

## Release-notes component / change-type mapping

How `/release-notes` buckets a PR into the wiki structure. Accrued as the skill learns; start heuristic:

- **Component** (`#### <Component>` group): `LinqToDB` by default; `LinqToDB CLI` when the PR touches `Source/LinqToDB.Tools` / CLI paths; provider-specific components when the change is provider-scoped. Confirm ambiguous cases with the user.
  - **Feature spanning core + an integration (EF / provider):** assign by *where the public API lives* — verify via which package's `PublicAPI.*.txt` declares it. A core API usable standalone (its entries are under `Source/LinqToDB/PublicAPI/…`) → `LinqToDB`; the integration/forwarding half → the integration component (`LinqToDB for EntityFramework`, a provider, …). **Split into per-component bullets** rather than filing the whole PR under the integration. (e.g. #5525 named query filters: the `HasQueryFilter(key,…)` / `IgnoreFilters(keys)` API is core `LinqToDB`; the EF-Core-10 keyed-filter forwarding is `LinqToDB for EntityFramework`.)
- **Change type** (sub-group): from PR labels — `breaking-change` → ⚠ Breaking, `enhancement` / `feature` → Added, `bug` → Fixed; otherwise ask. Order within a component: Breaking, Added, Improved, Fixed.

## Release notes location

Maintained on the GitHub wiki:

- **Landing page:** `Releases-and-Roadmap.md` (linked from NuGet `<PackageReleaseNotes>` URL template `https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-<version-no-dots>`). This is the single full-notes artifact — `/release-notes` drafts and applies here.

Per-version `Release-Notes-<version>.md` pages are **retired** (they were created only for major releases due to volume; with release-notes automation everything lands on the landing page). `/release-notes-validate` and `/release-notes` read the landing page via `https://raw.githubusercontent.com/wiki/linq2db/linq2db/Releases-and-Roadmap.md` (the wiki is a separate GitHub repo `linq2db.wiki`, but its contents are not exposed under the GitHub REST `/contents/` endpoint — raw GitHub URLs are the documented read path).

## GitHub release template anchor

Use `v6.0.0` as the canonical template for GitHub release creation:

- URL: `https://github.com/linq2db/linq2db/releases/tag/v6.0.0`
- Why: preserves the auto-generated **New Contributors** section. That section is produced by `gh release create --generate-notes`; releases created without the flag are missing it.
- When creating a release in `/release-postpublish` step 3, pass `--generate-notes` to ensure the contributors section appears.

## User-specific paths

Filled on first run. Not committed if user-private (mirror to user auto-memory if necessary).

- **Local NuGet server (`user-local.nuget-server`):** machine-specific (do not commit) — stored in user auto-memory. Recorded shape: `{ingestionFolder, feedUrl, wakeProtocol, ingestionIndicator}`. Generic behavior contract: drop `.nupkg` into the ingestion folder, the server consumes + removes them in ~10s; folder emptiness = ingestion complete.
- **Self-hosted fuget server (`user-local.fuget-server`):** machine-specific (do not commit) — stored in user auto-memory. Generic behavior contract: drop-in replacement for `https://www.fuget.org` (used as `-FugetBase` override by [`/release-deps`](../../skills/release-deps/SKILL.md) Fuget API-diff procedure — see [`nuget-package-notes.md`](./nuget-package-notes.md) → *Cross-package procedures*).
