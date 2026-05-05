# Worktrees

Worktree-workflow notes. The high-level "blocked `git checkout`" rule lives in [`agent-rules.md`](agent-rules.md) → *Creating a new branch* — this doc covers the worktree-specific mechanics once you're inside one.

## When to use a worktree

Only when the user explicitly asks. Do not silently `git worktree add` to work around a blocked `git checkout` / `gh pr checkout` — that hides the state conflict and fragments work across two directories. Ask the user how to proceed (stash, commit, discard) and name the blocking files in the question.

## Local / gitignored files in the main repo

When working inside an authorized worktree, local / gitignored files in the *main* repo (`UserDataProviders.json`, `.claude/settings.local.json`, etc.) don't need to be stashed — the worktree has its own copy and edits there leave the main repo untouched.

## `UserDataProviders.json` in a worktree

The test harness reads this file from the worktree root, and a fresh worktree starts without one (only `UserDataProviders.json.template` is tracked). Before running tests from a worktree:

1. Copy the **main repo's** `UserDataProviders.json` into the worktree root (e.g. `cp C:/GitHub/linq2db/UserDataProviders.json <worktree>/UserDataProviders.json`).
2. Adjust the `Providers` list under the TFM you're testing against (`NET100` for `net10.0` runs).

Editing the main repo's copy for a worktree-scoped run is the wrong place — it affects every future run in the main repo too.
