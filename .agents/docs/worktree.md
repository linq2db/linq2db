# Worktrees

Worktree-workflow notes. The high-level "blocked `git checkout`" rule lives in [`agent-rules.md`](agent-rules.md) → *Creating a new branch* — this doc covers the worktree-specific mechanics once you're inside one.

## When to use a worktree

Only when the user explicitly asks. Do not silently `git worktree add` to work around a blocked `git checkout` / `gh pr checkout` — that hides the state conflict and fragments work across two directories. Ask the user how to proceed (stash, commit, discard) and name the blocking files in the question.

## Base an existing-branch worktree on `origin/<branch>`, not the local ref

`git worktree add <path> <branch>` checks out the **local** branch ref — and a plain `git fetch origin <branch>` does **not** fast-forward it (the fetch only advances `origin/<branch>` and `FETCH_HEAD`, leaving any pre-existing local `<branch>` stale). Worktreeing the bare name then bases your work on the stale tip: a `git merge origin/master` lands on old code, and the eventual `git push` is rejected non-fast-forward because the remote branch is actually ahead. For an existing (especially already-pushed PR) branch, create the worktree from the remote-tracking ref — `git worktree add <path> origin/<branch>` — or `git -C <worktree> reset --hard origin/<branch>` immediately after creating it, before doing any work. (Surfaced syncing PR #5648: worktree off the stale local `feature/…` ref → merge on a base 3 commits behind origin → push rejected → full redo after `reset --hard origin/<branch>`.)

## Local / gitignored files in the main repo

When working inside an authorized worktree, local / gitignored files in the *main* repo (`UserDataProviders.json`, `.agents/settings.local.json`, etc.) don't need to be stashed — the worktree has its own copy and edits there leave the main repo untouched.

Once you know you'll *edit* a file in the worktree, do the investigation `Read` / `Grep` against the **worktree** path, not the primary clone. `Edit`'s read-precondition is path-specific, so a read of the primary-clone copy doesn't satisfy it — you'd have to re-read the worktree copy before editing. Reading source the PR doesn't touch from the primary clone is harmless (same bytes), but it costs an extra round-trip the moment you decide to edit.

## `UserDataProviders.json` in a worktree

`TestConfiguration` finds this file by walking **up** the directory tree from the test assembly (`GetFilePath` in `Tests/Base/TestConfiguration.cs`), not from a fixed path — and the walk-up traverses **ancestors only**. Worktrees are created as **siblings** of the primary clone (`git worktree add ../<clone-dir>.<slug>`, per [`agent-rules.md`](agent-rules.md) → *Creating a new branch*), so the walk-up from `<worktree>/.build/bin/…` reaches the **worktree root** — which tracks only `UserDataProviders.json.template` — and **never** the primary clone's `UserDataProviders.json` (a sibling, not an ancestor). A `--provider` run needs nothing from that file; a baseline-sensitive run does (see the second consequence below).

Two consequences:

- **Per-run provider selection: use `--provider`, don't copy/edit the JSON.** `<exe> --provider <name>` (or `/test … on <name>`) replaces the active provider set for that run, so you need neither a worktree-local file nor any `Providers`-array edit. The provider only needs a connection string (already in the tracked `DataProviders.json`) and, for server providers, a running container. See [`testing.md`](testing.md) → *Scoping a run to specific providers*.
- **Baseline-sensitive runs need `UserDataProviders.json` copied into the worktree root, or they come back vacuously green.** `BaselinesManager.Dump` is gated on `TestConfiguration.BaselinesPath`, which lives only in `UserDataProviders.json` (not the template). A sibling worktree's walk-up never finds it, so `BaselinesPath` is null, the direct-vs-remote `.sql` / `.sql.other` comparison silently no-ops, and a divergence-introducing change passes **undetected** (a green run that never ran the parity check). Copy the primary clone's `UserDataProviders.json` into the worktree root (gitignored scratch) before such runs, and confirm a baseline file's mtime updated during the run. The same copy also lets you set a *different default provider set* for the worktree (adjust the `Providers` list under the TFM, e.g. `NET100` for `net10.0`) — don't edit the primary clone's copy for a worktree-scoped run, it affects every future main-repo run too.

## Running tests from a worktree

`test-runner`, `/test`, and `/test-providers` accept an explicit repo-root override so a worktree can be the test target instead of the primary clone — without it the skills resolve project paths against the inherited primary-clone cwd and build/test the *primary* clone, not the worktree.

1. **Env** — start the container(s) the test needs (`/test-providers` owns container start/stop). Provider *selection* is per-run via `--provider` (next step), so no worktree-local `UserDataProviders.json` seeding or `Providers`-array enable is required — the provider only needs a connection string in the tracked `DataProviders.json`. A `--provider` naming a **stopped** container fails every `[DataSources]` case with `connection refused`, which looks like a wave of regressions but is just the dead container — start it first.
2. **Run** — `/test run <filter> worktree <abs-worktree-path>`: `/test` passes `repoRoot=<worktree>` to `test-runner`, which runs `dotnet test --project <worktree>/<project> --provider <names>` so the worktree's code is built and exactly those providers run. Prepend `CreateData.CreateDatabase` to the filter only when the test needs the full schema — a self-contained `CreateLocalTable` test doesn't, which avoids rebuilding the target DB.
3. **Scratch** — a `<Compile Include>` link added to the worktree's `Tests.Playground.csproj` for a fast Playground build (and any worktree-local `UserDataProviders.json` you seeded for a different default set) is scratch: keep it out of any commit (stage with explicit pathspec; see [`agent-rules.md`](agent-rules.md) → *Git commit rules*).

**EFCore single-test runs (EF8 / EF9 / EF10).** EF tests live in their own projects (`Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF<n>.csproj`, **net10.0-only**), which `test-runner` targets directly via `--project … Tests.EntityFrameworkCore.EF10.csproj`. Provider resolution differs from the main suite: `[EFDataSources]` / `[EFIncludeDataSources]` yield `TestConfiguration.UserProviders ∩ TestConfiguration.EFProviders`, where `EFProviders` is a curated list (`SQLiteMS`, `SqlServer2016+MS`, `PostgreSQL13+`, …). So the target provider must be both in that curated set **and** passed via `--provider` (or enabled in the worktree's `UserDataProviders.json`) — enabling a non-EF provider like `SQLite.Classic` resolves **zero** EF tests. `SQLite.MS` needs no container, so it's the cheapest EF target for a worktree run. (Surfaced on PR #5525: a single EF10 `IgnoreQueryFilters([])` regression test was run red→green on `SQLite.MS` via `/test … worktree`.)

**Reviewing / verifying a PR's *own* new tests — run them yourself, not via `test-runner`.** When the tests under verification are **added by the PR** (absent from the curation checkout), `test-runner` reports the fixture "doesn't exist" if pointed at the primary clone, and — being Bash-only, it can't `Set-Location` into the worktree — blocks on the worktree's `global.json` runner resolution (MTP-vs-VSTest, exit 2) when pointed there. On the block it tends to **rogue-spawn detached `dotnet` background jobs** that leak processes and lock the worktree directory against later `git worktree remove`. Instead: create the worktree off `origin/pr/<n>`, seed its `UserDataProviders.json`, and run the tests yourself — `Set-Location <worktree>` then `dotnet test --project Tests/Linq/Tests.csproj -f net10.0 --filter "…" -c Debug --settings .runsettings --provider <names>` via the **PowerShell tool**, capturing to a log under `.build/.agents/` and `Grep`-ing the `Test run summary:` + `^(failed|skipped) ` lines (MTP prints only failed/skipped per-test; succeeded ones show in the summary count only). Before `git worktree remove`, kill stray `dotnet`/`testhost` and run `dotnet build-server shutdown` to release the directory lock. (Surfaced on PR #5643; matches the PR #5627 worktree-MTP precedent.)

## Regenerating API baselines from a worktree

`/api-baselines` has **no repo-root override** (unlike `/test`) — it deletes and regenerates `Source/**/CompatibilitySuppressions.xml` against the session's cwd, which is the *primary* clone. So when the API-surface change under review lives on a branch checked out in a worktree, invoking the skill regenerates the **wrong** clone's baselines (primary clone's branch, which lacks the change) and produces a meaningless or empty diff.

Run the skill's underlying tool command in the worktree instead — it's the sanctioned action the skill wraps, not a hand-edit (which stays banned, per [`agent-rules.md`](agent-rules.md) → *Agent guardrails* → **Never hand-edit API baseline files**):

1. From the worktree (`Set-Location <worktree>` so `global.json` resolves), run `dotnet pack <Source/Project>/<Project>.csproj -c Release -p:ApiCompatGenerateSuppressionFile=true` for each affected project. `ApiCompatGenerateSuppressionFile=true` overwrites the file with all current suppressions, so the git diff is the minimal delta for the change.
2. Do the `LinqToDB.Internal.*` policy review by hand on the diff (the skill's value-add): any non-`Internal.*` suppression added is a public-contract break needing explicit user sign-off.
3. A **clean** (empty) diff means the change isn't ApiCompat-flagged — no baseline update is needed. (Surfaced on #5639: a shipped public static field→property regen was a no-op — see auto-memory `project_apicompat_field_to_property_noop`.)

## Removing a worktree blocked by file locks

`git worktree remove --force <path>` may fail with `error: failed to delete '<path>': Permission denied` when the worktree was recently built — VBCSCompiler / MSBuild server still holds file handles inside `.build/bin/` even though git's internal worktree registration was successfully dropped (`git worktree list` no longer shows it).

Most removals don't hit this — `git worktree remove --force <path>` succeeds outright once the worktree's own build has finished, no shutdown needed. And **on a shared / multi-worktree machine, do not lead with `dotnet build-server shutdown`**: it is global per-SDK and kills the VBCSCompiler / MSBuild servers that *other* concurrent worktree builds are using, disrupting them mid-build. (This repo is routinely checked out as a dozen parallel worktrees — `git worktree list`.) Reach for `build-server shutdown` only when removal is genuinely lock-blocked **and** no other builds are running.

Cleanup (only when removal is lock-blocked): **`pwsh -NoProfile -File .agents/scripts/remove-worktree-locked.ps1 -Path <worktree-path>`**. It tries `git worktree remove --force` first (the common, no-shutdown case), then `Remove-Item -Recurse -Force` (succeeds in practice even with the lock-reporting process still around), then `git worktree prune`. Build-server shutdown is **off by default** per the shared-machine guard above — pass `-AllowBuildServerShutdown` only once you've confirmed no other worktree builds are running. Don't loop on `git worktree remove --force` — it'll keep failing on the same locked dll.

## Carrying `.agents/` curation across branch switches

`.agents/` skills, docs, hooks, and scripts accumulate on `infra/agents-curation` between weekly merges to `master`. Switching from `infra/agents-curation` to a working branch (feature/\*, issue/\*, etc.) without carrying those changes forward causes the agent to operate against stale `.agents/` state, losing every refinement since the last master merge. The one-line trigger lives in [`agent-rules.md`](agent-rules.md) → *Creating a new branch*.

- **Rule:** when the working branch is not `master`/`release`, the `.agents/` working tree should reflect the latest `origin/infra/agents-curation` state as **uncommitted** modifications. Right after `git switch <target>`, run **`pwsh -NoProfile -File .agents/scripts/carry-curation.ps1`** — it refuses on master/release, fetches the curation branch, checks out its `.agents/`, and leaves the result as *unstaged* working-tree changes.
- **Never commit the carried-over changes on the working branch.** When staging, use `git add <specific paths>` only — never `git add .` / `-A` while curation diffs are present (`git restore --staged .agents/` if they slip in). Before `git merge` / `rebase origin/master`, discard the carry first (`git restore --staged --worktree .agents/`) or the modified `.agents/` tree blocks the merge; re-pull after. Before any `git push`, run **`carry-curation.ps1 -Verify`** — it flags any committed or staged `.agents/` in the `origin/<branch>..HEAD` range.
  - Before any `git push` on a working branch, verify the pushed range carries no `.agents/` diff: `git log origin/<branch>..HEAD --stat -- .agents/` should be empty.
- **Exceptions:** switching to `master` or `release` does **not** carry curation diffs — those branches reflect merged state and should diff cleanly.
- **The only branch where `.agents/` changes are committed is `infra/agents-curation` itself.** Session-end learnings captured via `/session-reflect`, `/audit-agents`, or ad-hoc edits should be applied on the curation branch, not on a working branch. When a session ends with carried-over `.agents/` changes on a working branch and the user wants to keep new edits, the canonical save path is: `git switch infra/agents-curation`, replay the edits there, commit on curation, switch back if more work remains.

## Release-prep orchestration model

When `/release` runs against a `release-prep/<ver>` worktree, the moving parts split between two clones:

| Clone | Branch | Owns |
|---|---|---|
| `<curation-clone>` (curation workspace) | `infra/agents-curation` | `.agents/` skills + scripts; orchestrator state file at `.build/.agents/release-<ver>.json`; per-task plan caches; walk-decisions tracker |
| `../<clone-dir>.release-<ver>` (worktree) | `release-prep/<ver>` | source-tree edits (`Directory.Packages.props`, `.editorconfig`, csproj `VersionOverride` sites, code fixes); per-build outputs under `.build/bin/` |

**Cross-clone calling pattern:** sub-skills that need to run a script from inside the worktree invoke `pwsh -NoProfile -File <curation-clone>\.agents\scripts\<name>.ps1 ...` with an absolute path back to curation. The script's `Get-Location` then yields the worktree, so file-system reads (Directory.Packages.props parsing, source globbing) target the right tree. The PowerShell tool's working directory is set explicitly via `Set-Location <worktree>` before each cross-clone call.

**State files** always under curation (`<curation-clone>\.build\.agents\release-<ver>*.json` etc.) — one canonical location regardless of which clone the agent is operating from. Plan caches written by sub-skills running in the worktree should also write to curation's `.build/.agents/` (pass `-WriteDir <abs-path>` if the script defaults to a relative `.build/.agents/`).

**Disk pressure.** Each Release build of `linq2db.slnx` adds ~9 GB of `.build/bin` output. With a worktree, that's two `.build/bin/` trees (curation's may be empty if no builds run there; worktree's accumulates per release-prep cycle). When iterating against a near-full C: drive, clean per the *Iterative-build gotchas* section in [`agent-rules.md`](agent-rules.md).

**Session resume primer.** Without the orchestration-model context above, the agent rediscovers the dual-clone setup from scratch each session — costs 10-20 turns. Resume prompts for /release should explicitly state: "orchestrator runs from curation; worktree at `<path>`; state file at `<path>`".
