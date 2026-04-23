---
name: update-slnx
description: Sync the /.claude/* virtual folders in linq2db.slnx with the actual on-disk contents of the .claude/ directory. Use after adding, removing, or renaming files or subdirectories under .claude/ (skills, hooks, local settings, etc.).
---

# update-slnx

User-triggered workflow to keep `linq2db.slnx` in sync with the on-disk `.claude/` folder.

## When to run

Only when the user explicitly invokes this skill or asks to sync the slnx with `.claude/`. Do not run it spontaneously as a "cleanup".

## Scope

This skill only touches virtual folders whose name starts with `/.claude/` in `linq2db.slnx`. It does not reorganize `/.github/`, `/.root/`, project references, or any other solution entries.

## Conventions (match existing slnx style)

- Flat folder entries keyed by full path — one `<Folder Name="/.claude/<subpath>/">` block per directory level. Do **not** nest folder elements. Compare to how `/.github/` and `/.github/ISSUE_TEMPLATE/` are listed as separate top-level `<Folder>` blocks.
- Each `<File Path="…">` value is the **repo-relative** path (e.g. `.claude/skills/version-bump/SKILL.md`), with one exception: the top-level `/.claude/` folder also lists `CLAUDE.md` by its bare name, because `CLAUDE.md` lives at the repo root but is grouped here for editorial convenience.
- Include gitignored files that the user keeps under `.claude/` (e.g. `settings.local.json`). The slnx is a tool for the developer's IDE — its contents don't have to mirror git-tracked files. Do not drop gitignored files unless the user asks.
- Preserve indentation: two-space indent inside `<Solution>`, four-space indent inside each `<Folder>` — match the surrounding file exactly.

## Always-included entries

Even if these files are missing from disk, they must appear in the slnx's `/.claude/` folder:

- `CLAUDE.md` — repo-root CLAUDE.md listed by bare name.
- `.claude/settings.local.json` — personal settings override (gitignored).

If `.claude/settings.local.json` does not exist on disk, propose creating it with the content `{}\n` and include the entry in the slnx once the user confirms the file creation. If the user declines to create the file, still include the slnx entry — the IDE will show it as missing, which is acceptable.

## Steps

### 1. Enumerate on-disk `.claude/` contents

Walk `.claude/` and collect:

- Every file directly in `.claude/` (e.g. `.claude/settings.local.json`).
- Every file in each subdirectory, grouped by directory (e.g. `.claude/skills/version-bump/SKILL.md` belongs to the group `.claude/skills/version-bump/`).

Skip empty directories — they produce no `<Folder>` entry.

### 2. Check always-included entries

If `.claude/settings.local.json` is absent from disk, tell the user and offer to create it with `{}\n`. Wait for their decision before moving on — either create the file or note that it will be listed as missing in the slnx.

### 3. Read the current slnx state

Read `linq2db.slnx` and extract every `<Folder>` whose `Name` starts with `/.claude/`, along with its `<File>` children.

### 4. Compute the desired state

For each on-disk directory under `.claude/`, produce one `<Folder>` block:

- Virtual folder name: `/.claude/<subpath>/` (use `/.claude/` for files directly in `.claude/`).
- Files listed in sorted order, using repo-relative paths.
- The top-level `/.claude/` folder additionally lists `<File Path="CLAUDE.md" />` as the first entry and `<File Path=".claude/settings.local.json" />` (always — per "Always-included entries" above).

### 5. Show the diff and wait for confirmation

Present the diff against the current slnx (unified `- old` / `+ new` lines, scoped to the `/.claude/*` folder blocks). Wait for explicit user approval before editing.

If the desired state matches the current state, say so and stop — no edit needed.

### 6. Apply

Only after user confirmation:

- Replace the existing block of `/.claude/*` `<Folder>` entries in `linq2db.slnx` with the desired ones, keeping them contiguous and in the same position in the file (right after `<Configurations>`, before `/.github/`).
- Do **not** commit. Per `.claude/docs/agent-rules.md` → **Git commit rules**, commits require an explicit user request.
