---
name: release-test-matrix
description: Manual test-matrix walkthrough for release prep — LINQPad 5 (.lpx), LINQPad 7+ (nugets), NuGet T4 scaffold (Tests.T4.Nugets), T4 templates (Tests.T4), CLI scaffold, plus the T4 binary prerequisite and per-provider DB init that gate them. Mostly user-driven (Visual Studio is the test runner for T4 templates) but the skill keeps the order straight, surfaces what to check, gates on per-track pass/fail, and accrues "how do I init this provider" / "exact T4 build command" answers into `.agents/docs/release/` for next time. Invoked by `/release` step 4 or directly when running release prep.
---

# /release-test-matrix

## What this skill is (and isn't)

**Is:** the release-only test walkthrough that complements the CI test-all run. CI exercises `Tests/Linq` against the provider matrix; this skill exercises the **artifacts** CI doesn't natively run — LINQPad plugin (`.lpx`), LINQPad nuget driver, NuGet-T4 model generation, T4 templates in Visual Studio, the CLI scaffold tool. All of these are Windows + Visual Studio + LINQPad-driven; can't be automated end-to-end.

**Isn't:**

- Not a substitute for CI test-all. That runs in parallel via `/release-deps` step 7 and any subsequent push. Don't skip it.
- Not for normal-development testing. Routine test runs go through `/test` against `Tests/Linq` or `Tests/Tests.Playground`.
- Not where `UserDataProviders.json` is edited. Release-testing artifacts (T4, CLI, LINQPad) read connection strings from settings directly and do **not** require providers to be enabled in `UserDataProviders.json`.

## When to run

- During release prep as task 4 (called by `/release` orchestrator).
- Manually when validating a LINQPad / T4 / CLI artefact outside a release.

## Required reading

- [`.agents/docs/release/linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md) — accrued LINQPad smoke + targeted-change rows.
- [`.agents/docs/release/provider-db-init.md`](../../docs/release/provider-db-init.md) — accrued per-provider container + DB-init invocations.
- [`.agents/docs/release/external-repos.md`](../../docs/release/external-repos.md) — local-nuget-server folder + other user-specific paths.

## Procedure overview

Eight tracks under task 4. The orchestrator's checklist enumerates them as `4.0`–`4.8`. Each is independently skippable (`[-]`), but the skill warns when a track has prior issues recorded in `linqpad-test-checklist.md` for this kind of change.

**Order matters for 4.0 → 4.5 → 4.6 → 4.7** (T4 binaries are a prerequisite for T4 / NuGet-T4 / CLI). 4.1 (DB init) gates all tracks except 4.0. The LINQPad tracks (4.3 / 4.4 / 4.8) can run in parallel with T4 tracks.

After picking a track:

1. Print the track's instructions (each section below).
2. Ask the user to do the manual step.
3. Capture pass/fail/notes from the user.
4. If anything went wrong in a way that suggests doc gaps, record + session-reload (see **First-run learning** at the bottom).
5. Update state via `release-state.ps1 -Action update -Version <ver> -TaskId 4.<n> -Status <done|skipped> -Annotation <text>`.

## Track 4.0 — T4 binary prerequisite

T4 templates under `Tests/Tests.T4/` and `Tests/Tests.T4.Nugets/` consume binaries produced by a Debug build targeting net462. Without this, T4 templates fail with missing-assembly errors.

**Steps:**

1. From the repo root, run the documented T4 build command. **First run:** ask the user for the exact command — record in [`linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md) under **T4 build prerequisite** and prompt session-reload.
   - Likely shape: `dotnet build linq2db.slnx -c Debug` (slnx-level build emits net462 outputs alongside other TFMs).
   - Single-TFM net462 via `-f net462` is rejected on slnx files; per-project would be needed.
2. Confirm the expected output exists under `.build/` (look for `bin/Debug/net462/`-flavored outputs of the involved projects).
3. Ask user to confirm. On non-trivial output (e.g. unexpected build error), surface the first 30 lines and ask whether to abort.

Tick `4.0` once user confirms the build succeeded.

## Track 4.1 — Provider container + DB init

Release testing reads connection strings from settings — **no `UserDataProviders.json` enable is needed**. The only env prep is: start the docker container + run the matching setup script(s) from `Data\Setup Scripts\` (in the linq2db repo).

**Steps:**

1. Ask the user which providers to refresh, or `all`. Default: the standard set the project's release-runner usually tests (SQL Server, PostgreSQL, MySQL, Oracle, SQLite — confirm on first run; record in [`linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md) under **Standard release-test providers**).
2. For each provider:
   a. `docker ps -a --filter name=<container>` to check state. If not present, **stop and ask** — container creation is out of scope (user-owned).
   b. If stopped: `docker start <container>`. (Per `agent-rules.md` — `docker start` is in-scope for the skill; container *configuration* is not.)
   c. Run the recorded setup-script invocation from [`provider-db-init.md`](../../docs/release/provider-db-init.md):
      - **First run for a provider:** read the invocation from `Data\Setup Scripts\` in the linq2db repo and ask the user to confirm or correct. Save to `provider-db-init.md`, prompt session-reload, retry.
3. Ask user to confirm databases initialized cleanly.

Tick `4.1` once every selected provider's container is started + DB initialized.

## Track 4.2 — DB2 iSeries decision

This is a single gate question, not a workflow. DB2 iSeries provider often lags or hits namespace incompat with linq2db's current `LinqToDB.Internal.*` surface — sometimes it ships for a release, sometimes not.

**Ask the user:**

> _"Should DB2 iSeries support be enabled or disabled in the LINQPad build for this release? If enabled and incompatible, LINQPad will throw at runtime when a DB2 iSeries connection is used — detectable by smoke-test."_

Record the decision in release state for 4.3 / 4.4 to consume:

```
pwsh -NoProfile -File .agents/scripts/release-state.ps1 -Action update -Version <ver> -TaskId 4.2 -Status done -Annotation "DB2 iSeries: <enabled|disabled>"
```

Configuration knob location for the LINQPad build: ask user on first run, record in [`linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md) under **DB2 iSeries toggle**.

Tick `4.2` once recorded.

## Track 4.3 — LINQPad 5 (.lpx)

LINQPad 5 is the .NET Framework version; ships a `.lpx` plugin bundle.

**Steps:**

1. Build the lpx: ask user for the exact command. Typical shape is `dotnet build Source/LinqToDB.LINQPad/LinqToDB.LINQPad.Pack.csproj -c Release` or a separate Pack csproj invocation — record on first run in [`linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md) under **LINQPad 5 lpx build**.
2. Print the artefact path (expected: `.build/lpx/linq2db.LINQPad.lpx`).
3. Ask user to install in LINQPad 5 (LINQPad → Help → Install Driver → "Browse" → point at the .lpx).
4. Smoke-test via the checklist in `linqpad-test-checklist.md` → **LINQPad 5 (.lpx) smoke**. Default rows:
   - LINQPad starts with no error dialog.
   - linq2db connection wizard appears under Add Connection.
   - Connect to one provider (default: SQL Server) — schema browsable, sample query runs.
   - Run a simple LINQ query → expected results.
5. If release has LINQPad-related changes, run the targeted-change rows from `linqpad-test-checklist.md` → **Targeted-change rows** for this release version.

Capture pass / fail / partial. On failure: ask user to describe; record a row in the release's targeted-change section (so next release tests for the same regression).

Tick `4.3` on pass. On fail, leave `[~]` and dispatch back to `/release` for next-step decision (likely: fix on prep branch, re-run from 4.0).

## Track 4.4 — LINQPad 7+ (nugets)

LINQPad 7+ runs on modern .NET (net8/9/10). Doesn't use `.lpx` — uses the linq2db driver via NuGet from a user-local NuGet test feed.

**Steps:**

1. Build all linq2db nugets: ask user for the exact command. Typical: `dotnet pack linq2db.slnx -c Release -o .build/.agents/release-<ver>-nugets/`. Record on first run.
2. Identify the user's local NuGet test feed: read [`external-repos.md`](../../docs/release/external-repos.md) → **User-specific paths** → `user-local.nuget-server`. **First run:** ask the user for the path. If they don't have one, give them a setup pointer (LINQPad 7+ → Edit → Preferences → NuGet → Custom Package Sources → add a local-folder source) and ask for the path once they have it. Save to `external-repos.md`, prompt session-reload.
3. Copy the built `.nupkg` files into the recorded folder.
4. Ask user to ping/refresh the local server if it needs it (varies by setup — record any specifics in `external-repos.md`).
5. Ask user to install the linq2db driver in LINQPad 7+ from the local feed.
6. Run the LINQPad 7+ smoke rows from `linqpad-test-checklist.md`.
7. Run targeted-change rows if applicable.

**If user reports issues requiring code changes during 4.4:**
- The current local nuget version is `<X>.<Y>.<Z>-local.<N>`. After fixes, rebuild with `-local.<N+1>` to invalidate LINQPad's NuGet cache. Confirm with the user before rebuilding.
- Track each iteration in `state.tasks.4.4.annotation`.

Tick `4.4` on user-confirmed pass.

## Track 4.5 — NuGet T4 scaffold (Tests.T4.Nugets)

Tests.T4.Nugets validates T4 templates that consume linq2db.t4models from a published-ish nuget (sourced from the local feed during testing).

**Steps:**

1. Set the local nuget version in `Tests/Tests.T4.Nugets/Directory.Packages.props`. Find the property (likely `<Version>` in the props' PropertyGroup) and update to the just-built local version (e.g. `6.3.0-local.1`). Confirm the exact property name on first run (record in [`linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md) under **Tests.T4.Nugets version property**).
2. **Copy the rebuilt `.nupkg` files to `<repo>/.build/package/release/`** — that's the local-folder source `Tests/Tests.T4.Nugets/nuget.config` points at (the `linq2db-testing` source maps `linq2db*` patterns to that folder via `packageSourceMapping`). The user's remote nuget feed (Track 4.4) is **not** the source for Track 4.5 — pushing there does nothing for this restore. After the copy, run `dotnet restore Tests/Tests.T4.Nugets/Tests.T4.Nugets.slnx --force` to pull the new versions.
3. Ask user to open the test solution in Visual Studio and run all T4 templates from the `Tests.T4.Nugets` project **except `t4model`**.
4. After those complete, ask user to reload the solution (to drop the T4 cache) and run the `t4model` template.
5. Check generated-file diff in `Tests/Tests.T4.Nugets` working tree. Expected: small or zero diff. Unexpected diff → investigate (likely a code change that broke scaffold output; surfaces a release-blocker).

Tick `4.5` on a clean diff or user-confirmed-expected diff.

## Track 4.6 — T4 templates (Tests.T4)

Same idea as 4.5 but using the in-repo linq2db source (not from nuget). Excludes the `cli/` subfolder — that's track 4.7.

**Steps:**

1. Ask user to open the test solution in Visual Studio.
2. Run every T4 template under `Tests/Tests.T4/` **except templates under `Tests/Tests.T4/Cli/`**.
3. Check generated-file diff. Expected: zero or small. Unexpected → investigate (often points at the DB init step in 4.1 being stale).

If an unexpected diff is from a provider whose DB init steps look correct: prompt user to teach the init details — update `provider-db-init.md`, prompt session-reload, retry 4.1 → 4.6 for that provider.

Tick `4.6` on a clean diff or user-confirmed-expected diff.

## Track 4.7 — CLI scaffold

The CLI tool (`linq2db.cli`) scaffolds database models. Two ways to exercise it:

### Recommended — automated, parallel via `release-test-cli-scaffold.ps1`

Mirrors every `RunCliTool()` call in `Tests/Tests.T4/Cli/*.tt` and runs them in parallel against the locally-built CLI artifact (`<RepoRoot>/.build/publish/LinqToDB.CLI/Debug/net9.0/win-x64/dotnet-linq2db.dll`). No nuget install needed; depends only on track 4.0's Debug build.

**Steps:**

1. Verify the CLI artifact exists (track 4.0 must have run):
   ```
   Test-Path <RepoRoot>/.build/publish/LinqToDB.CLI/Debug/net9.0/win-x64/dotnet-linq2db.dll
   ```
2. Dry-run to see the full plan (114 raw rows; ~107 after default skips):
   ```
   pwsh -NoProfile -File .agents/scripts/release-test-cli-scaffold.ps1 -DryRun
   ```
3. Ask the user about provider availability — at minimum confirm:
   - **Azure / Azure.MI** — skipped by default; only enable if the user has an external Azure SQL DB.
   - **SqlCe** — needs SQL Server Compact 4.0 runtime at `c:\Program Files\Microsoft SQL Server Compact Edition\v4.0\Private\System.Data.SqlServerCe.dll`. Skip via `-SkipProviders SqlCe` if not installed.
   - Any docker container the user wants to skip (e.g. `-SkipProviders SapHana,Informix`).
4. Sanity-check on one row first:
   ```
   pwsh -NoProfile -File .agents/scripts/release-test-cli-scaffold.ps1 -Templates Default -Providers SQLite
   ```
   Confirm exit 0 + zero `git diff` under `Tests/Tests.T4/Cli/Default/SQLite/`.
5. Run the full matrix:
   ```
   pwsh -NoProfile -File .agents/scripts/release-test-cli-scaffold.ps1
   ```
   Defaults: 6 in parallel, 120s per-invocation timeout. Tune via `-Parallelism` / `-TimeoutSec`.
6. Check generated-file diff across all `Tests/Tests.T4/Cli/<Template>/<Key>/` folders.

The script does **not** install `linq2db.cli` as a global tool — it invokes the already-built DLL via `dotnet <dll> scaffold ...`, which matches what the `.tt` templates do under `Process.Start("dotnet", ...)` from `Tests/Tests.T4/Cli/CLI.ttinclude`.

### Fallback — manual install + Visual Studio

Use this only when the user wants to validate the CLI as a published nuget (catches packaging issues the in-place DLL invocation won't):

1. Install the CLI tool from local feed:
   ```
   dotnet tool install --global linq2db.cli --version <X>.<Y>.<Z>-local.<N> --add-source <user-local.nuget-server>
   ```
   (If already installed: `dotnet tool update --global linq2db.cli ...`.)
2. Ask user to run CLI templates under `Tests/Tests.T4/Cli/` from Visual Studio.
3. Check generated-file diff.

### F# baselines (#1553)

The CLI also emits F# (`--target-language f#`). Its baselines live in separate compile-gate projects (`Tests/FSharp.Scaffold/`), **not** the `Tests/Tests.T4/Cli/` tree, and regenerate via their own script — the F# target forces one file per provider with a different option set, so it doesn't fit `release-test-cli-scaffold.ps1`'s C# row matrix. Both nullness modes are covered: `--nrt true` (`Tests.FSharp.Scaffold.fsproj`, `<Nullable>enable</Nullable>`) and `--nrt false` (`Tests.FSharp.Scaffold.NoNrt.fsproj`, `<Nullable>disable</Nullable>`).

1. Regenerate both modes (needs the SQLite file DB plus the PostgreSQL + SQL Server containers):
   ```
   pwsh -NoProfile -File Tests/FSharp.Scaffold/generate.ps1
   ```
2. Validate — build both compile-gate projects; a build failure **is** the generator regression:
   ```
   dotnet build Tests/FSharp.Scaffold/Tests.FSharp.Scaffold.fsproj -c Debug
   dotnet build Tests/FSharp.Scaffold/Tests.FSharp.Scaffold.NoNrt.fsproj -c Debug
   ```
3. Confirm zero `git diff` under `Tests/FSharp.Scaffold/` (both the `<Provider>/` and `NoNrt/<Provider>/` trees).

Tick `4.7` on a clean diff or user-confirmed-expected diff.

## Track 4.8 — Targeted-change retest

If the release contains changes to the LINQPad driver, scaffold library, or provider surface, prompt the user about specific test rows that should be run beyond the smoke checklists.

**Steps:**

1. Summarize the changes the release contains in those areas — read `git log origin/release..HEAD --oneline -- Source/LinqToDB.LINQPad Source/LinqToDB.Scaffold Source/LinqToDB.CLI 'Source/LinqToDB/Internal/DataProvider'`.
2. For each non-trivial change, ask the user: "what should we test for this in LINQPad / scaffold / CLI?". Capture the answer as a row under [`linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md) → **Targeted-change rows** for this release version.
3. Run each captured row through the relevant LINQPad / T4 / CLI surface (typically returns to 4.3 / 4.4 / 4.6 / 4.7 with a specific scenario).

If no LINQPad / scaffold / provider-surface changes are in the release: mark `4.8` `[-]` skipped.

Tick `4.8` on user-confirmed pass.

## First-run learning

Whenever a step asks the user a question the skill couldn't answer from existing docs:

1. Record the answer in the right doc:
   - Provider DB init invocation → [`provider-db-init.md`](../../docs/release/provider-db-init.md).
   - Build command (T4 prerequisite, lpx pack, nuget pack) → [`linqpad-test-checklist.md`](../../docs/release/linqpad-test-checklist.md).
   - User-specific paths (local nuget server) → [`external-repos.md`](../../docs/release/external-repos.md).
   - Anything not fitting → new section in `linqpad-test-checklist.md`.
2. After saving, end the turn with:
   > 📌 Reload session to pick up `.agents/docs/release/` edits before continuing the test matrix.
3. The user reloads and re-invokes the orchestrator; state resume picks up at the same track.

## Don'ts

- Do **not** edit `UserDataProviders.json` or call `/test-providers` from this skill. Release-testing artefacts don't read that file.
- Do **not** auto-run `Data\Setup Scripts\` invocations the skill hasn't seen before. The first run for an unfamiliar provider always asks the user. After confirmation + record, subsequent runs are agent-executable.
- Do **not** mix tracks 4.3 / 4.4 results — `.lpx` (LINQPad 5) and nugets (LINQPad 7+) are independent. Failing one doesn't fail the other.
- Do **not** push the prep branch from inside this skill. Test results aren't committed; they're recorded as state-file annotations. The push semantics rule in `/release` Conventions still applies if the user explicitly asks for a commit.
- Do **not** skip 4.0 (T4 build prerequisite) — every T4-consuming track depends on it. If user wants to skip the T4 / CLI tracks entirely, mark 4.0 `[-]` too.
