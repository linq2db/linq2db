## Working in this repo

Rules governing how an agent should operate on this codebase. This file is auto-imported by `CLAUDE.md`.

### Creating a new branch

- **Name.** If the user didn't specify a branch name, derive one from the task using the schema in `CLAUDE.md` → **Branch Conventions** (`issue/<id>` for a referenced issue, `feature/<slug>` otherwise). If the task doesn't give enough context to infer a name, ask the user.
- **Base.** Always branch from `origin/master`. Run `git fetch origin master` first so the base isn't stale. Branch from something else only if the user explicitly says so.
- **Dirty working tree.** If there are staged or unstaged changes before branching, stop and ask the user whether to stash or discard them. Never silently discard or carry them across.

### Bash command rules

The user may have a PreToolUse hook that rejects compound Bash calls, because the permission system evaluates them as a single opaque command, which forces a prompt instead of matching an allowlisted rule. Regardless of whether the hook is enforcing it in the current session, follow the rule: each Bash tool call must be a single command.

- No `&&` or `||` chaining
- No `;` chaining
- No shell control flow (`for`, `while`, `until`, `case`, `if`, `function`)
- No nested chains inside `$(...)` command substitution (plain `$(cmd)` is fine)
- Pipes (`|`) and heredocs are allowed

Split chained work into separate tool calls — run them in parallel when independent, sequentially when one depends on the previous.

### Dedicated tools over raw CLI

Every Bash call is matched against an allowlist of exact command prefixes — `grep`, `rg`, `find`, `cat`, `head`, `tail`, `sed`, `awk` are not allowlisted and will prompt. They also force you into pipe / redirect idioms (see below) that make the prompt worse. Use the dedicated Claude Code tool instead:

| Want to… | Use | Not |
|---|---|---|
| Search file contents | `Grep` (`pattern`, `-n`, `-i`, `-A`/`-B`/`-C`, `head_limit`, `multiline`, `glob`, `type`) | `grep`, `rg`, `ack`, `ag` |
| Read a file (full or sliced) | `Read` (`offset` / `limit`) | `cat`, `head`, `tail`, `less` |
| Find files by name / glob | `Glob` | `find`, `ls -R`, `fd` |
| Edit a file | `Edit` / `Write` | `sed -i`, `awk -i`, redirect-into-file |

Reserve Bash for commands that have no tool equivalent: `git`, `gh`, `dotnet`, `pwsh`, helper scripts under `.claude/scripts/`. When a dedicated tool exists, using the CLI equivalent is always worse — it costs a permission prompt *and* is more fragile around quoting, escaping, and platform differences.

### Avoiding pipes

Every `|` in a Bash call creates a novel command string that fails allowlist matching and triggers a permission prompt, even when both sides are individually allowed. Pipes are permitted by the chain rule above, but they add friction — prefer pipe-free equivalents:

- **Limiting output.** Use the command's own flag (`git log -n N`, `grep -m N`, `head_limit` on the `Grep` tool) before resorting to `| head` / `| tail`.
- **Filtering with jq.** Use `gh api --jq '…'` (built-in), never `gh api … | jq …`.
- **Filtering file content from a non-HEAD git ref.** Do **not** reach for `git show ref:path > .build/.claude/foo.txt` — the `>` redirect is a novel command string just like a pipe and misses the `Bash(git show *)` allowlist entry, triggering a prompt. If the content is already in a helper script's remit (e.g. PR review → `diff-reader.ps1` with `writeDir` / `include.base`), use that. Otherwise, when there's no script for it, let the raw `git show ref:path` output persist to the tool-result store and `Read` the persisted path; or grow the relevant helper script to dump the needed files for you.
- **Slicing by line number on files already on disk.** Use the `Read` tool with `offset` / `limit` directly — no dump needed. For content not on disk, same rule as above: prefer a helper script over a shell redirect.

Reserve pipes (and shell redirects) for cases that truly have no flag or script equivalent. When used, expect the prompt.

### Permission-friendly Bash patterns

Each unique Bash command string is matched against the allowlist as a whole — a pipe, redirect, or unexpected flag breaks the match even when the underlying command is allowed. The table below collects patterns that triggered prompts in recent review sessions and the equivalents that don't:

| Avoid | Prefer | Why |
|---|---|---|
| `git fetch origin --prune \| tail -5` | `git fetch origin --prune` (output is already short) | Pipe breaks allowlist match on `Bash(git fetch *)`. |
| `gh api repos/.../reviews > .build/.claude/foo.json` | `gh api ... --jq '.[] \| {...}'` for extraction, or let the raw output persist to the tool-result store and `Read` that path | `>` redirect creates a novel command string, misses `Bash(gh api *)`. |
| `pwsh -NoProfile -Command "$x = Get-Content ... \| ConvertFrom-Json; ..."` for "just read one field from a JSON file" | `Grep` on the dumped JSON for the field, or `Read` the file and eyeball the structure | Inline pwsh is never allowlisted safely; it's usually a sign you're not using a dedicated tool. See "Dedicated tools over raw CLI" above. |
| `cat .build/.claude/foo.json` / `head -20 foo` | `Read` tool (optionally with `offset`/`limit`) | `cat` / `head` / `tail` are documented "use the dedicated tool" violations. |
| `ls .build/.claude/pr<n>/Source/...` to "discover" cache layout | Consult the documented `writeDir` layout in `.claude/docs/pr-context-prep.md` and `Read` / `Grep` directly | The layout is fixed and documented; `ls` is cheap but prompts, and the discovery is unnecessary. |
| `ls -la ../linq2db.baselines` to "check if clone exists" | Just run `git -C ../linq2db.baselines fetch origin`; it errors loudly if the clone is missing | `ls` on documented sibling paths is not allowlisted and always prompts. The `git fetch` is self-diagnosing: on a missing clone it fails with `fatal: not a git repository`, which the skill handles by asking the user to clone. |
| `mkdir -p .build/.claude/pr<n>` before calling `diff-reader.ps1` / other scripts that take `writeDir` | Just call the script — it creates the directory itself | Every helper under `.claude/scripts/` that accepts a `writeDir` (or any output-path parameter) runs `Directory.CreateDirectory` on it internally. The pre-mkdir is a permission-prompting no-op. |
| `git fetch origin refs/pull/<n>/head:refs/remotes/origin/pr/<n>` after running `pr-context.ps1` | Skip — `pr-context.ps1` already fetches the PR head (it's the first job in the parallel batch) | `pr-context.ps1` sets `fetchHead: true` by default. The second fetch is a guaranteed no-op. |
| `git log origin/pr/<n> …` or `git rev-parse origin/pr/<n>` to find the PR head SHA | Read `headSha` from the `pr-context.ps1` output | `headSha` is populated authoritatively from `git rev-parse` inside the script; any other call path is redundant. |
| Scratch scripts under arbitrary paths (`/tmp/x.ps1`, `~/script.ps1`) | Always under `.build/.claude/*.ps1` — that path is allowlisted and gitignored | Only `.build/.claude/` is whitelisted for scratch pwsh invocations. |

**When data is already on disk, don't re-fetch it.** The `diff-reader.ps1` `writeDir` feature persists every changed file's HEAD, base-ref body, and per-file diff to `.build/.claude/pr<n>/`. Before writing a helper script to extract content from there, ask: would `Read` or `Grep` on the file that's already there answer the question? Usually yes.

### Windows Git Bash gotchas

The shell used by the Bash tool on Windows is Git Bash (MSYS / MINGW). It rewrites and fails on a few patterns that work fine on POSIX. Use the known-working forms below — don't re-derive each time.

- **`gh api` endpoints must not start with `/`.** MSYS path-mangles a leading slash into `C:/Program Files/Git/...` and `gh` rejects it. Always write `gh api user`, `gh api repos/linq2db/linq2db/pulls/<n>/reviews` — never `gh api /user` or `gh api /repos/...`. GraphQL calls (`gh api graphql`) are unaffected.
- **Fetch a PR head via `refs/pull/<n>/head` into `origin/pr/<n>`.** `git fetch origin <headRefName>:refs/remotes/origin/<headRefName>` is fragile — when the head ref isn't tracked by the local remote's fetch refspec (fork PRs, pruned branches, stale refs), the fetch exits 0 but creates no usable ref, and a later `git diff origin/master...origin/<headRefName>` dies with "ambiguous argument". Instead:
  ```
  git fetch origin refs/pull/<n>/head:refs/remotes/origin/pr/<n>
  ```
  Then diff/log against `origin/pr/<n>` — works for any PR (upstream branch, fork, closed, whatever), never collides with local branch names, and the `pr/<n>` namespace is self-documenting.
- **Transient `fatal error — add_item` on parallel fork bursts.** When several parallel Bash calls launch Git Bash at once, one may die with `fatal error — add_item (… errno 1)`. Retry the specific failed command individually; it almost always succeeds on the next attempt. This is a MSYS cygheap race, not a command error.
- **Native-command stdout is decoded via the console code page, not UTF-8.** Capturing `gh` / `git` / other native-command output that may contain non-ASCII (emoji, em-dash, accented letters) into a pwsh variable mangles the bytes before any string op runs — the robot emoji `🤖` (UTF-8 `F0 9F A4 96`) comes back as the literal 4-character sequence `≡ƒñû`, subsequent `.Contains(robot)` / `.Replace(robot, …)` silently misses, and the garbled bytes get round-tripped back to GitHub. **Don't capture body-ish output into a variable for string surgery.** Options in priority order:
  1. **Use an existing helper that goes through `Invoke-Gh` from `_shared.ps1`** — it configures the process pipes as UTF-8, so round-trip is clean. For PR body edits specifically, use `.claude/scripts/pr-body-edit.ps1` (manifest-driven, ASCII-anchor insertions, encoding-safe). Do **not** roll an ad-hoc `gh pr view … | pwsh` pipeline.
  2. **File roundtrip.** `gh api repos/<o>/<r>/pulls/<n> --jq '.body' > path` to land raw bytes on disk, then `[System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))` to read them back as UTF-8. Write the modified body with the same UTF-8-no-BOM encoding and post via `gh pr edit --body-file`.
  3. **ASCII-only anchors.** When doing any string-match / substitution on content that may have traveled through native-command stdout, use ASCII-only markers (`"Generated with [Claude Code]"`, not the emoji). Relatedly: pwsh captures multi-line native-command stdout as a **string array**, not a joined string — always `-join "\`n"` (or file roundtrip) before `.Contains` / `.Replace`.
  4. **Preview before push.** Whatever the mechanism, dump the candidate body to a file and `Read` it before calling `gh pr edit`. Encoding mistakes are invisible from stdout counts alone.

### Batching and user interaction

Reduce round-trips and preserve the user's attention span.

- **Batch independent tool calls.** When multiple reads, searches, or shell commands don't depend on each other's output, issue them in a single assistant turn (multiple tool calls in one message). Sequential calls are only for true dependencies. This applies to Read, Grep, Glob, Bash, and any other non-mutating tool.
- **Ask-ask-do-all, not ask-do-ask-do.** When a task requires multiple user decisions, don't interleave question → action → question → action. Front-load every question you can anticipate into a single turn (numbered list so the user can reply by number), wait for all answers, then execute all resulting actions in one batch. Only fall back to interleaving when a later question genuinely depends on the outcome of an earlier action.
- **Do not batch code-change reviews.** Each unrelated code change should be proposed in its own review turn, even if that means more round-trips. Mixing several unrelated diffs into one confirmation forces the user to context-switch between concerns and makes "approve partially" awkward. Group diffs only when they belong to the same logical change.

### PowerShell Core scripts for complex operations

When a task requires multiple related `gh` / `git` / subprocess calls, wrapping the whole sequence in a single **PowerShell Core (`pwsh`) script under `.claude/scripts/`** is preferable to chaining Bash calls. Use this pattern whenever the agent would otherwise need to pass data between successive Bash tool calls or split an atomic operation across several permission prompts.

Why — what a script gives you over raw Bash:

- **Contract, not a recipe.** The script exposes a single stdin-JSON / stdout-JSON interface. The caller doesn't need to shuttle intermediate values through temp files, environment variables, or shell-variable plumbing — all multi-step state lives inside the script.
- **One permission rule per operation.** The permission system evaluates each unique Bash command string independently, so a sequence of N related commands fires N allowlist prompts even when each piece is individually permitted. A single script invocation (e.g. `pwsh -NoProfile -File .claude/scripts/foo.ps1:*`) matches one rule for all internal calls it makes.
- **No compound-command friction.** The Bash chain rule above forbids `&&`, `;`, and shell control flow. Work that genuinely needs branching, loops, or error-gated sequencing fits naturally inside a script and stays off the Bash surface.
- **Structured error handling.** The script can fail early with a non-zero exit and a diagnostic line on stderr, or emit a partial-success JSON with per-item `ok` flags. The caller reads one result object instead of reconstructing status from several partial Bash outputs.
- **Cross-platform.** `pwsh` behaves identically on Windows (including Git Bash), macOS, and Linux, sidestepping the MSYS path-mangling gotchas that bite raw `gh` / `git` calls.

Contract expected of scripts under `.claude/scripts/`:

- Single entrypoint, named after the operation in kebab-case (e.g. `pr-context.ps1`, `post-pr-review.ps1`).
- Read exactly one JSON manifest from stdin. Emit exactly one JSON result to stdout — nothing else. Diagnostics and errors go to stderr.
- Common helpers live in `_shared.ps1`, dot-sourced at the top of each script (`. "$PSScriptRoot/_shared.ps1"`).
- Invoke as `pwsh -NoProfile -File .claude/scripts/<name>.ps1 <<'EOF' ... EOF` from Bash. `-NoProfile` skips user profile load (faster startup, no side effects).
- Do not write scratch files unless the manifest asks for one. When a caller needs to stream a large body into the script, accept either an inline field (`"body"`) or a file path (`"bodyFile"`) and resolve it server-side.
- Fan-out parallelism inside a script uses `Start-ThreadJob` or `ForEach-Object -Parallel` — both require PowerShell 7+ and run independent subprocess invocations without adding any Bash-call cost to the caller.

Gotchas discovered while building the current set of scripts — worth knowing before you add a new one:

- **`$using:` does not nest.** A `Start-ThreadJob` spawned inside `ForEach-Object -Parallel` cannot access `$using:foo` from the outer parallel scope. Flatten to one parallel level, or copy the value into a local variable first.
- **`exit` inside a parallel block only kills that runspace.** Surface fatal errors from inside `Start-ThreadJob` / `ForEach-Object -Parallel` by returning an object with an `error` field and letting the parent check it — do not call the shared `Exit-WithError` helper from inside the scriptblock.
- **`ConvertFrom-Json` parses numbers as `[int64]`, not `[int32]`.** A plain `-is [int]` check fails for every JSON-sourced integer. Use the `Test-IsInteger` helper in `_shared.ps1` (or coerce via `[long]` / `-as [int]`) when validating numeric manifest fields.
- **Empty arrays get unwrapped in single-expression `if/else` assignments.** `$x = if ($cond) { @(...) } else { @() }` stores `$null` in `$x` when the else branch is taken, and `ConvertTo-Json` then serialises the field as `null`. Use statement form: `$x = @(); if ($cond) { $x = @(...) }`.
- **Use forward slashes for dot-source paths.** `. "$PSScriptRoot/_shared.ps1"` works on every platform; backslashes are literal characters in filenames on Linux/macOS and cause the lookup to fail there.

When to reach for this pattern:

- The agent would otherwise make ≥ 3 related Bash calls whose outputs feed a fourth call (e.g. load PR metadata + reviews + comments + closing-issues, then compute a derived result).
- The operation is already described in a `.claude/docs/*.md` reference and is invoked from more than one skill or subagent.
- The work has a natural "manifest in, result out" shape that would otherwise need several Bash round-trips with JSON payloads built on the fly.

Skip the pattern for one-shot calls (a single `gh pr view`, a single `git fetch`) — the overhead isn't worth it.

### Temp files

Any skill, subagent, or ad-hoc command that needs to write a scratch file (JSON payloads for `gh api --input`, generated diffs, intermediate output, etc.) must place it under **`.build/.claude/`** at the repo root.

- `.build/` is already in `.gitignore`, so files there are never accidentally committed.
- The `.claude/` subpath disambiguates from other tooling's build output.
- Ensure the directory exists with `mkdir -p .build/.claude` (single Bash call) before the first write in an invocation.
- Name files so they identify the skill and any relevant id, e.g. `.build/.claude/review-pr-1234.json`.
- Do not use `%TEMP%`, `/tmp`, or an absolute OS temp path — keeping scratch files inside the repo makes them easy to inspect if a run fails and makes cleanup a single directory.
- Clean-up is optional — it's a gitignored dir, and keeping the last payload is often useful for debugging.

### Git commit rules

- **Never run `git commit` without an explicit user request.** "Explicit" means the user told you to commit in the current turn (e.g. "commit", "commit this", "commit changes"). Finishing edits, passing tests, or a clean working tree are not requests. When in doubt, stop and ask.
- This applies even when the preceding turn ended with a commit — each new change needs its own explicit go-ahead.
- Same rule for `git push`, `git tag`, `gh pr create`, and any other publishing action.

### Push to remote rules

- **Never `git push` without an explicit user request.** Same rule as commits — each push needs its own go-ahead.
- **After every successful push**, check for a PR on that branch (`gh pr list --head <branch> --json number,title,body,url`):
  - If **no PR exists**, propose creating one (see **Pull request rules**) and wait for confirmation.
  - If **a PR exists**, diff the newly pushed commits against the current PR body. If the body no longer accurately describes the work (new summary bullets, new linked issues, etc.), propose a concrete edit and wait for confirmation before calling `gh pr edit`. **Show the proposed change as a diff between the current body and the new one** (e.g. a unified diff or `- old line` / `+ new line` markers) — do not just paste the new body in full. If the body is still accurate, say so and move on — don't edit gratuitously.

### Pull request rules

When creating a PR on `linq2db/linq2db`:

- **Always open as draft** (`gh pr create --draft`). Never publish a ready-for-review PR unless the user explicitly asks.
- **Confirm title and body with the user before running `gh pr create`.** Propose both, wait for approval, then create.
- **Link referenced issues/tasks as closed on merge.** If the work targets a known issue or task, include `Fixes #<n>` / `Closes #<n>` in the PR body so GitHub auto-closes it when the PR merges. One keyword per issue.
- **Assignee.** Assign the PR to the current GitHub user (`gh pr create --assignee @me`) unless the user specifies someone else.
- **Milestone.**
  - If the linked issue/task has a milestone, reuse it.
  - Otherwise ask the user to pick one. Fetch open milestones via `gh api repos/linq2db/linq2db/milestones?state=open` and present a **numbered list** (so the user can reply with just a number) in this order:
    1. The **next-version milestone** (matching `<Version>` in `Directory.Build.props`, or the closest upcoming version) — always first.
    2. Remaining **versioned** milestones (titles starting with a digit, e.g. `6.x`, `7.0.0`), sorted by version.
    3. **Non-versioned** milestones (e.g. `Backlog`, `In-progress`), sorted alphabetically by title.

### Agent Guardrails

Operational rules for how agents should act on this codebase. The codebase design invariants these rules protect — public-API contract, cross-cutting internals, SQL AST namespace placement, column-aligned formatting — live in [`code-design.md`](code-design.md). Read that first; it defines what these guardrails exist to preserve.

- **Don't reformat, rename, or clean up unrelated code.** The repo's column-aligned formatting is intentional (see `code-design.md` → **Column-aligned formatting is intentional**). Only flag formatting when it is clearly broken: three or more consecutive blank lines, mixed tabs/spaces causing visible misalignment, indentation that doesn't match the enclosing scope.
- **Don't reshape cross-cutting internals for a local fix.** When a task scoped to one provider or test seems to need a change in the SQL AST, `IDataProvider`, or translator interfaces, raise the question explicitly before making the change — the blast radius is the whole product (see `code-design.md` → **Cross-cutting internals are shared**).
- **Surface trade-offs on non-local choices.** If a decision affects public API, generated SQL, or provider behavior, describe the options in the conversation rather than picking silently. For SQL AST signature changes specifically, also flag whether the type's current namespace placement is correct — see `code-design.md` → **SQL AST types live in `LinqToDB.Internal.SqlQuery`**.
- **Document arbitrary values explicitly.** If a change requires picking an arbitrary constant (timeout, threshold, version cutoff) or making an assumption, leave a short comment or `// TODO` at the call site so a reviewer can verify it. This is a deliberate exception to Claude Code's default "no comments" policy — the value is inherently questionable and the comment is the signal for review.
