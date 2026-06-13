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

## Running tests from a worktree

`test-runner`, `/test`, and `/test-providers` accept an explicit repo-root override so a worktree can be the test target instead of the primary clone. This matters because the skills otherwise resolve `UserDataProviders.json` and project paths against cwd, and subagents inherit the primary-clone cwd — so without the override they build and test the *primary* clone, not the worktree.

1. **Env** — `/test-providers <providers> worktree <abs-worktree-path>` seeds `<worktree>/UserDataProviders.json` from the primary/sibling clone if absent, enables the requested providers *there*, and starts the (shared) containers. Enable **only** the provider(s) under test — `[DataSources]` tests otherwise fan out to every enabled provider's container. **Disable it again when that test is done:** a provider left enabled from an earlier repro whose container is later stopped makes every subsequent `[DataSources]` case fail with `connection refused`, which looks like a wave of regressions but is just the dead container drowning the real result.
2. **Run** — `/test run <filter> worktree <abs-worktree-path>`: `/test` passes `repoRoot=<worktree>` to `test-runner`, which reads that `UserDataProviders.json` and runs `dotnet test --project <worktree>/<project>` so the worktree's code is built. Prepend `CreateData.CreateDatabase` to the filter only when the test needs the full schema — a self-contained `CreateLocalTable` test doesn't, which avoids rebuilding the target DB.
3. **Scratch** — a `<Compile Include>` link added to the worktree's `Tests.Playground.csproj` for a fast Playground build, and the seeded `UserDataProviders.json`, are scratch: keep them out of any commit (stage with explicit pathspec; see [`agent-rules.md`](agent-rules.md) → *Git commit rules*).

## Removing a worktree blocked by file locks

`git worktree remove --force <path>` may fail with `error: failed to delete '<path>': Permission denied` when the worktree was recently built — VBCSCompiler / MSBuild server still holds file handles inside `.build/bin/` even though git's internal worktree registration was successfully dropped (`git worktree list` no longer shows it).

Most removals don't hit this — `git worktree remove --force <path>` succeeds outright once the worktree's own build has finished, no shutdown needed. And **on a shared / multi-worktree machine, do not lead with `dotnet build-server shutdown`**: it is global per-SDK and kills the VBCSCompiler / MSBuild servers that *other* concurrent worktree builds are using, disrupting them mid-build. (This repo is routinely checked out as a dozen parallel worktrees — `git worktree list`.) Reach for `build-server shutdown` only when removal is genuinely lock-blocked **and** no other builds are running.

Cleanup sequence (only when removal is lock-blocked):

1. `dotnet build-server shutdown` — often partial; the VB/C# compiler server may report "failed to shut down" but the handle release still progresses enough.
2. `Remove-Item -Recurse -Force <worktree-path>` from PowerShell. This succeeds in practice even when the build-server output suggested otherwise.
3. `git worktree prune` to drop any leftover registration if step 1 left the worktree in a half-removed state (rare; `--force` in step 0 usually cleared it already).

Don't loop on `git worktree remove --force` — it'll keep failing on the same locked dll. Skip to PowerShell `Remove-Item -Recurse -Force` and the directory deletes cleanly even with the lock-reporting process still around.

## Carrying `.claude/` curation across branch switches

`.claude/` skills, docs, hooks, and scripts accumulate on `infra/claude-curation` between weekly merges to `master`. Switching from `infra/claude-curation` to a working branch (feature/\*, issue/\*, etc.) without carrying those changes forward causes the agent to operate against stale `.claude/` state, losing every refinement since the last master merge. The one-line trigger lives in [`agent-rules.md`](agent-rules.md) → *Creating a new branch*.

- **Rule:** when the working branch is not `master` and not `release`, the `.claude/` working tree should reflect the latest `origin/infra/claude-curation` state, applied as **uncommitted** modifications. Most commonly this means: right after `git switch <target-branch>` (or `git switch -c …`), pull the curation branch's `.claude/` contents into the new branch's tree:
  ```
  git fetch origin infra/claude-curation
  git checkout origin/infra/claude-curation -- .claude/
  ```
- **Never commit the carried-over changes on the working branch.** They show as modified in `git status` but must not be included in any commit. When staging:
  - `git add <specific paths>` only — never `git add .` or `git add -A` while curation diffs are present.
  - `git restore --staged .claude/` if `.claude/` accidentally gets staged.
  - Before any `git push` on a working branch, verify the pushed range carries no `.claude/` diff: `git log origin/<branch>..HEAD --stat -- .claude/` should be empty.
- **Exceptions:** switching to `master` or `release` does **not** carry curation diffs — those branches reflect merged state and should diff cleanly.
- **The only branch where `.claude/` changes are committed is `infra/claude-curation` itself.** Session-end learnings captured via `/session-reflect`, `/audit-claude`, or ad-hoc edits should be applied on the curation branch, not on a working branch. When a session ends with carried-over `.claude/` changes on a working branch and the user wants to keep new edits, the canonical save path is: `git switch infra/claude-curation`, replay the edits there, commit on curation, switch back if more work remains.

## Release-prep orchestration model

When `/release` runs against a `release-prep/<ver>` worktree, the moving parts split between two clones:

| Clone | Branch | Owns |
|---|---|---|
| `C:\GitHub\linq2db.claude` (curation workspace) | `infra/claude-curation` | `.claude/` skills + scripts; orchestrator state file at `.build/.claude/release-<ver>.json`; per-task plan caches; walk-decisions tracker |
| `C:\GitHub\linq2db.claude.release-<ver>` (worktree) | `release-prep/<ver>` | source-tree edits (`Directory.Packages.props`, `.editorconfig`, csproj `VersionOverride` sites, code fixes); per-build outputs under `.build/bin/` |

**Cross-clone calling pattern:** sub-skills that need to run a script from inside the worktree invoke `pwsh -NoProfile -File C:\GitHub\linq2db.claude\.claude\scripts\<name>.ps1 ...` with an absolute path back to curation. The script's `Get-Location` then yields the worktree, so file-system reads (Directory.Packages.props parsing, source globbing) target the right tree. The PowerShell tool's working directory is set explicitly via `Set-Location <worktree>` before each cross-clone call.

**State files** always under curation (`C:\GitHub\linq2db.claude\.build\.claude\release-<ver>*.json` etc.) — one canonical location regardless of which clone the agent is operating from. Plan caches written by sub-skills running in the worktree should also write to curation's `.build/.claude/` (pass `-WriteDir <abs-path>` if the script defaults to a relative `.build/.claude/`).

**Disk pressure.** Each Release build of `linq2db.slnx` adds ~9 GB of `.build/bin` output. With a worktree, that's two `.build/bin/` trees (curation's may be empty if no builds run there; worktree's accumulates per release-prep cycle). When iterating against a near-full C: drive, clean per the *Iterative-build gotchas* section in [`agent-rules.md`](agent-rules.md).

**Session resume primer.** Without the orchestration-model context above, the agent rediscovers the dual-clone setup from scratch each session — costs 10-20 turns. Resume prompts for /release should explicitly state: "orchestrator runs from curation; worktree at `<path>`; state file at `<path>`".
