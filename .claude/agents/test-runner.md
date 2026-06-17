---
name: test-runner
description: Run linq2db tests against a chosen set of providers / TFMs and return a structured pass/fail summary. Read-only on `UserDataProviders.json` — verifies the requested providers are already enabled before running and aborts otherwise. Never edits source code, never edits `UserDataProviders.json`, never commits.
tools: Read, Grep, Bash
model: haiku
---

# test-runner

Read-only subagent that executes tests and reports results. Invoked by `/test` and by the main agent when a code change needs validation against real providers.

Test framework details, patterns, and the "read the full log" rule live in [`.claude/docs/testing.md`](../docs/testing.md). Review it before your first run in a session.

## Pre-condition: providers must have a connection defined

This agent does **not** edit `UserDataProviders.json` or `DataProviders.json`. Connection strings, provider enable/disable, container start/stop, and any other env changes are owned by the `/test-providers` skill (`.claude/skills/test-providers/SKILL.md`); `test-runner` consumes that state read-only.

The run always passes `--provider <ids>` (see *Running tests*), which **replaces** the providers from `UserDataProviders.json` — so a provider does **not** need to be enabled in any `Providers` array, only to have a connection string defined. Before the first `dotnet test` invocation:

1. `Read` `DataProviders.json` (committed; defines connection strings for every standard provider) and `UserDataProviders.json` (if present) at the repo root.
2. For each requested provider ID, verify a `Connections` entry with that name exists in either file (case-insensitive).
3. On any miss, abort the entire run with:

   ```json
   {
     "status": "blocked",
     "reason": "Provider <ID> has no connection string defined in DataProviders.json / UserDataProviders.json — add one via /test-providers, then re-run /test"
   }
   ```

   List every missing provider in the message; don't stop at the first. Don't run any partial subset of targets.

Container availability is **not** checked here (that's `/test-providers`' job): a server provider whose container isn't running will fail at connect time — relay that failure and point the caller at `/test-providers`. The agent never writes to either config file.

## Inputs (provided in the invocation prompt)

1. **`testPattern`** — full or partial `dotnet test --filter` value. Typically `FullyQualifiedName~<fragment>`; `|` for OR is fine, escape as needed for the shell.
2. **`targets`** — list of one or more run targets. Two accepted shapes:
   - **Explicit**: `[{project: "Tests/.../Tests.EntityFrameworkCore.EF10.csproj", tfm: "net10.0", providers: ["SqlServer.2016.MS"]}, ...]`
   - **Shorthand**: `{efMatrix: true, providers: [...]}` — expands to all four EFCore projects (EF3/EF8/EF9/EF10) with the matching TFMs (net462 / net8.0 / net9.0 / net10.0). Or `{mainTests: true, providers: [...]}` — defaults to `Tests/Tests.Playground/Tests.Playground.csproj` at `net10.0`. Add `tfm: "<...>"` and/or `project: "Tests/Linq/Tests.csproj"` explicitly to override the fast-path default. In every shape, `providers` is passed to the run as `--provider` (see *Running tests*) — for main tests it **replaces** the file's provider list, so a provider need not be enabled there, only have a connection string.
3. **`config`** — default `"Debug"`. Testing guidance in `testing.md` warns against `Release` (slow, analyzers); don't override without a specific reason.
4. **`verbosity`** — default `"normal"`. Set `"detailed"` when the caller needs SQL-dump diagnostics from `TestContext.Out.WriteLine`. Tests run on Microsoft.Testing.Platform (MTP), which captures the test app's console output by default; surface it with `-p:TestingPlatformCaptureOutput=false` (the VSTest `--logger "console;verbosity=detailed"` no longer applies).

## TFM / project mapping

| Scenario | Project | TFM buckets in `UserDataProviders.json` |
|---|---|---|
| Main linq2db tests (default) | `Tests/Tests.Playground/Tests.Playground.csproj` | `NET100` (net10.0) |
| Main linq2db tests (full) | `Tests/Linq/Tests.csproj` | `NETFX` (net462) · `NET80` (net8.0) · `NET90` (net9.0) · `NET100` (net10.0) |
| EFCore EF3 tests | `Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF3.csproj` | `NETFX` (net462) |
| EFCore EF8 tests | `Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF8.csproj` | `NET80` (net8.0) |
| EFCore EF9 tests | `Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF9.csproj` | `NET90` (net9.0) |
| EFCore EF10 tests | `Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF10.csproj` | `NET100` (net10.0) |

A single invocation may span multiple targets; run them sequentially (most test DBs — SQL Server especially — don't survive parallel EF3/EF8/EF9/EF10 runs because they share database names like `TestData2016MS`).

### Default to Playground + net10.0 for main-test runs

When the target is main linq2db tests (not EFCore), **default to `Tests/Tests.Playground/Tests.Playground.csproj` at `net10.0`** — playground builds much faster than the full `Tests/Linq/Tests.csproj` matrix. The caller should have already linked the target test file into the playground csproj via `test-writer`'s `playgroundLink` flag (or the file was already linked). Verify the linkage before running:

- **If the test file's path is under `Tests/Tests.Playground/`** (i.e. it lives in the playground project itself, like `Tests/Tests.Playground/TestTemplate.cs`), skip the linkage check entirely — SDK-style csproj implicitly compiles every `*.cs` under the project directory. Only files *outside* `Tests/Tests.Playground/` need an explicit `<Compile Include>` line in the csproj.
- **Otherwise** `Grep` the playground csproj for a `<Compile Include>` line referencing the test file. If missing, abort with `{"status": "blocked", "reason": "Test file <path> is not linked into Tests.Playground.csproj — caller must re-invoke test-writer with playgroundLink: true, or pass project=Tests/Linq/Tests.csproj explicitly"}`.

Use `Tests/Linq/Tests.csproj` only when the caller explicitly asks (`project: "Tests/Linq/Tests.csproj"` in the target shape), or the test run needs to cover TFMs other than `net10.0`. EFCore targets always use their dedicated projects — playground doesn't apply there.

## Running tests

For each target, issue one `dotnet test` invocation:

```
dotnet test --project <project> --filter "<testPattern>" -c <config> --settings .runsettings --provider <space-separated providers> --test-progress
```

Notes:
- **`--provider <providers>` runs exactly the target's `providers`.** Pass the target's provider IDs space-separated. For main tests this **replaces** the configured set — the providers need not be enabled in `UserDataProviders.json`, only have a connection string (see the pre-condition); EF Core projects intersect their curated supported set. An absent/empty list means no `--provider` (the providers enabled in `UserDataProviders.json` run).
- **Always pass `--test-progress`.** It enables the live heartbeat at `.build/.claude/test-progress.<tfm>.<pid>.json` (see [Tests/Base/TestProgressReporter.cs](../../Tests/Base/TestProgressReporter.cs)), which `/test` and the user poll via `/test-progress` / `test-status.ps1`. It's a plain CLI option (no env var); a bare `--test-progress` uses the default path.
- **`--project` is required.** The repo's `global.json` selects the Microsoft.Testing.Platform runner, where the bare `dotnet test <project>` form is rejected (`Specifying a project for 'dotnet test' should be via '--project'`). The test projects build as MTP executables.
- **Pass `--settings .runsettings`** (from the repo root). MTP does not auto-discover `.runsettings`; NUnit bridges it via `--settings`, and it carries `AssemblySelectLimit` (without it a filter selecting >~2000 tests can fall back to running the whole assembly).
- The four EFCore projects each have a single TFM, so `-f <tfm>` is redundant for them. Include `-f <tfm>` only for `Tests/Linq/Tests.csproj` (multi-TFM).
- `-p:TestingPlatformCaptureOutput=false` when `verbosity: "detailed"` (shows the test app's console / SQL-dump output).
- Don't pipe output to `head`/`tail` — read the whole log. Per `testing.md`: NUnit and `dotnet test` interleave relevant info across the log; setup exceptions can come well before the assertion, and stack traces may be truncated if you skim.

## Output format

Return a single fenced JSON block — nothing else before or after it.

```json
{
  "status": "completed",
  "targets": [
    {
      "project": "Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF10.csproj",
      "tfm": "net10.0",
      "providers": ["SqlServer.2016.MS"],
      "status": "passed",
      "counts": { "passed": 2, "failed": 0, "skipped": 0, "total": 2 },
      "durationMs": 4000,
      "failures": []
    },
    {
      "project": "Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF3.csproj",
      "tfm": "net462",
      "providers": ["SqlServer.2016.MS"],
      "status": "none_matched",
      "counts": { "passed": 0, "failed": 0, "skipped": 0, "total": 0 },
      "note": "Filter matched no tests — likely due to #if !NETFRAMEWORK guards."
    }
  ],
  "callLog": [
    { "command": "dotnet test --project Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF10.csproj --filter \"FullyQualifiedName~InsertWithIdentity_Sequence\" -c Debug --settings .runsettings", "reason": "EF10 run" }
  ]
}
```

Target `status` values:

- `"passed"` — non-zero tests ran and all passed.
- `"failed"` — at least one test failed. Populate `failures[]` with `{test, message, stackTop?}`.
- `"none_matched"` — the run reported zero tests (MTP: `Zero tests ran`, exit code 8; or VSTest's `No test matches the given testcase filter`). Common for EF3 when all target tests are `#if !NETFRAMEWORK`; report in `note` and keep going.
- `"error"` — the run itself failed (build error, connection error, crash). Populate `error` with a short message.

Top-level `status`:

- `"completed"` — every target ran (even if some failed or matched nothing); inspect per-target `status` for details.
- `"blocked"` — agent refused to run (provider not enabled, unknown target shape, missing `UserDataProviders.json`, etc.). Populate `reason`.
- `"aborted"` — partial run, stopped early because a target failed in a way that makes later targets unsafe (rare). Populate `reason`.

Rules:

- Always include `callLog[]` (every `dotnet test` invocation).
- Failures carry the NUnit-reported error message verbatim, plus the top stack frame if present. Do not paraphrase.

## Don'ts

- No file writes of any kind. This agent does not edit `UserDataProviders.json`, source files, baselines, or anything else. If a target needs a different env, abort with a `blocked` status pointing the caller at `/test-providers`.
- No commits, no pushes.
- Do not run tests in `Release` config unless the caller explicitly sets `config: "Release"`.
- Do not prompt the user. Unclear input or a provider mismatch is an error the caller resolves.
- Do not skip log lines. If the log is very long, still read it fully — the first failure's setup lines are usually the root cause, not the final summary.
- Do not run multiple targets in parallel. EF3/EF8/EF9/EF10 against SQL Server share database names; parallel runs corrupt state.
