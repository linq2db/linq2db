# Worktrees

Worktree-workflow notes. The high-level "blocked `git checkout`" rule lives in [`agent-rules.md`](agent-rules.md) → *Creating a new branch* — this doc covers the worktree-specific mechanics once you're inside one.

## When to use a worktree

Only when the user explicitly asks. Do not silently `git worktree add` to work around a blocked `git checkout` / `gh pr checkout` — that hides the state conflict and fragments work across two directories. Ask the user how to proceed (stash, commit, discard) and name the blocking files in the question.

## Local / gitignored files in the main repo

When working inside an authorized worktree, local / gitignored files in the *main* repo (`UserDataProviders.json`, `.claude/settings.local.json`, etc.) don't need to be stashed — the worktree has its own copy and edits there leave the main repo untouched.

## `UserDataProviders.json` in a worktree

`TestConfiguration` finds this file by walking **up** the directory tree from the test assembly (`GetFilePath` in `Tests/Base/TestConfiguration.cs`), not from a fixed path. A worktree lives under `<main-repo>/.claude/worktrees/<name>` and builds to `<worktree>/.build/bin/…`, so when the worktree has no `UserDataProviders.json` of its own (a fresh worktree only tracks `UserDataProviders.json.template`), the walk-up reaches the **main repo's** copy. Tests run from a worktree therefore work without copying anything.

Two consequences:

- **Per-run provider selection: use `--provider`, don't copy/edit the JSON.** `<exe> --provider <name>` (or `/test … on <name>`) replaces the active provider set for that run, so you need neither a worktree-local file nor any `Providers`-array edit. The provider only needs a connection string (already in the tracked `DataProviders.json`) and, for server providers, a running container. See [`testing.md`](testing.md) → *Scoping a run to specific providers*.
- A **default** run (no `--provider`) from a worktree inherits the **main repo's** enabled set and `BaselinesPath` via the walk-up. Only if you want a *different default set* for the worktree, copy the main repo's `UserDataProviders.json` into the worktree root and adjust the `Providers` list under the TFM you're testing (`NET100` for `net10.0`). Don't edit the main repo's copy for a worktree-scoped run — it affects every future main-repo run too.
