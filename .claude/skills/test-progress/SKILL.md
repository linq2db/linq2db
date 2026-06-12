---
name: test-progress
description: Enable, disable, or check the live test-progress heartbeat for linq2db test runs. Toggles the LINQ2DB_TEST_PROGRESS environment variable session-wide via .claude/settings.local.json → env, so dotnet test runs (via /test, test-runner, or ad-hoc) emit a JSON heartbeat (current test, completed/total, pass/fail tally) that can be polled mid-run. Use when the user says "/test-progress", "enable test progress/trace", "watch the test run", "how far along are the tests".
---

# /test-progress

Toggle and inspect the live progress heartbeat written by the test assembly during a run. Backed by [Tests/Base/TestProgressReporter.cs](../../../Tests/Base/TestProgressReporter.cs); reader docs in [`.claude/docs/testing.md`](../../docs/testing.md) → **Monitoring a long run**.

## How it works

The reporter is **opt-in**: it writes nothing unless `LINQ2DB_TEST_PROGRESS` is set to a truthy value in the process running `dotnet test`. This skill flips that variable in `.claude/settings.local.json` → `env`, which Claude Code injects into every Bash-tool subprocess (and their children, e.g. the NUnit test host). The `dotnet test` command itself is unchanged, so the existing `Bash(dotnet test *)` allowlist still matches (no new permission prompts).

**On = `"1"`, off = `"0"` (not key-removal).** Claude Code applies an added or *changed* env value to the running session immediately (verified), but does **not** un-apply a *removed* key until restart. So `off` sets the value to `"0"` — a present-but-falsy value the reporter treats as disabled — which propagates right away. Removing the key would leave the trace silently on for the rest of the session.

Scope: this affects test runs **Claude** launches via the Bash tool. A run the user starts in their **own** terminal won't inherit it — they set `LINQ2DB_TEST_PROGRESS=1` there themselves (see `testing.md`).

## Arguments

| Arg | Action |
|---|---|
| _(none)_ / `status` | Report whether the trace is on, and show the latest heartbeat if a run is active or recent. |
| `on` / `enable` | Set `env.LINQ2DB_TEST_PROGRESS = "1"` in `settings.local.json`. |
| `on <path>` | Same, but set the value to `<path>` (a directory or `*.json` file) to redirect the heartbeat location. |
| `off` / `disable` | Set `env.LINQ2DB_TEST_PROGRESS = "0"` (immediate effect — see below; do **not** remove the key). |

## Steps

### `on` / `enable`

1. `Read` `.claude/settings.local.json` at the repo (worktree) root.
2. Ensure a top-level `"env"` object exists; set `"LINQ2DB_TEST_PROGRESS"` to `"1"` (or the `<path>` argument if given). Use `Edit` for a minimal change that preserves `permissions` and any other keys. If `env` already has the key with the same value, report "already enabled" and stop.
3. Confirm to the user: trace enabled, value, and that it applies to the next Claude-run `dotnet test`. Remind that the file lands at `.build/.claude/test-progress.<tfm>.<pid>.json` (or the custom path) and is polled with `/test-progress` or `test-status.ps1`.

### `off` / `disable`

1. `Read` `.claude/settings.local.json`.
2. Set `env.LINQ2DB_TEST_PROGRESS` to `"0"` (add the `env` object / key if missing). **Do not remove the key** — a removed key isn't un-applied until restart, but a changed value propagates immediately. If it's already `"0"` (or any falsy value), report "already disabled" and stop.
3. Confirm to the user: trace disabled, effective for the next run. Existing `.build/.claude/test-progress.*.json` files are left as-is (gitignored scratch).

### `status` (default)

1. `Read` `.claude/settings.local.json`; report the trace state from `env.LINQ2DB_TEST_PROGRESS` — **on** for a truthy value (`1`/`true`/`on`/`yes` or a path), **off** when absent or falsy (`0`/`false`/`off`/`no`/empty). Show the raw value when it's a custom path.
2. Run the summary helper for the most recent run:

   ```
   pwsh -NoProfile -File .claude/scripts/test-status.ps1
   ```

   Relay its one-line output. If it reports no heartbeat file, say so. Add `-Raw` only if the user wants the full JSON.

## JSON-editing notes

- `settings.local.json` is small, gitignored, and user-owned. Edit it directly — the user invoking this skill *is* the consent. Do not route through `/update-config` for this single toggle.
- Make the **minimal** edit: insert or change only the `env.LINQ2DB_TEST_PROGRESS` line. Never reorder or rewrite the `permissions` block.
- This skill is the one place that toggles this variable. If the user wants other harness env vars managed, point them at `/update-config`.

## Don'ts

- Don't prefix `dotnet test` commands with the variable, and don't ask `test-runner` to — the whole point of the settings-`env` approach is to keep the command (and its allowlist match) unchanged.
- Don't edit `Tests/Base/TestProgressReporter.cs` or any test source from here — this skill only flips the environment toggle.
- `/test` auto-enables the trace for **long / full-suite** runs (its step 3.1a) and monitors the heartbeat — that's expected. For **short** single-test runs the trace stays as-is; don't flip it on for those unless the user asks.
