---
name: test-providers
description: Configure the local test environment — enable / disable test-provider entries in `UserDataProviders.json` per TFM bucket, manage the docker containers those providers need (start / stop / setup-script run), and reset the file from `UserDataProviders.json.template`. Owns every edit to `UserDataProviders.json` and every `docker start` / `docker stop` call across the `.claude/` toolset; `/test` and `test-runner` consume the resulting state read-only.
---

# test-providers

User-triggered workflow for managing the local test environment that `/test` runs against. The single concern of this skill is making `UserDataProviders.json` and the docker containers behind it match what the user wants — it never runs tests and never builds anything.

Shared reference material:

- **Test database catalog** (provider IDs → setup script → container → image → preference): [`.claude/docs/test-databases.md`](../../docs/test-databases.md)
- **`UserDataProviders.json` shape**: see `UserDataProviders.json.template` at the repo root and the **Test Database Configuration** section of [`.claude/docs/testing.md`](../../docs/testing.md)

## When to run

Only when the user explicitly invokes `/test-providers` — typically before a `/test run …` to make sure the right providers are enabled and their containers are up, or after a session to stop containers the user no longer needs. Do not invoke this skill from inside `/test`, from `test-runner`, or from any other skill — the boundary is intentional, and `/test` is allowed to fail when the env doesn't match (the user re-runs `/test-providers` to fix it).

## Accepted arg shapes

`/test-providers` parses one of the shapes below from the args. On ambiguity, stop and ask with a single numbered prompt.

| Arg shape | Intent |
|---|---|
| empty | Show current per-TFM enabled-provider set + container statuses for any container referenced by an enabled provider, then ask what to do. |
| `<provider> [<provider>…]` (e.g. `SQLite.MS PostgreSQL.18`) | **Set** mode. Mark exactly the listed providers enabled in the affected TFM bucket(s) and disable every other provider in those buckets. Start the corresponding containers if needed. **Default scope is `NET100` only** — see *Bucket scope* below to widen. |
| `add <provider> [...]` | **Additive** mode. Mark the listed providers enabled in the affected TFM bucket(s) without changing the rest. Start containers as needed. **Default scope is `NET100` only.** |
| `remove <provider> [...]` | **Subtractive** mode. Mark the listed providers disabled in the affected TFM bucket(s). Does not stop containers (user does that explicitly via `stop`). **Default scope is `NET100` only.** |
| `stop` / `stop <container> [...]` | Stop all (or named) containers we have records of from this session. No `UserDataProviders.json` edit. |
| `reset` | Restore `UserDataProviders.json` from `UserDataProviders.json.template` after explicit confirmation. Does not touch containers. |

Provider IDs are the strings under each TFM bucket's `Providers` array (e.g. `SQLite.MS`, `PostgreSQL.18`, `SqlServer.2016.MS`). Container names match the **Container** column of `test-databases.md`.

### Bucket scope

Every Set / add / remove invocation accepts an optional `in <bucket>[,<bucket>...]` clause anywhere in the args. The clause selects which TFM bucket(s) of `UserDataProviders.json` get edited:

- **No clause** → `NET100` only (the user's default workflow target).
- **`in <bucket>[,<bucket>...]`** → the named buckets only (`NETFX`, `NET80`, `NET90`, `NET100`).
- **`in all`** → every TFM bucket present in the file.

Examples:

- `/test-providers SQLite.MS PostgreSQL.18` — edits `NET100` only.
- `/test-providers in NETFX,NET100 SQLite.MS Firebird.5` — edits `NETFX` and `NET100`, leaves `NET80` and `NET90` alone.
- `/test-providers add Oracle.12 in all` — adds `Oracle.12.Managed` (after family-rule normalisation) to every bucket.

The clause may appear before or after the provider list; parse it once, then strip it before family-rule normalisation runs on the remaining args.

## Provider name shortcuts and family rules

Bare-family / version-only inputs are normalised to fully-qualified provider IDs before any edit. The family-rule table, bare-family version resolution, override and exclusion tables, and sticky-entry rule live in [`test-databases.md`](../../docs/test-databases.md) → **Provider name resolution**. Step 1 (Resolve intent) calls those rules; the rest of this skill consumes the normalised output.

## Permission-prompt discipline

Every Bash call is allowlist-matched as one opaque string, so:

- One Bash call per command. No `&&` / `||` / `;` chaining; no shell control flow. (Also enforced by the rules in `.claude/docs/agent-rules.md`.)
- Batch independent inspects in a single assistant turn (multiple parallel Bash tool calls), not one chained string.
- Setup scripts under `Data/Setup Scripts/<name>.cmd` must be run from that directory — issue `cd "Data/Setup Scripts" && <script>.cmd` is **not** allowed; use the script's documented invocation form via a single command (e.g. invoke through `pwsh -NoProfile -File ...` if a sequence is required, or just `Data/Setup\ Scripts/<script>.cmd` from the repo root if the script supports it). When the script truly requires a `cd` first, wrap the two-step sequence in a small pwsh under `.build/.claude/` rather than chaining.

## Steps

### 1. Resolve intent

Parse args in this order:

1. **Mode token.** First positional token decides the mode: `add`, `remove`, `stop`, `reset`, or (anything else) Set mode.
2. **Bucket-scope clause.** Look anywhere in the remaining args for `in <bucket>[,<bucket>...]` or `in all`. Strip it from the args; record the resolved bucket list (default `["NET100"]` when absent). See *Bucket scope* in the **Accepted arg shapes** section.
3. **Family-rule normalisation** (Set / add / remove only). For each remaining provider token, apply the **Family-variant shortcuts** and **Bare-family version** rules per [`test-databases.md`](../../docs/test-databases.md) → **Provider name resolution**. Keep an internal record `rewrites: [{from, to, reason}]` of every token that changed — this drives the *Normalised inputs* block in step 4. Tokens that are already fully qualified (or match an exclusion list and were typed explicitly) record `from === to` and don't appear in the rewrites list.
4. **Ambiguity check.** If a bare token could plausibly be a provider ID or a container name (rare — most container names like `pgsql18` don't collide with provider IDs), stop and ask. Do not guess.

After this step, the agent has: `{ mode, buckets[], providers[], rewrites[] }`. Subsequent steps consume this normalised structure, never the raw user input.

### 2. Read current state

Always read state before computing any change. Batch the calls in one turn:

1. **`Read` `UserDataProviders.json`** at the repo root. Parse the per-TFM `Providers` arrays and `MyConnectionStrings.BaselinesPath` (string or absent).
2. **Snapshot container state** with a single `docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Image}}"` call. The status column (`Up …` / `Exited …` / `Created`) and image column give everything needed to decide `running` / `will-start` / `will-create` for every container referenced by the target enabled-provider set. Per `agent-rules.md` → *Docker containers: start/stop/create only*, **do not** call `docker container inspect` or `docker image inspect` — they're outside the agent's container scope.
3. Build an in-memory map: `{ container -> { exists, status, image } }`. Containers not present in the `docker ps -a` snapshot are treated as missing (`will-create`).

For empty args (status mode), report the snapshot and ask what to do; don't proceed past this step until the user picks an arg shape.

### 3. Compute target state

#### Affected TFM buckets

Use the `buckets[]` list resolved in step 1. Default is `["NET100"]`; widened only if the user passed an `in …` clause. Buckets not in the list are completely untouched — no read, no edit, not even a no-op replacement of their `Providers` array.

#### Provider-set / add / remove modes

Per affected TFM bucket:

- **Set mode** — every entry whose ID is in the (normalised) target list becomes enabled (drop a leading `- ` if present); every other entry in the same bucket becomes disabled (add a leading `- ` if absent), **with one exception: `TestNoopProvider` is sticky and is skipped by the disable sweep — see [`test-databases.md`](../../docs/test-databases.md) → *Sticky entries***.
- **Add mode** — listed entries become enabled; every other entry is left untouched.
- **Remove mode** — listed entries become disabled; every other entry is left untouched. The sticky rule for `TestNoopProvider` does **not** apply to Remove mode — if the user explicitly types `remove TestNoopProvider`, honour it (it's an explicit choice, not a sweep).

Preserve order, formatting, comments, and unrelated buckets exactly. Don't restructure, don't sort, don't add or remove array entries — only flip the enable/disable marker on existing lines. The file is JSONC (JSON with comments); single-line `//` comments and trailing commas are valid and must be preserved verbatim.

#### `DefaultConfiguration` is immutable

The per-bucket replacement edit (step 5b) covers **only** the `"Providers": [ ... ]` array. The surrounding bucket keys — `BasedOn`, `DefaultConfiguration`, and any others — stay byte-for-byte identical. Do not read these to make decisions, do not write them, do not re-emit them. If a bucket has no `DefaultConfiguration` today, do not add one. If it has `"DefaultConfiguration": "SQLite.MS"` today, do not change it to anything else.

#### Missing provider IDs

If a requested provider ID isn't present in the current bucket's `Providers` array, surface it explicitly in step 4 and ask the user whether to copy from the template (which has the full provider list) or skip the missing one. Do not auto-insert.

#### Container plan

For every container that the **target** enabled-provider set references (post-edit), compute one of:

- `running` — container exists and `inspect` returned `running`. No action.
- `will-start` — container exists and status is `exited` / `created`. Action: `docker start <name>`.
- `will-create` — container does not exist. Action: run `Data/Setup Scripts/<script>.cmd` (which creates and starts it; pulls the image if not cached).
- `image-pull-needed` — same as `will-create` but the image isn't cached locally either; setup script will pull. Surface the cost note when it's a heavy provider (DB2 / Informix / SAP HANA / SAP ASE — see **Heavy providers (ask first)** in `test-databases.md`).

Local non-docker SQL Server providers (`SqlServer.2005` … `SqlServer.2016` per `test-databases.md` → **Local (non-docker) SQL Server**) need no container action — note in the plan as `local-instance`.

### 4. Confirm with user

Show a single confirmation block in three sections:

**Normalised inputs** (only when step 1 produced rewrites — omit the section entirely otherwise):

```
Oracle      → Oracle.23.Managed       (bare-family + family rule)
MySql       → MySqlConnector.8.0       (bare-family + family rule)
SqlServer   → SqlServer.2019.MS        (bare-family + family rule + override)
Oracle.12   → Oracle.12.Managed        (family rule)
```

This is the user's chance to spot an unintended rewrite before any edit applies. If the user objects, restart from step 1 with their corrected input — do not patch the rewrite locally.

**`UserDataProviders.json`** — per affected TFM bucket, list:

```
NET100  enable: PostgreSQL.18
        disable: Oracle.11.Native, Oracle.12.Native
        sticky: TestNoopProvider (kept enabled)
        no-change: 14 entries
```

**Containers** — one row each:

```
pgsql18   running         (no action)
sql2022   will-start      (docker start)
hana2     will-create     (run Data/Setup Scripts/saphana2.cmd)
                          ⚠ heavy: ~5–10 min startup, very high RAM (per test-databases.md)
```

Also include:

- The backup target path: `.build/.claude/UserDataProviders.json.bak.<ISO-timestamp>`.
- Whether `BaselinesPath` is currently set; if unset, ask in this same prompt whether to set it (propose `c:\\GitHub\\linq2db.bls` per `testing.md`'s **Enabling baselines locally**) — same numbered list, so the user can answer everything at once.

Wait for an explicit go-ahead before any mutation. On a refusal, stop cleanly — no partial application.

### 5. Apply

Run the confirmed plan. Order matters: **JSON edits first, then container actions** — if a setup script fails after a JSON edit, the user can re-run `/test-providers` to fix the container without re-typing the provider list, but a JSON edit after a successful container start would leave a half-applied state if the edit fails.

#### 5a. Backup

On the first edit per session, copy the current file:

```
cp UserDataProviders.json .build/.claude/UserDataProviders.json.bak.<ISO-timestamp>
```

Single Bash call. Skip if the user explicitly chose `skip-backup` in the consent prompt; in either case keep an in-memory copy of the pre-edit contents for the duration of this `/test-providers` invocation in case the user aborts after the edit.

If the consent prompt was `cancel`, abort here — no edit, no container action.

#### 5b. JSON edit (one Edit per affected bucket)

Per affected TFM bucket:

1. The `old_string` is the exact current `"Providers": [ … ]` block from the opening `[` to the closing `]`.
2. The `new_string` is the same block with markers flipped per step 3. Do not reorder, do not regex-replace across the whole file, and do not split into one `Edit` per provider — that triggers N permission prompts. One `Edit` per bucket.
3. Leave whitespace, comments, and unrelated buckets exactly as they were.

If the user agreed in step 4 to set `BaselinesPath`: a separate `Edit` call inserts the `"BaselinesPath": "c:\\GitHub\\linq2db.bls"` field into the `MyConnectionStrings` block (see the example in `testing.md` → **Enabling baselines locally**).

#### 5c. Container actions

For each `will-start` container: `docker start <name>` (one Bash call per container; can be parallelised across multiple tool calls in a single turn when the containers are independent — they almost always are).

For each `will-create` container: `Data/Setup Scripts/<script>.cmd` (one Bash call per script). These can take minutes; do not run more than two heavy-provider scripts in parallel because they compete heavily for I/O and RAM.

Record `startedByUs[<container>] = true` for every container we transitioned from `exited` / `created` / missing to `running`. Persist this map in memory for the lifetime of the agent session (read by step 6 of `stop` mode).

### 6. Stop mode

Triggered by `/test-providers stop` (with or without container names). Branches:

1. **Read state.** Run a single `docker ps -a --format "table {{.Names}}\t{{.Status}}"` call and filter for every container in the `startedByUs` map (and any containers the user named explicitly). Per `agent-rules.md` → *Docker containers: start/stop/create only*, do not use `docker container inspect`.
2. **Confirm.** Present a numbered list of running containers with their state and ask which to stop (`1,3`, `all`, `none`). Default on empty reply is `none` — never auto-stop.
3. **Stop.** One Bash call per chosen container: `docker stop <name>`. Update the in-memory state map.
4. **Report.** What stopped, what stayed running.

Do not edit `UserDataProviders.json` in stop mode — the file is independent of container state, and disabling a provider doesn't require stopping its container.

### 7. Reset mode

Triggered by `/test-providers reset`.

1. **Confirm explicitly.** Reset overwrites the user's local enable/disable choices and any custom `MyConnectionStrings` entries. Make this clear in the prompt and require an explicit go-ahead.
2. **Backup.** Copy `UserDataProviders.json` to `.build/.claude/UserDataProviders.json.bak.<ISO-timestamp>` (single Bash call). Same backup rules as step 5a; not skippable for reset (the destruction is wholesale).
3. **Apply.** `cp UserDataProviders.json.template UserDataProviders.json` (single Bash call).
4. **Report** the backup path. Do not touch containers.

### 8. Report

End with a concise summary:

- Per affected TFM bucket: enabled-now, disabled-now, no-change counts.
- Per container: state transition (e.g. `pgsql18: exited → running`).
- Backup path.
- Any deferred items (heavy provider the user declined to start, BaselinesPath nudge skipped, etc.).
- One-liner: "Run `/test-providers stop` when you're done with the containers."

## Don'ts

- Do not run `dotnet test` or `dotnet build`. That's `/test`'s job; this skill never invokes the test pipeline.
- Do not silently edit `UserDataProviders.json`. Step 4's confirmation is mandatory on every edit, including `add` / `remove` / `reset`.
- Do not start heavy providers (DB2 / Informix / SAP HANA / SAP ASE) without surfacing the cost note from `test-databases.md`.
- Do not auto-stop containers. The user must name what to stop, or pick `all`. Empty reply means "leave running".
- Do not regex-replace across the whole `UserDataProviders.json` or restructure it. Per-bucket batched `Edit` only — see step 5b.
- Do not edit `UserDataProviders.json.template`. The template is the source of truth for `reset`; don't drift it from upstream.
- Do not call `test-runner` or invoke `/test`. The boundary is one-way: `/test` reads the state this skill left behind, never the other direction.
- Do not edit TFM buckets the user didn't ask for. Default scope is `NET100` only; widen exclusively via the `in <bucket>` clause. `NETFX`, `NET80`, and `NET90` should diff byte-for-byte clean after a default invocation.
- Do not flip `TestNoopProvider` to disabled in Set mode's sweep. It's sticky — see [`test-databases.md`](../../docs/test-databases.md) → *Sticky entries*.
- Do not touch `DefaultConfiguration`. Per-bucket edits replace only the `Providers` array; surrounding keys stay byte-for-byte identical.
- Do not auto-correct fully-qualified provider IDs. The family rules apply only to bare-family / version-only inputs; `Oracle.12.Native` (and similar explicit variants) pass through unchanged even when the family rule would prefer Managed.
