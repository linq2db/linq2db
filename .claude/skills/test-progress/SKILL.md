---
name: test-progress
description: Report the live test-progress heartbeat for linq2db test runs (read-only). The heartbeat is always written by Claude-launched runs (test-runner passes --test-progress), so this skill reads the latest .build/.claude/test-progress.*.json and relays a one-line status — current test, completed/total, pass/fail tally. Use when the user says "/test-progress", "how far along are the tests", "watch the test run", "test status".
---

# /test-progress

Report the live progress heartbeat written by the test assembly during a run. Backed by [Tests/Base/TestProgressReporter.cs](../../../Tests/Base/TestProgressReporter.cs); reader docs in [`.claude/docs/testing.md`](../../docs/testing.md) → **Monitoring a long run**.

## How it works

The heartbeat is opt-in via the **`--test-progress`** command-line option on the test executable. Claude-launched runs **always** pass it — `test-runner` appends `--test-progress`, and `/test` step 3.1a relies on that for long runs — so a heartbeat file is normally present for the active or most-recent run. There is **no env var and nothing to toggle**; this skill only *reads* the heartbeat.

The file lands at `.build/.claude/test-progress.<tfm>.<pid>.json` (one per TFM / process). For a run the user starts in their own terminal, they pass `--test-progress` themselves (see `testing.md`).

## Steps (default / `status`)

1. Run the summary helper for the most recent run:

   ```
   pwsh -NoProfile -File .claude/scripts/test-status.ps1
   ```

   Relay its one-line output (state, completed/total, pass/fail/skip, rate, elapsed, ETA, current test). If it reports no heartbeat file, say so — either no run has started, or the run was launched without `--test-progress` (unusual for a Claude-launched run).
2. Add `-Raw` if the user wants the full JSON, or `-Path <file>` to target a specific run's heartbeat.
3. For a parallel per-provider run, each process writes its own `.build/.claude/test-progress.<tfm>.<pid>.json`; point `-Path` at the one you want to watch.

## Don'ts

- Don't toggle env vars or edit `settings.local.json` — the heartbeat is controlled by the `--test-progress` option, which `test-runner` always passes. There is no on/off switch to flip.
- Don't edit `Tests/Base/TestProgressReporter.cs` or any test source from here — this skill only reads the heartbeat.
- Don't launch test runs from here — that's `/test`. This skill reports the status of a run already in progress or finished.
