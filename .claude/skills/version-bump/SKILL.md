---
name: version-bump
description: Bump product and EF versions in Directory.Build.props to match the next release milestone. Creates an infra/bump-versions branch from origin/master, sets <Version> to the next milestone version, increments each <EFxVersion> minor by 1, and requires explicit user confirmation of the proposed diff.
---

# version-bump

User-triggered workflow to prepare a version-bump PR.

## When to run

Only when the user explicitly invokes this skill. Do not propose bumps during unrelated work.

## Steps

### 1. Verify bump is needed

1. `git fetch origin master`.
2. Read `<Version>` from `Directory.Build.props` on `origin/master`:
   ```
   git show origin/master:Directory.Build.props
   ```
   Extract the value of the `<Version>` property (not `<AssemblyVersion>`, `<PackageVersion>`, `<BaselineVersion>`, or any `<EFxVersion>`).
3. Fetch open milestones: `gh api repos/linq2db/linq2db/milestones?state=open --jq '.[] | .title'`.
4. Pick the **next release milestone** — the lowest versioned milestone (title starts with a digit, has the form `M.m.p`) that is `>= master <Version>`.
5. If master `<Version>` is already equal to or greater than the next milestone version, report the state and stop. No bump.
6. Otherwise continue.

### 2. Prepare the branch

Follow the "Creating a new branch" rules in `CLAUDE.md`:

- Branch name: `infra/bump-versions`.
- Base: `origin/master` (already fetched above).
- If the working tree is dirty, stop and ask the user whether to stash or discard before branching.

Do **not** create the branch yet — wait for user confirmation of the proposed changes in step 3.

### 3. Compute and present the diff

Read current values from `Directory.Build.props` on `origin/master`:

- `<Version>` → the next milestone's version (from step 1.4)
- `<EF3Version>` → increment minor by 1, reset patch to 0 (e.g. `3.31.0` → `3.32.0`)
- `<EF8Version>` → same rule
- `<EF9Version>` → same rule
- `<EF10Version>` → same rule

Present the proposal in this exact format:

```
Version       <old>  -> <new>
EF3Version    <old>  -> <new>
EF8Version    <old>  -> <new>
EF9Version    <old>  -> <new>
EF10Version   <old>  -> <new>
```

Wait for explicit user confirmation (e.g. "yes", "go", "apply") before editing.

### 4. All-or-nothing check

All four `<EFxVersion>` values must end up different from their previous value. If any one would remain unchanged, **refuse the bump**, explain which one didn't move, and stop. Do not commit a partial update.

### 5. Apply

Only after user confirmation:

1. Create branch `infra/bump-versions` from `origin/master`.
2. Edit `Directory.Build.props` — update all five properties. Preserve existing formatting (tabs, column alignment, comments).
3. Do **not** commit, push, or open a PR automatically. Per `CLAUDE.md` rules, each of those actions needs its own explicit user request.
