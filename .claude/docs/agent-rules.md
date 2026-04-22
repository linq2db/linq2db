## Working in this repo

Rules governing how an agent should operate on this codebase. This file is auto-imported by `CLAUDE.md`.

### Creating a new branch

- **Name.** If the user didn't specify a branch name, derive one from the task using the schema in `CLAUDE.md` → **Branch Conventions**.
  - For an issue-backed task: `issue/<id>-<kebab-slug>` (e.g. `issue/1234-fix-cte-column-aliases`).
  - For a feature: `feature/<id-or-slug>-<kebab-slug>` (e.g. `feature/5501-duckdb-provider`).
  - **Kebab-slug rules.** 2–5 words, lowercase, hyphen-separated, derived from the task goal. Prefer verb-led for issues (`fix-…`, `support-…`, `reject-…`). Strip filler words (`the`, `a`, `in`, `on`, provider names already obvious from the issue). Keep it under ~40 characters. If the task doesn't give enough context to infer a slug, ask the user rather than guessing.
  - Applies to every skill that creates branches — `/fix-issue`, `/review-pr` checkouts of new branches, `/create-issue` when it suggests a follow-up branch, ad-hoc branching from the main agent.
- **Base.** Always branch from `origin/master`. Run `git fetch origin master` first so the base isn't stale. Branch from something else only if the user explicitly says so.
- **Dirty working tree.** If there are staged or unstaged changes before branching, stop and ask the user whether to stash or discard them. Never silently discard or carry them across.
- **Blocked `git checkout` / `gh pr checkout`.** If the switch fails because uncommitted changes would be overwritten, stop and ask the user how to proceed (stash, commit, discard) — name the blocking files in the question. Do **not** silently `git worktree add` as a workaround: it hides the state conflict and fragments work across two directories. Only create a worktree when the user explicitly asks for one. When working inside an authorized worktree, local / gitignored files in the *main* repo (`UserDataProviders.json`, `.claude/settings.local.json`, etc.) don't need to be stashed — the worktree has its own copy and edits there leave the main repo untouched.

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
| `git fetch origin refs/pull/<n>/head:refs/remotes/origin/pr/<n>` **or** `git fetch origin master` after running `pr-context.ps1` | Skip — `pr-context.ps1` already fetches both the PR head and the base branch in a single bundled fetch | `pr-context.ps1` sets `fetchHead: true` by default and bundles the base-ref refresh into the same `git fetch`. Either follow-up fetch is a guaranteed no-op. |
| `git log origin/pr/<n> …` or `git rev-parse origin/pr/<n>` to find the PR head SHA | Read `headSha` from the `pr-context.ps1` output | `headSha` is populated authoritatively from `git rev-parse` inside the script; any other call path is redundant. |
| Scratch scripts under arbitrary paths (`/tmp/x.ps1`, `~/script.ps1`) | Always under `.build/.claude/*.ps1` — that path is allowlisted and gitignored | Only `.build/.claude/` is whitelisted for scratch pwsh invocations. |

**When data is already on disk, don't re-fetch it.** The `diff-reader.ps1` `writeDir` feature persists every changed file's HEAD, base-ref body, and per-file diff to `.build/.claude/pr<n>/`. Before writing a helper script to extract content from there, ask: would `Read` or `Grep` on the file that's already there answer the question? Usually yes.

### Windows Git Bash gotchas

The shell used by the Bash tool on Windows is Git Bash (MSYS / MINGW). It rewrites and fails on a few patterns that work fine on POSIX. Use the known-working forms below — don't re-derive each time.

- **`gh api` endpoints must not start with `/`.** MSYS path-mangles a leading slash into `C:/Program Files/Git/...` and `gh` rejects it. Always write `gh api user`, `gh api repos/linq2db/linq2db/pulls/<n>/reviews` — never `gh api /user` or `gh api /repos/...`. GraphQL calls (`gh api graphql`) are unaffected.
- **`gh … --body "/<literal>"` is path-mangled the same way.** Any `gh` subcommand (`gh pr comment`, `gh issue comment`, `gh pr create`, `gh issue create`, `gh pr edit`, `gh issue edit`, …) that receives a body whose first token starts with `/` has the leading `/` rewritten to `C:/Program Files/Git/...` before `gh` sees it — the comment posts successfully with a mangled body and there is no error. Symptom seen in the wild: `gh pr comment <n> --body "/azp run test-all"` landed as `C:/Program Files/Git/azp run test-all`. Workaround: use `--body-file -` and pass the body via a stdin heredoc — stdin is not subject to MSYS path conversion:
  ```
  gh pr comment <n> --repo <o>/<r> --body-file - <<'BODY'
  /azp run test-all
  BODY
  ```
  After posting a body containing a leading-slash token, verify with `gh api repos/<o>/<r>/issues/comments/<id> --jq '.body'`. The mangling is invisible from the `gh pr comment` stdout (it only prints the comment URL) — the verify is the only way to catch it.
- **Fetch a PR head via `refs/pull/<n>/head` into `origin/pr/<n>`.** `git fetch origin <headRefName>:refs/remotes/origin/<headRefName>` is fragile — when the head ref isn't tracked by the local remote's fetch refspec (fork PRs, pruned branches, stale refs), the fetch exits 0 but creates no usable ref, and a later `git diff origin/master...origin/<headRefName>` dies with "ambiguous argument". Instead:
  ```
  git fetch origin refs/pull/<n>/head:refs/remotes/origin/pr/<n>
  ```
  Then diff/log against `origin/pr/<n>` — works for any PR (upstream branch, fork, closed, whatever), never collides with local branch names, and the `pr/<n>` namespace is self-documenting.
- **Transient `fatal error — add_item` on parallel fork bursts.** When several parallel Bash calls launch Git Bash at once, one may die with `fatal error — add_item (… errno 1)`. Retry the specific failed command individually; it almost always succeeds on the next attempt. This is a MSYS cygheap race, not a command error.
- **`docker exec <container> /<container-side-path>` is path-mangled.** Git Bash rewrites `/usr/local/bin/foo` to `C:/Program Files/Git/usr/local/bin/foo` before docker sees it, so any `docker exec` that references a container-side absolute path fails with `stat … no such file or directory`. Workaround: prefix the command with `MSYS_NO_PATHCONV=1` **and** invoke through `bash -c '…'` so the path lives inside a single argument that bypasses the rewrite:
  ```
  MSYS_NO_PATHCONV=1 docker exec firebird50 bash -c '/usr/local/firebird/bin/firebird -z'
  ```
  Commands that don't reference container-side paths (`docker exec firebird50 isql -version`) are unaffected.
- **`gh … --body @-` is not the stdin flag.** `gh` treats `@-` as a literal body string, not "read from stdin" — `gh issue edit N --body @- <<'BODY' … BODY` silently sets the body to `@-`. To stream a body in, always use `--body-file`: `--body-file -` for stdin, `--body-file <path>` for a file. Applies to `gh issue create`, `gh issue edit`, `gh pr create`, `gh pr edit`, `gh pr comment`, and every `gh` subcommand that accepts `--body`.
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
- **On a surprising failure, stop and wait.** If a command, test run, or agent invocation fails in a way the plan didn't anticipate (connection refused, unexpected parser error, container not running, permission denied, etc.), don't improvise alternative paths to keep the session flowing — report what happened in one or two sentences and wait for user direction. Workarounds invented mid-failure often mask a real signal (wrong premise, wrong tool, wrong target); the user's redirect is usually faster than the bot's recovery.

### Before coding a fix or feature

Before proposing code changes for a bug fix or new feature, enumerate existing tests that already exercise the affected path and surface them to the user. Grep `Tests/` for the target code's keywords (SQL builder type, translator method, provider class); shortlist `<Fixture>.<Test>` entries with a one-line purpose each; flag what the new work will add on top. Do this before writing any code, and before invoking `test-writer` for a new regression test.

The user needs the validation story to sign off on the fix approach — implementing then retrofitting coverage is how bugs slip past review, and guessing at coverage without actually grepping produces a wrong story. When the task has no obvious affected code path yet (pure greenfield feature, vague bug report), say so and ask the user to narrow the target before attempting the enumeration.

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
- **CI run proposal.** After `gh pr create`, propose running the full provider matrix on Azure Pipelines via a `/azp run test-all` comment. See [`ci-tests.md`](ci-tests.md) for the trigger syntax and when a narrower `/azp run test-<dbname>` makes more sense. Wait for the user to confirm before posting the comment.

### Docker containers: start/stop/create only

Provider docker containers (`oracle11`, `hana2`, `postgres*`, `mysql*`, `db2`, etc.) are managed by the user; the agent's scope is limited to `docker start` / `docker stop` / `docker create` / `docker ps` to see state. **Do not** read docker-compose files, `docker inspect` env/config, read setup scripts under `Build/`, or propose changes to container configuration. Connection strings in `UserDataProviders.json` are the authoritative spec — trust them even when hostnames don't resolve locally.

If a test fails to connect after the container is started, report the failure and wait for user direction. Don't chase credentials / ports / hostnames by inspecting the container — it usually ends in guessing at a setup that doesn't match the user's actual environment.

### GitHub content authored by others

Never edit, PATCH, or overwrite GitHub content authored by a user other than the current `gh`-authenticated user. This covers:

- issue bodies
- PR bodies
- issue-comment bodies
- review-comment bodies
- commit messages
- CHANGELOG entries attributed to others (only amend your own lines)

To respond to or add to someone else's content, post a new comment / reply / review — don't modify the original. Retractions and corrections happen in a reply on the same thread, not by overwriting the thing you're retracting.

Retraction mechanics (applies also to your **own** previously submitted review/comment, since overwriting erases the public history visitors / notifications / linkers see):

- **Always check state before any `PUT` / `PATCH` on a review or comment body.** Fetch `state` + `submitted_at` first, e.g. `gh api repos/<o>/<r>/pulls/<n>/reviews/<id> --jq '{state, submitted_at}'`. A review that was `PENDING` when you posted it may have been submitted via the GitHub UI since — a submitted review (`state` ∈ {APPROVED, CHANGES_REQUESTED, COMMENTED}, `submitted_at` populated) is public history and must be retracted via reply, not edited. A truly `PENDING` review with `submitted_at: null` is still editable in place.
- **Line / file review comments:** reply via `POST /repos/{o}/{r}/pulls/{n}/comments/{comment_id}/replies` (or GraphQL `addPullRequestReviewComment` with `inReplyTo`). Reply body starts with `Retraction:` or `Correction:`, states what was wrong and why in one line.
- **Review body (top-level):** post a new review (or a PR issue comment if no new review is warranted) that references the prior review and states the retraction. Do not `PUT` the prior review body.
- **Exception — typo / broken link / formatting-only fixes that don't change meaning:** still OK to edit in place.

Metadata changes — closing/reopening, labels, milestones, assignees — are **not** content edits and remain allowed under their usual confirmation rules (commits need explicit user ask, pushes need explicit user ask, etc.).

### GitHub wording discipline

Issue bodies, PR bodies, review comments, and replies are terse and fact-dense. GitHub is a record of what changed and why — not a place for framing, apologies, or summaries of what the diff already shows.

Cut:

- **Restating the diff in prose.** If the diff already shows it, don't repeat it.
- **Apologetic / confessional framing.** No "sorry for the churn", "I wasn't sure", "this could probably be improved".
- **Puffed adjectives.** "Comprehensive", "robust", "clean", "thorough", "elegant", "proper" — replace with the concrete fact ("covers X / Y / Z") or drop.
- **Anticipatory reassurance.** "I made sure not to break anything" is filler; "pre-FB5 paths stay byte-for-byte identical" is fine.
- **Meta-narrative about the process.** "I originally tried X then switched to Y" belongs in a commit message at most, not the PR body. The body describes the state, not the journey.

Keep:

- What changed (bullets, imperative).
- Why it changed (constraint / upstream / linked issue, with a link).
- Non-obvious trade-offs the reviewer must notice (new public type, deferred test-plan item, baselines refresh).
- `Fixes #<n>` / `Closes #<n>` for auto-closing.

Review comments: lead with `**<Severity> · <ID>**`, state the finding, state the fix. No "I noticed that…" or "this might be worth looking at…" — the severity label already says "I think this matters".

Retraction/correction replies: state what was wrong, what the correct reading is, one link to evidence. No apologies — the retraction is the apology.

Your own prior posts (comments, review bodies) authored by the current `gh` user are editable without this guardrail applying.

### Agent Guardrails

Operational rules for how agents should act on this codebase. The codebase design invariants these rules protect — public-API contract, cross-cutting internals, SQL AST namespace placement, column-aligned formatting — live in [`code-design.md`](code-design.md). Read that first; it defines what these guardrails exist to preserve.

- **Don't reformat, rename, or clean up unrelated code.** The repo's column-aligned formatting is intentional (see `code-design.md` → **Column-aligned formatting is intentional**). *Unrelated* is the key word: the rule forbids touching lines the current task doesn't already modify — it does **not** suppress review findings on lines the PR itself adds or modifies. On PR-introduced lines, flag any of: **trailing whitespace on a line**, **3+ consecutive blank lines**, **mixed tabs/spaces that visibly misalign**, or **indentation not matching the enclosing scope**. These are new noise the codebase didn't have before, and the fix is a one-line ```suggestion. Don't flag the same patterns on lines the PR doesn't touch — that *is* reformatting unrelated code.
- **Don't reshape cross-cutting internals for a local fix.** When a task scoped to one provider or test seems to need a change in the SQL AST, `IDataProvider`, or translator interfaces, raise the question explicitly before making the change — the blast radius is the whole product (see `code-design.md` → **Cross-cutting internals are shared**).
- **Surface trade-offs on non-local choices.** If a decision affects public API, generated SQL, or provider behavior, describe the options in the conversation rather than picking silently. For SQL AST signature changes specifically, also flag whether the type's current namespace placement is correct — see `code-design.md` → **SQL AST types live in `LinqToDB.Internal.SqlQuery`**.
- **Document arbitrary values explicitly.** If a change requires picking an arbitrary constant (timeout, threshold, version cutoff) or making an assumption, leave a short comment or `// TODO` at the call site so a reviewer can verify it. This is a deliberate exception to Claude Code's default "no comments" policy — the value is inherently questionable and the comment is the signal for review.
- **Never hand-edit API baseline files.** `Source/**/CompatibilitySuppressions.xml` is generated output owned by the ApiCompat tool. Do not use `Edit`, `Write`, `sed`, or any other direct mutation on these files — not to add/remove a single suppression, not to "fix up" formatting, not to resolve a merge conflict. The only supported way to change them is the `api-baselines` skill (`.claude/skills/api-baselines/SKILL.md`), which regenerates them via `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` and applies the `LinqToDB.Internal.*` policy check. If a task seems to require editing these files directly (for example, an existing PR's baseline conflicts with `master`), stop and invoke `api-baselines` instead. Applies equally to the main agent, subagents, and any generated scripts.
