---
name: setup-tests
description: Wire up the local test environment — start docker containers and edit `UserDataProviders.json` to enable a chosen provider set. Runs once per session on user demand so `/test` can skip all docker + config work and dispatch tests immediately.
---

# /setup-tests

User-triggered one-shot setup. Prepares the machine for subsequent `/test` runs by:

1. Resolving the target provider list (user-supplied or proposed defaults).
2. Walking each non-SQLite, non-local-SQL-Server provider through the docker lifecycle (image check → container check → start / setup script).
3. Editing `UserDataProviders.json` (with consent) to enable exactly that provider set in the requested TFM bucket, disabling the rest.
4. Optionally configuring `BaselinesPath` when baselines are expected to move.
5. Reporting the final state so `/test` can trust the setup.

**Never runs tests.** This skill is the setup half that used to live inside `/test` steps 3.2–3.4 and 3.7. `/test` itself assumes `/setup-tests` has already succeeded.

Shared reference material:

- **Test database catalog** (provider → setup script → container → preference): `.claude/docs/test-databases.md`
- **Testing conventions** (TFM buckets, provider variant defaults): `.claude/docs/testing.md`

## When to run

The user invokes `/setup-tests <args>`. Accepted arg shapes:

| Arg shape | Intent |
|---|---|
| `<provider> [<provider> ...]` (e.g. `SqlServer.2016.MS PostgreSQL.18`) | Enable exactly this set in the default bucket (`NET100`). |
| `<providers> --tfm <BUCKET>` / `<providers> for <net9.0\|net10.0\|net462\|net8.0>` | Enable in a specific TFM bucket. |
| `defaults` | Enable the default trio: `SQLite.MS`, `SqlServer.2016.MS` (local non-docker), `PostgreSQL.18`. |
| `for <task description>` (no explicit providers) | Propose a provider set based on the task; confirm with user before acting. |
| empty `/setup-tests` | Ask: numbered menu of default sets (SQLite-only · default trio · Oracle regression · Firebird matrix · "let me pick"). |

`UserDataProviders.json` bucket names match `.claude/docs/testing.md` → **`Providers` is keyed by TFM**: `NETFX`, `NETBASE`, `NET80`, `NET90`, `NET100`. Default bucket is `NET100` (matches `/test`'s playground-at-net10.0 default).

## Steps

### 1. Resolve the provider set

Parse args per the table above. If ambiguous or empty, ask with a numbered menu so the user replies by number. Normalize the provider list to the exact strings `UserDataProviders.json` uses (cross-check `.claude/docs/test-databases.md` → provider IDs); reject unknown entries with a pointer to that file.

Apply provider variant defaults from `.claude/docs/testing.md` when the user names only a family: `Oracle` → `Oracle.<ver>.Managed`; `Access` → `Access.*.Ace.OleDb` + `Access.*.Ace.Odbc`; `SAP HANA` → both `SapHana.Native` and `SapHana.Odbc`. Never enable `Access.*.Jet.*` without an explicit user request; when the user does, warn that the run must be x86-only and no other providers may be enabled alongside Jet in the same bucket (per `testing.md`).

Confirm the final list + target TFM bucket back to the user in one line before proceeding:

> Setting up `NET100` bucket with: `SQLite.MS`, `SqlServer.2016.MS` (local), `PostgreSQL.18`. Proceed?

### 2. Docker lifecycle (non-SQLite, non-local-SQL-Server providers)

For each provider in the confirmed list that needs a container:

1. Look up the container name + image + setup script via `.claude/docs/test-databases.md`.
2. `docker image inspect <image>` — succeeds if the image layer is cached.
3. `docker container inspect <container>` — reports `running` / `exited` / `created` or fails if missing.

Batch the image + container inspects for all providers in a single turn (independent Bash calls).

Decision tree per provider:

- **Container running** — use as-is; note in the report.
- **Container exited/created** — `docker start <container>`. The `track-docker-start` PostToolUse hook records it for session-end cleanup automatically.
- **Container missing OR image missing** — ask the user (single numbered prompt) whether to run `Data/Setup Scripts/<script>.cmd` (creates + starts; may pull a large image). On confirmation, run the script via Bash from `Data/Setup Scripts/`. The hook tracks the `docker start` the script emits.

**Heavy providers** (DB2, Informix, SAP HANA, SAP ASE per `test-databases.md` → *Heavy providers*): prefix every startup/creation prompt with the cost note ("SAP HANA typically takes 5–10 min to warm up and uses several GB of RAM — proceed?"). Never start one silently.

Batch all container starts in a single turn too — they're independent. Collect the final `running` / `started-by-us` / `created-by-us` state per container for the step-5 report.

### 3. UserDataProviders.json consent + edit

Check whether the requested provider set already matches the target bucket's current state (`Providers` array with matching enable markers). If it does, **skip** this step entirely — no edit needed, no consent needed. Report "already configured" in step 5.

Otherwise prompt the user once (per session):

> `UserDataProviders.json` is gitignored and holds your local test config. `/setup-tests` will change the enabled providers in the `NET100` bucket. Options:
>
> 1. **auto-backup** — copy the current file to `.build/.claude/UserDataProviders.json.bak.<timestamp>` before editing. (recommended)
> 2. **skip-backup** — edit in place without a backup copy.
> 3. **cancel** — abort, don't touch the file.
>
> Choose 1, 2, or 3.

On `cancel`, stop and report. On `auto-backup`, copy the file first (single Bash `cp` to `.build/.claude/UserDataProviders.json.bak.<ISO-timestamp>`).

Edit procedure (single atomic `Edit` per bucket):

1. **Read** the file once.
2. **Compute** the new state of the target bucket's `Providers` array off-line: every entry whose string matches one in the confirmed provider list becomes enabled (no leading `"- "`), every other entry becomes disabled (leading `"- "`). Preserve order, whitespace, and comments.
3. **Apply** one `Edit` call whose `old_string` is the exact current `"Providers": [ ... ]` block (opening `[` through closing `]`) and `new_string` is the same block with markers flipped. Do not regex across the whole file, do not split into per-line edits, do not reorder.

When the user asked for multiple TFM buckets (rare — e.g. `--tfm NETFX,NET100`), issue one `Edit` per bucket in the same turn (back-to-back, no intermediate re-reads).

**No restore-on-completion.** Unlike `/test`'s old run-flow, the edit is meant to persist — future `/test` calls rely on the bucket staying in the configured state. Session-end cleanup (via the docker hook) only stops containers, not config. The user reverses config explicitly by re-running `/setup-tests` or restoring the `.bak` file.

### 4. Baselines (optional)

Check `UserDataProviders.json` → `MyConnectionStrings.BaselinesPath`.

- **Set already** — leave alone.
- **Unset** AND the user's task context suggests baselines will move (new SQL emission, new provider path, ongoing PR review against `../linq2db.baselines`) — offer to set it. Propose `c:\\GitHub\\linq2db.bls` (or the `../linq2db.baselines` sibling per `testing.md`); wait for confirmation, then edit the `MyConnectionStrings` block in the same file. Do **not** prompt proactively when the task doesn't obviously touch SQL — it's noise.

### 5. Report

Single fenced block summarizing:

- **Providers enabled** per TFM bucket (e.g. `NET100: SQLite.MS, SqlServer.2016.MS, PostgreSQL.18`).
- **Containers** — one row each: `running` (was up already) / `started` (we started an existing container) / `created` (we ran the setup script).
- **Backup path** (when `auto-backup` was used).
- **Baselines path** — new value if set this run, or "already configured" / "unset (skipped)".
- **Next step** — "Run `/test <filter>` to dispatch tests against this set."

Example:

```
Setup complete:
- NET100 providers: SQLite.MS, SqlServer.2016.MS (local), PostgreSQL.18
- Containers: pgsql18 started (was exited); sql2016 = local non-docker (n/a)
- Backup: .build/.claude/UserDataProviders.json.bak.2026-04-23T14-02-11Z
- Baselines: c:\GitHub\linq2db.bls (set this run)
Next: /test <filter>
```

## Don'ts

- **No test invocation.** `dotnet test` / `dotnet build` never run here. If the user asked for tests too, point at `/test` and stop.
- **No silent file edits.** Every `UserDataProviders.json` edit is gated by `auto-backup` / `skip-backup` / `cancel` consent. The only exception is the "already configured" fast-path where no edit happens.
- **No docker `stop` / `rm`.** Setup only starts or creates containers. Session-end cleanup is handled by the `cleanup-docker-session` SessionEnd hook; the user can also `docker stop` manually.
- **No cross-TFM bulk edits** unless the user explicitly listed multiple buckets. Default is `NET100` only; touching other buckets silently leaves the user in an unexpected state for other TFM runs.
- **No heavy-provider startup without an explicit cost-prefixed prompt.** Per `test-databases.md` → *Heavy providers*.
- **No retry loops on docker failures.** If a container fails to start or the setup script errors, stop and report the failure verbatim — don't chase credentials / ports / networking (per `agent-rules.md` → *Docker containers: start/stop/create only*).
