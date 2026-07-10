---
name: release-notes
description: Draft and publish linq2db release notes. Per-PR, composes a user-facing change summary (from the diff + PR body + linked issues/discussions) and posts it as an idempotent draft comment on the PR with checkboxes controlling whether it ships and proposed full (wiki) + brief (GitHub release) text. On merge, applies the note to the wiki release-notes page (`Releases-and-Roadmap.md`). Orphan-sweep mode finds milestone PRs the agent never witnessed (user-merged) and backfills their drafts + wiki application; harvest mode assembles the GitHub-release brief. Also keeps a merged PR and the issues it closes on the same milestone. Modes: draft, refresh, apply, sweep, harvest. Every GitHub/wiki write is user-confirmed.
---

# /release-notes

## What this skill is (and isn't)

**Is:** the release-notes *drafter and applier*. It builds the user-facing notes as work happens, stores each PR's draft in a marker comment on the PR (the durable per-PR store), and publishes to the two artifacts at the right moments:

1. **Full notes** — GitHub wiki `Releases-and-Roadmap.md` (single landing page). Per-release `### Release X.Y.Z` section, `#### <Component>` groups, change-type sub-groups (⚠ Breaking / Added / Improved / Fixed), `##### deep-dive` subsections with code for notable features.
2. **Brief notes** — the GitHub release body highlights (assembled at release time from the per-PR `include-brief` entries).

**Isn't:**

- Not the prose author's replacement — *the agent writes the change description*; the scripts only do the GitHub/wiki plumbing. Follow the per-provider claim-verification discipline (`agent-rules.md` → **Agent Guardrails**) when describing provider behavior.
- Not the coverage auditor — that's [`/release-notes-validate`](../release-notes-validate/SKILL.md), which confirms every milestone item is mentioned. This skill *produces and places* the notes; validate runs after, as the safety check. Both share enumeration via `release-notes-audit.ps1`.
- Not autonomous — every comment create/update, every wiki write, and every milestone assignment is shown to the user and confirmed before it lands.

## When to run

| Mode | Trigger |
|------|---------|
| `draft <pr>` | User asks for a release-notes draft; also dispatched from `/review-pr`'s opt-in draft step. |
| `refresh <pr>` | After a successful push to a PR that already has a draft comment (the "After every successful push" rule in [`pr-and-push.md`](../../docs/pr-and-push.md)). |
| `apply <pr>` | **Optional, explicit** — when the user asks to publish to the wiki (often right after merge so users can preview upcoming changes). Not automatic on merge. The authoritative full-section generation is at release prep (`sweep`/`harvest`). |
| `sweep` | Release prep task 5; ad-hoc "find PRs missing release notes". |
| `harvest` | Release prep task 5 (assemble the GitHub-release brief). |

## Required reading

- [`.agents/docs/release/external-repos.md`](../../docs/release/external-repos.md) → wiki clone path + `wiki-write-strategy` + component-mapping notes.
- [`.agents/docs/pr-and-push.md`](../../docs/pr-and-push.md) → the push-time and merge-time integration blocks.
- [`.agents/docs/github-authoring.md`](../../docs/github-authoring.md) → comment/PATCH mechanics, never-edit-others' content, milestone-by-numeric-id.

## The draft comment

One marker comment per PR, found by the version-agnostic prefix `<!-- release-notes:draft:` and updated in place. Shape (built by `release-notes-draft.ps1`):

- Two task-list checkboxes — **Omit from release notes** (both artifacts) and **Include in the GitHub release highlights** (brief). The rendered checkboxes are authoritative for the flags (the maintainer may toggle them in the UI).
- **Full release notes (wiki)** section — the proposed wiki text, between `<!-- rn:full:start/end -->`.
- **GitHub release highlight (brief)** section — the proposed one-liner, between `<!-- rn:brief:start/end -->`.
- A hidden machine block `<!-- release-notes:state {json} -->` carrying `lastSha` (the PR HEAD the draft was generated from — authoritative for change detection), plus `omit` / `includeBrief` / `markerVersion`.

## Procedure

### Mode: `draft <pr>` / `refresh <pr>`

1. **Load context + decide if work is needed.**
   - `release-notes-draft.ps1 -Action find -Pr <n>` → existing comment + state (`present`, `lastSha`, flags).
   - Resolve PR HEAD: `pr-context.ps1` (returns `headSha`, body, linked issues).
   - For `refresh`: if no draft exists, **do nothing** (drafts are created via `draft`, not auto-created on a stray push). If `lastSha == headSha`, it's a no-op — report "draft current" and stop. If the draft is marked `omit`, leave it (still note the SHA is stale, but don't regenerate prose for an omitted PR unless the user asks).
2. **Compose the user-facing text.** **Read the actual code diff — it is the source of truth, not the PR body.** The `pr-context.ps1` preview shows only the PR body (and truncates it); a body regularly omits or under-describes what the code actually does (a second supported type — e.g. #5624 also added `'T voption`, not just `'T option`; extra overloads; an additional fix folded in; a facet the author didn't call out). **Before composing, run `diff-reader.ps1` over the PR's non-test source files and read every changed source hunk** — enumerate the new/changed public API and behavior from the diff, then reconcile against the body (the body fills in *intent*; the diff decides *what shipped*). Never compose a draft from the body/title/memory alone — that is how a real feature (a whole supported type) gets silently dropped from the notes. Then write:
   - **Full text:** the wiki bullet(s) and any `##### deep-dive` for a notable feature. Group mentally by component + change type (the wiki inserter does the final grouping at `apply`).
   - **Brief text:** a single user-facing highlight line.
   - Verify any provider-specific behavior claim against the translator code at PR HEAD before writing it.
3. **Show the proposed comment to the user and confirm.** Present the full + brief text and the proposed checkbox defaults (default: not omitted; include-brief on for user-visible features, off for pure internal/infra). Wait for confirmation; let the user adjust text or flags.
4. **Write the comment.** Build the manifest at `.build/.agents/release-notes-draft-<pr>.json`:
   ```json
   { "pr": <n>, "lastSha": "<headSha>", "omit": false, "includeBrief": true,
     "fullText": "...", "briefText": "..." }
   ```
   When a draft **already exists** (this will be a PATCH, not a create), first show the user a diff of the changed **full** / **brief** sections (current comment text → proposed text) so it's clear what's changing — don't just re-present the whole new body. Get confirmation, then:
   Then `release-notes-draft.ps1 -Action upsert -ManifestFile <path>`. The script posts (first time) or PATCHes + byte-verifies (update). Exit code 2 = verify mismatch — report it, don't claim success.

### Mode: `apply <pr>` (on merge — wiki strategy B: stage → diff → push on confirm)

1. **Harvest the PR's draft.** `find` the comment; if `omit` is set, **skip** (nothing to publish) and report. Read the full text + linked issues.
2. **Resolve the wiki clone** (`external-repos.md` → clone path). If absent, stop and ask the user to clone it once (`git clone https://github.com/linq2db/linq2db.wiki.git <path>`) — don't auto-clone.
3. **Structure the bullet(s).** From the harvested full text, build the apply manifest — one `prBullets[]` entry per bullet with `{pr, issue?, component, changeType, text, url}` and any `deepDives[]` `{pr, heading, body}`. Component from changed-file paths + labels (see `external-repos.md` mapping); change type from labels (`breaking-change`→Breaking, `enhancement`→Added, `bug`→Fixed) — confirm ambiguous mappings with the user.
4. **Regenerate + diff.** `release-notes-draft.ps1 -Action apply-wiki -ManifestFile <path>`. The script requires a clean clone, ff-only-pulls, **rebuilds the entire `### Release <ver>` section** from the manifest (inherently idempotent), writes the page, and emits a git diff to `diffPath`. It does **not** commit or push.
   - Because the section is fully regenerated, `apply` for a single PR must be given the *cumulative* bullet set for the version (re-harvest the milestone or accumulate). For incremental single-PR merges, prefer running `sweep` (which gathers all merged PRs) over a one-PR `apply` — see note below.
5. **Show the diff and confirm.** Read `diffPath`; present it. On confirmation: `git -C <clone> commit -am "Release notes: <ver> — #<pr>"` then (separate, explicitly confirmed) `git -C <clone> push`. Never push without the confirm.

> **Single-merge vs batch.** Full-section regeneration means the apply manifest should carry every non-omitted bullet for the version, not just the one PR. In practice: on each merge, run `sweep` (or `harvest`) to assemble the complete current bullet set, then one `apply-wiki`. A lone-PR apply is only safe when that PR is the first in the version.

### Mode: `sweep` (orphan backfill — release task 5 + ad-hoc)

PRs merged by the user (not the agent) have no draft comment and no wiki entry. This mode finds and fixes them.

1. `release-notes-draft.ps1 -Action sweep-plan -Milestone <ver> [-PlanFile .build/.agents/release-<ver>-notes-plan.json] [-WikiClone <path>]`.
   - `missingDraft[]` — merged milestone PRs with no draft comment.
   - `missingWiki[]` — non-omitted PRs not yet mentioned in the wiki notes (PR# or any linked issue#).
2. **Render both lists** as numbered tables; let the user pick which to process (default: all).
   - **Persist every omit decision on the PR — don't just decide it in-session.** When the user (or you, with confirmation) triages a PR as "not user-facing / omit", immediately `upsert` an **omit-flagged** draft comment for it (`omit:true`, empty `fullText`/`briefText`). An in-session-only decision is lost the moment the session ends, so the PR reappears in the *next* `sweep-plan`'s `missingDraft`/`missingWiki` and the same triage gets re-litigated. The persisted omit comment sets `hasDraft:true` + `omit:true`, which excludes it from both lists permanently. Batch the omit upserts (one driver over the PR→headSha map) — omit records need no prose, so they don't fall under the one-at-a-time per-PR approval that user-facing drafts do; a single bulk confirmation of the omit set is enough.
3. For each `missingDraft` PR → run the `draft` procedure (compose + confirm + upsert).
4. Once drafts exist, build the cumulative `apply-wiki` manifest from `harvest` and run the `apply` procedure once (diff → confirm → push). **`apply-wiki` regenerates the whole `### Release <ver>` section, so the manifest MUST carry every non-omitted bullet already in the wiki *plus* the new ones — a manifest of only the new bullets silently deletes the already-published ones.** `harvest` returns each PR's *draft-comment* text, which can diverge from a hand-edited wiki; when the live section may hold maintainer edits, parse the current `Releases-and-Roadmap.md` section and merge the new bullets into it rather than trusting harvest text alone. The emitted diff is the safety gate: it should show **additions only** — any `-` line touching an existing bullet means the manifest is incomplete, so stop and fix it before commit.
5. A PR that only fixes a feature introduced in this **same** release is an intentional omission — there's no prior-release regression users experienced; mark it `omit` rather than adding a Fixed bullet.
6. Hand off to `/release-notes-validate` for the final coverage confirmation.

### Mode: `harvest` (assemble the GitHub-release brief)

1. `release-notes-draft.ps1 -Action harvest -Milestone <ver> [-PlanFile ...]` → `.build/.agents/release-<ver>-notes-harvest.json` (`items[]` with per-PR `omit` / `includeBrief` / `fullText` / `briefText`).
2. From items where `includeBrief && !omit`, assemble the GitHub release highlights, bucketed like the existing release body (LinqToDB highlights, provider-specific, linq2db.cli — same component mapping as the wiki inserter). Present the assembled brief for the user to drop into the GitHub release body (`/release-postpublish` step 3, above the `--generate-notes` `## What's Changed`).

## Format conventions (wiki)

**Write for users, not contributors.** Describe the observable behavior — what the user saw go wrong (LINQ shape, `Sql.Expr`, option, provider, the exception they hit) and that it's fixed. Keep linq2db internals out of the notes: no visitor/optimizer/corrector names, AST node types, visit modes, reflection mechanics. Internal detail belongs in the PR/commit. Also exclude, as no-user-value noise: **dependency version bumps** with no user-visible effect, a **database's own vendor limitations** (users know their DB — e.g. "provider X has no keyless tables"), and **internal/plumbing types even when technically public** (descriptor / introspection types like `EntityDescriptor.*`). Describe only the user-facing API and the observable behavior. (Always read the diff first per `agent-rules.md` → **Before summarizing a PR**, then translate to user language.) If a change is *worded* internally but is genuinely user-relevant, **reword it in user-observable terms — don't drop it** (e.g. #5556's second fix shipped as "a computed expression reused in both a subquery column and the outer `ORDER BY` could reference the wrong table alias", not omitted).

Applied only to **newly generated** content (the tool-owned in-progress version section); never rewrite existing released sections.

- Component as `#### <Component>` (h4). **Ordering: all project (linq2db package) areas first, then database/provider areas alphabetically.** `LinqToDB` core first → other `LinqToDB *` packages → `LinqToDB CLI` → then DB/provider areas (Access, ClickHouse, Oracle, SQLite, Sybase ASE, …) alphabetically. Encoded in `release-notes-draft.ps1` `Get-ComponentRank` (project = 0/100/300, db = 1000).
- **Component names must match how older releases name them — don't invent.** Established project-area headers: `LinqToDB`, `LinqToDB for EntityFramework` (NOT `LinqToDB.EntityFrameworkCore`), `LinqToDB LINQPad Driver` (NOT `LinqToDB.LINQPad`), `LinqToDB CLI`, `LinqToDB F# Support`. Before naming any component, grep the older `### Release` sections of `Releases-and-Roadmap.md` for the precedent.
- Within a component, change-type sub-groups as `##### <type>` (h5 — visibly smaller than the component) in order **⚠ Breaking changes**, **Added**, **Improved**, **Fixed** (then Changed/Removed/Other). Breaking floats to the top.
- Bullets: `- <description> ([#<pr>](<url>))`, sorted by PR number within a group.
- Deep dives: `#### <heading>` (h4 spotlight) preceded by a `<!-- rn:deepdive:#<pr> -->` anchor, after the component groups. **The feature's summary bullet links to its deep-dive** with `See [details](#<heading-slug>) below.` appended to the bullet text (older-release convention, e.g. `See [details](#clickhouse) below`). The slug is the GitHub heading slug — lowercase, drop everything but `[a-z0-9 -]`, spaces→hyphens, consecutive separators **not** collapsed (`"NULLS FIRST / LAST ordering"` → `#nulls-first--last-ordering`); `release-notes-draft.ps1` `Build-VersionSection` emits it via `Get-GitHubAnchor`. **Wiki-only** — the anchor resolves on that page, so do **not** add the link to the PR draft comment. Verify after push: `curl -sL <wiki-url> | grep 'href="#<slug>"'`. A **provider spotlight** (a new/promoted-provider deep-dive, e.g. DuckDB / YDB) lists **supported features only** — no "Not supported" section (matches the existing DuckDB spotlight).
- **Crediting external contributors:** use the contributor's **name** as the profile link text when it's known (e.g. `Thanks to [Tim Haasdyk](https://github.com/myieye)`), falling back to the `@handle` only when no name is available. The name comes from the PR/commit author (`gh pr view --json author` `.name`, or the commit `From:` line).

## Milestone consistency (companion)

A merged PR and the issues it closes should share a milestone. `milestone-consistency.ps1` (used here and by `/review-pr` + `/release-milestone-check`):

- `-Action check -Pr <n>` → laggard issues (closed by the PR but on a different/no milestone), each annotated with `relation` + `likelyIntentional`. A `likelyIntentional` laggard (issue on an earlier/closed milestone — its fix shipped earlier, this PR is a follow-up like a test-enable) is **not** drift; leave it.
- `-Action assign -Pr <n> [-DryRun] [-IncludeReleased]` → assign the PR's milestone to the genuine laggards (REST PATCH by numeric id; verifies after). Skips `likelyIntentional` ones by default (`-IncludeReleased` overrides). **Propose, then confirm** before `assign` — milestone is metadata but the change is visible.

Run `check` whenever you draft/apply notes for a PR; surface laggards and offer `assign`.

## Don'ts

- Do **not** post or update a comment, write the wiki, or assign a milestone without showing the user and getting confirmation.
- Do **not** `git push` the wiki without an explicit confirm of the diff (strategy B).
- Do **not** auto-create a draft on a stray push — `refresh` only updates an existing draft.
- Do **not** regenerate prose for an `omit`-flagged PR unless the user asks.
- Do **not** leave an omit decision in-session only — persist it as an `omit`-flagged draft comment on the PR the same turn it's decided (see `sweep` step 2), or the PR resurfaces in the next sweep and the triage is re-litigated.
- Do **not** hand-edit released wiki sections — only the in-progress version section is tool-owned.
- Do **not** tick release task 5 until `sweep` shows no `missingDraft`/`missingWiki` and `/release-notes-validate` is clean.
