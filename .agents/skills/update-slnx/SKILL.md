---
name: update-slnx
description: Sync the /.agents/* virtual folders in linq2db.slnx with the actual on-disk contents of the .agents/ directory. Use after adding, removing, or renaming files or subdirectories under .agents/ (skills, hooks, docs, local settings, etc.).
---

# update-slnx

User-triggered workflow to keep `linq2db.slnx` in sync with the on-disk `.agents/` folder.

> `.agents/` is the real instruction source; `.claude` is a symlink to it (for Claude Code's hardcoded discovery). The slnx represents the **real** `.agents/` paths — never the `.claude` symlink, and never a `/.claude/` virtual folder.

## When to run

Only when the user explicitly invokes this skill or asks to sync the slnx with `.agents/`. Do not run it spontaneously as a "cleanup".

## Scope

This skill only touches virtual folders whose name starts with `/.agents/` (plus the top-level editorial folder described below) in `linq2db.slnx`. It does not reorganize `/.github/`, `/.root/`, project references, or any other solution entries.

**Excluded subtree.** `.agents/knowledge-base/**` is intentionally not represented in `linq2db.slnx`. The KB is a generated index (built and refreshed by `/kb-build` and `/kb-refresh`); listing it in the IDE solution is noise. When walking `.agents/` (Step 1) and computing the desired state (Step 4), skip the entire `.agents/knowledge-base/` subtree — neither the `<Folder>` blocks nor any `<File>` children. If the slnx already contains `/.agents/knowledge-base/*` entries (e.g. from a stale earlier sync), remove them as part of the diff.

## Conventions (match existing slnx style)

- Flat folder entries keyed by full path — one `<Folder Name="/.agents/<subpath>/">` block per directory level. Do **not** nest folder elements. Compare to how `/.github/` and `/.github/ISSUE_TEMPLATE/` are listed as separate top-level `<Folder>` blocks.
- Each `<File Path="…">` value is the **repo-relative** path (e.g. `.agents/skills/version-bump/SKILL.md`), with one exception: the top-level `/.agents/` folder also lists the repo-root `AGENTS.md` and `CLAUDE.md` by their bare names, because they live at the repo root but are grouped here for editorial convenience.
- Include gitignored files that the user keeps under `.agents/` (e.g. `settings.local.json`). The slnx is a tool for the developer's IDE — its contents don't have to mirror git-tracked files. Do not drop gitignored files unless the user asks.
- Preserve indentation: two-space indent inside `<Solution>`, four-space indent inside each `<Folder>` — match the surrounding file exactly.

## Always-included entries

Even if these files are missing from disk, they must appear in the slnx's top-level `/.agents/` folder:

- `AGENTS.md` — repo-root canonical contributor guide, listed by bare name.
- `CLAUDE.md` — repo-root Claude Code entry point, listed by bare name.
- `.agents/settings.local.json` — personal settings override (gitignored).

If `.agents/settings.local.json` does not exist on disk, propose creating it with the content `{}\n` and include the entry in the slnx once the user confirms the file creation. If the user declines to create the file, still include the slnx entry — the IDE will show it as missing, which is acceptable.

## Steps

### 1. Enumerate on-disk `.agents/` contents

Walk `.agents/` and collect:

- Every file directly in `.agents/` (e.g. `.agents/settings.local.json`).
- Every file in each subdirectory, grouped by directory (e.g. `.agents/skills/version-bump/SKILL.md` belongs to the group `.agents/skills/version-bump/`).

Skip empty directories — they produce no `<Folder>` entry.

### 2. Check always-included entries

If `.agents/settings.local.json` is absent from disk, tell the user and offer to create it with `{}\n`. Wait for their decision before moving on — either create the file or note that it will be listed as missing in the slnx.

### 3. Read the current slnx state

Read `linq2db.slnx` and extract every `<Folder>` whose `Name` starts with `/.agents/` (and any stale `/.claude/` folder left from before the rename), along with its `<File>` children.

### 4. Compute the desired state

For each on-disk directory under `.agents/`, produce one `<Folder>` block:

- Virtual folder name: `/.agents/<subpath>/` (use `/.agents/` for files directly in `.agents/`).
- Files listed in sorted order, using repo-relative paths.
- The top-level `/.agents/` folder additionally lists `<File Path="AGENTS.md" />` and `<File Path="CLAUDE.md" />` as the first entries and `<File Path=".agents/settings.local.json" />` (always — per "Always-included entries" above).

### 5. Show the diff and wait for confirmation

Present the diff against the current slnx (unified `- old` / `+ new` lines, scoped to the `/.agents/*` folder blocks). Wait for explicit user approval before editing.

If the desired state matches the current state, say so and stop — no edit needed.

### 6. Apply

Only after user confirmation:

- Replace the existing block of `/.agents/*` (and any leftover `/.claude/*`) `<Folder>` entries in `linq2db.slnx` with the desired ones in a **single `Edit` call** — `old_string` spanning the first such `<Folder>` through the closing `</Folder>` of the last block, `new_string` containing the full desired block. Do not split into multiple targeted edits per added/removed file: each Edit triggers its own permission prompt and risks leaving the slnx mid-state if the user aborts between calls. Keep the block contiguous and in its current position (right after `<Configurations>`, before `/.github/`).
- Do **not** commit. Per `.agents/docs/agent-rules.md` → **Git commit rules**, commits require an explicit user request.
