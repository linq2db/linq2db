## Working in this repo

Rules governing how an agent should operate on this codebase. This file is auto-imported by `CLAUDE.md`.

### Creating a new branch

- **Name.** If the user didn't specify a branch name, derive one from the task:
  - For an issue-backed task: `issue/<id>-<kebab-slug>` (e.g. `issue/1234-fix-cte-column-aliases`).
  - For a feature: `feature/<id-or-slug>-<kebab-slug>` (e.g. `feature/5501-duckdb-provider`).
  - **Kebab-slug rules.** 2–5 words, lowercase, hyphen-separated, derived from the task goal. Prefer verb-led for issues (`fix-…`, `support-…`, `reject-…`). Strip filler words (`the`, `a`, `in`, `on`, provider names already obvious from the issue). Keep it under ~40 characters. If the task doesn't give enough context to infer a slug, ask the user rather than guessing.
  - Applies to every skill that creates branches — `/fix-issue`, `/review-pr` checkouts of new branches, `/create-issue` when it suggests a follow-up branch, ad-hoc branching from the main agent.
- **Base.** Always branch from `origin/master`. Run `git fetch origin master` first so the base isn't stale. Branch from something else only if the user explicitly says so.
- **Dirty working tree.** If there are staged or unstaged changes before branching, stop and ask the user whether to stash or discard them. Never silently discard or carry them across.
- **Blocked `git checkout` / `gh pr checkout`.** If the switch fails because uncommitted changes would be overwritten, stop and ask the user how to proceed (stash, commit, discard) — name the blocking files in the question. Do **not** silently `git worktree add` as a workaround.
- **Worktrees.** Only create one when the user explicitly asks. Worktree-specific mechanics (`UserDataProviders.json` placement, gitignored files in the main repo, etc.): see [`worktree.md`](worktree.md).
- **Don't switch to a recovery branch mid-rebase.** When `git rebase origin/master` (or another in-flight task) surfaces an *unrelated* breakage on master itself — typically a CI build failure caused by a merge race between recent PRs — finish the in-flight branch's mechanics before opening a recovery branch. Order: resolve the rebase conflict → force-push the rebased in-flight branch → `git switch -c <recovery-branch> origin/master` → open the recovery PR. Switching mid-rebase leaves the in-flight branch in "rebased but unpushed, mid-investigation" limbo and costs context. The recovery PR can run CI in parallel once the in-flight is pushed.

### Carrying `.claude/` curation across branch switches

`.claude/` skills, docs, hooks, and scripts accumulate on `infra/claude-curation` between weekly merges to `master`. Switching from `infra/claude-curation` to a working branch (feature/\*, issue/\*, etc.) without carrying those changes forward causes the agent to operate against the older `.claude/` state — losing skill refinements, operational rules, and context captured since the last master merge.

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

### Bash command rules

Each Bash tool call must be a single command — the user may have a PreToolUse hook that rejects compound calls, and the permission system evaluates them as one opaque string anyway:

- No `&&` / `||` / `;` chaining; no shell control flow (`for`, `while`, `until`, `case`, `if`, `function`); no nested chains inside `$(...)` (plain `$(cmd)` is fine).
- Pipes (`|`) and heredocs are allowed but every pipe / `>` redirect creates a novel command string that misses allowlist matching — prefer the command's own flag (`git log -n N`, `gh api --jq`, `Grep head_limit`) or persist raw output to the tool-result store and `Read` the path.

Split chained work into separate tool calls — parallel when independent, sequential when dependent.

#### Dedicated tools over raw CLI

`grep`, `rg`, `find`, `cat`, `head`, `tail`, `sed`, `awk` aren't allowlisted and prompt every time. Use the dedicated Claude Code tool:

| Want to… | Use | Not |
|---|---|---|
| Search file contents | `Grep` (`pattern`, `-n`, `-i`, `-A`/`-B`/`-C`, `head_limit`, `multiline`, `glob`, `type`) | `grep`, `rg`, `ack`, `ag` |
| Read a file (full or sliced) | `Read` (`offset` / `limit`) | `cat`, `head`, `tail`, `less` |
| Find files by name / glob | `Glob` | `find`, `ls -R`, `fd` |
| Edit a file | `Edit` / `Write` | `sed -i`, `awk -i`, redirect-into-file |

Reserve Bash for `git`, `gh`, `dotnet`, `pwsh`, helper scripts under `.claude/scripts/`.

#### Permission-friendly patterns

Patterns that triggered prompts in real sessions and the equivalents that don't:

| Avoid | Prefer | Why |
|---|---|---|
| `gh api ... > .build/.claude/foo.json` | `gh api ... --jq '...'` for extraction, or let raw output persist + `Read` | `>` redirect creates a novel command string, misses `Bash(gh api *)`. |
| `pwsh -NoProfile -Command "..."` for "just read one field" | `Grep` / `Read` directly | Inline pwsh is never allowlisted safely. |
| `pwsh -NoProfile -NonInteractive -File .claude/scripts/<name>.ps1` | `pwsh -NoProfile -File .claude/scripts/<name>.ps1` | Every script's allowlist is `pwsh -NoProfile -File .claude/scripts/<name>.ps1 *` (exact prefix + space-asterisk). Inserting `-NonInteractive` between `-NoProfile` and `-File` breaks the prefix match and triggers a prompt. Stdin-fed scripts can't prompt anyway. |
| `ls -la ../linq2db.baselines` to "check if clone exists" | `git -C ../linq2db.baselines fetch origin` (errors loudly if missing) | `ls` on documented sibling paths always prompts. |
| `mkdir -p .build/.claude/pr<n>` before a script that takes `writeDir` | Just call the script — it creates the dir itself | Helper scripts under `.claude/scripts/` create their `writeDir` internally. |
| `git fetch refs/pull/<n>/head:...` or `git fetch origin master` after `pr-context.ps1` | Skip — `pr-context.ps1` already bundles both fetches | `pr-context.ps1` sets `fetchHead: true` and refreshes the base ref in one fetch. |
| `git rev-parse origin/pr/<n>` to find the PR head SHA | Read `headSha` from the `pr-context.ps1` output | `headSha` is populated authoritatively from `git rev-parse` inside the script. |
| Scratch scripts at `/tmp/x.ps1` / `~/script.ps1` | Always under `.build/.claude/*.ps1` (allowlisted, gitignored) | Only `.build/.claude/` is whitelisted for scratch invocations. |
| `gh api ... -f body=@<file>` to PATCH a comment body from a markdown file | Build JSON via pwsh `@{body=Get-Content -Raw <md>} \| ConvertTo-Json -Compress \| Set-Content <json>` then `gh api --method PATCH ... --input <json>`. **For POST replies on review threads (`/pulls/<n>/comments/<id>/replies`) whose body is just `{body: "..."}`, the simpler `gh api ... -F body=@<file>` (capital `F`) form works — gh's `-F` flag interprets `@<file>` as "read file contents", unlike lowercase `-f` which treats `@<file>` as a literal string.** | `-f`'s `@<file>` form is **not** interpreted — it stores the literal string `@<file>` as the body. Same trap as `gh … --body @-` (already banned above). The `@<file>` shorthand only works on a few specific gh flags (`--body-file`, etc.); for REST PATCH bodies use `--input` with a JSON wrapper file. The capital-`F` form (`-F body=@<file>`) does interpret `@<file>` per gh CLI's documented field-coercion behavior — see `cli.github.com/manual/gh_api` (Type Coercion). |
| `echo '<json>' \| pwsh -File .claude/scripts/<name>.ps1` or `pwsh -File .claude/scripts/<name>.ps1 <<'EOF' ... EOF` to feed a script | Use the script's named-params or `-ManifestFile` form: `pwsh -File .claude/scripts/<name>.ps1 -Pr 5503` (scalar inputs) or `pwsh -File .claude/scripts/<name>.ps1 -ManifestFile <json>` (structured inputs). Write the JSON to `.build/.claude/<script>-<id>.json` first if needed | Stdin pipes / heredocs from Bash create novel command strings that miss the `Bash(pwsh -NoProfile -File <path> *)` allowlist match. Named parameters and `-ManifestFile <path>` keep the invocation a single allowlisted token sequence. Stdin-only invocations from the PowerShell tool (no bash layer) also hang because `[Console]::In.ReadToEnd()` blocks waiting for EOF that never arrives. See [`script-authoring.md`](script-authoring.md) → **Contract** → *Input shape*. |

When data is already on disk (e.g. `diff-reader.ps1`'s `writeDir` cache at `.build/.claude/pr<n>/`), `Read` or `Grep` it directly rather than re-fetching via `git show … | tail | cat -A` — the `Read` tool preserves tabs and trailing whitespace literally for whitespace-byte inspection.

### Windows Git Bash gotchas

MSYS path-mangling and stdout-decoding bite well-formed `gh` / `git` / `docker` calls. Full reference: [`windows-gotchas.md`](windows-gotchas.md). One-line triggers — when you hit one of these, read the doc:

- `gh api` rejects a leading-slash endpoint (use `gh api user`, never `gh api /user`).
- `git show <ref>:<path>` fails with `ambiguous argument` when `<ref>` contains `/` — use `git ls-tree` + `git cat-file -p` instead.
- **`gh … --body` is banned.** Always `--body-file <path>` or `--body-file -`. For `/azp run …` triggers use `.claude/scripts/azp-run.ps1`. (`gh … --body @-` is *not* stdin either — it sets the body to the literal string `@-`.)
- `git fetch <headRefName>` of a fork PR may produce no usable ref — fetch via `refs/pull/<n>/head:refs/remotes/origin/pr/<n>`.
- Transient `fatal error — add_item (… errno 1)` on parallel fork bursts: retry the failed command once.
- `docker exec <container> /<path>` mangles the path: prefix with `MSYS_NO_PATHCONV=1` and wrap in `bash -c '…'`.
- Captured `gh` / `git` stdout decodes via the console code page, not UTF-8 — non-ASCII (emoji, em-dash) gets mangled. Don't capture body-ish output into a pwsh variable; use `Invoke-Gh` helpers, file roundtrip, or ASCII-only anchors.
- `Glob` returns "No files found" for a path documented in CLAUDE.md / a SKILL / `agent-rules.md` — `Read` the documented path directly before concluding it's missing or reimplementing the helper.

### Batching and user interaction

Reduce round-trips and preserve the user's attention span.

- **Batch independent tool calls.** When multiple reads, searches, or shell commands don't depend on each other's output, issue them in a single assistant turn (multiple tool calls in one message). Sequential calls are only for true dependencies. This applies to Read, Grep, Glob, Bash, and any other non-mutating tool.
- **Ask-ask-do-all, not ask-do-ask-do.** When a task requires multiple user decisions, don't interleave question → action → question → action. Front-load every question you can anticipate into a single turn (numbered list so the user can reply by number), wait for all answers, then execute all resulting actions in one batch. Only fall back to interleaving when a later question genuinely depends on the outcome of an earlier action.
- **Do not batch code-change reviews.** Each unrelated code change should be proposed in its own review turn, even if that means more round-trips. Mixing several unrelated diffs into one confirmation forces the user to context-switch between concerns and makes "approve partially" awkward. Group diffs only when they belong to the same logical change.
- **On a surprising failure, stop and wait.** If a command, test run, or agent invocation fails in a way the plan didn't anticipate (connection refused, unexpected parser error, container not running, permission denied, etc.), don't improvise alternative paths to keep the session flowing — report what happened in one or two sentences and wait for user direction. Workarounds invented mid-failure often mask a real signal (wrong premise, wrong tool, wrong target); the user's redirect is usually faster than the bot's recovery.
- **Batch edits on a single config file.** When reshaping multiple sections of one file (enabling / disabling several providers across TFMs in `UserDataProviders.json`, toggling several `<PackageReference>` versions in `Directory.Build.props`, rewriting a handful of keys in `settings.json`, etc.), read the file once, plan the full set of edits, then apply them as a single `Edit` call with enough surrounding context to disambiguate each target — or, when `Edit` can't cover multiple distinct section headings in one shot, a back-to-back sequence with no intermediate re-reads. Incremental nibbles — edit a line, read back, edit another line, read back — burn permission surface and miss cluster-level invariants across the sections.

### Capability self-assessment

Before reporting a task as infeasible — "runtime test outside my reach", "can't bisect", "can't build" — do a one-pass environment check:

- `docker ps -a --filter name=<provider>` to list provider containers (Oracle, MySQL, PostgreSQL, etc.) that may be available
- `Glob` under `.claude/scripts/` for helper scripts that wrap multi-step sequences
- Check `UserDataProviders.json` (root) and the sibling clone at `c:\GitHub\linq2db\UserDataProviders.json` for connection-string availability
- Consider whether existing skills (`/test`, `/test-providers`) cover the workflow

The project has set up infrastructure for many investigation patterns (provider docker containers, sibling clones with real CSes, worktree-friendly scripts). Reaching for "I can't" before checking what's actually available wastes the user's time twice — once on the false-negative answer, once on the redirect.

When the runtime cost is real but the capability exists, surface the cost transparently and let the user decide — don't make the call for them.

### Presenting proposed code changes

When showing a snippet that interleaves existing context with new additions in a **non-diff** format (e.g. illustrating a fix against surrounding code), prefix each new line with `+ ` (two-char leading gutter) inside a fenced code block; existing/context lines carry two leading spaces to preserve alignment. Do not use `<mark>` inside `<pre>` (it does not render highlighted in the Claude Code CLI) and do not use trailing-sigil markers (`// ← new`) — the leading gutter is the agreed convention.

The gutter is only needed when context and additions are interleaved on adjacent lines. For a standalone new block or a real diff, use normal fenced code / unified diff.

### Before coding a fix or feature

Before proposing code changes for a bug fix or new feature, enumerate existing tests that already exercise the affected path and surface them to the user. Grep `Tests/` for the target code's keywords (SQL builder type, translator method, provider class); shortlist `<Fixture>.<Test>` entries with a one-line purpose each; flag what the new work will add on top. Do this before writing any code, and before invoking `test-writer` for a new regression test.

The user needs the validation story to sign off on the fix approach — implementing then retrofitting coverage is how bugs slip past review, and guessing at coverage without actually grepping produces a wrong story. When the task has no obvious affected code path yet (pure greenfield feature, vague bug report), say so and ask the user to narrow the target before attempting the enumeration.

### PowerShell Core scripts for complex operations

When the agent would otherwise make ≥ 3 related `gh` / `git` calls whose outputs feed each other (load → transform → post), wrap the sequence in a single **PowerShell Core script under `.claude/scripts/`** instead. One allowlist match instead of N, multi-step state stays inside the script, no compound-Bash friction, identical behavior on Windows / macOS / Linux. Skip for one-shot calls — the overhead isn't worth it.

Authoring contract, parallelism rules, and pwsh-script gotchas live in [`script-authoring.md`](script-authoring.md). Read it before adding or extending a script under `.claude/scripts/`.

**Prefer the PowerShell tool over `Bash(pwsh -NoProfile -File …)`** when invoking these scripts. Routing the call through Claude Code's PowerShell tool skips the Git-Bash / MSYS layer entirely — no path-mangling on slash-prefixed args, no `\??\C:\…` cygheap races, no quoting differences, no double allowlist hop. Use the Bash wrapper only when you need shell features the PowerShell tool can't express (multi-stage stdin heredoc piped between non-pwsh commands, etc.).

### Temp files

Any skill, subagent, or ad-hoc command that needs to write a scratch file (JSON payloads for `gh api --input`, generated diffs, intermediate output, etc.) must place it under **`.build/.claude/`** at the repo root.

- `.build/` is already in `.gitignore`, so files there are never accidentally committed.
- The `.claude/` subpath disambiguates from other tooling's build output.
- Ensure the directory exists with `mkdir -p .build/.claude` (single Bash call) before the first write in an invocation.
- Name files so they identify the skill and any relevant id, e.g. `.build/.claude/review-pr-1234.json`.
- Do not use `%TEMP%`, `/tmp`, or an absolute OS temp path — keeping scratch files inside the repo makes them easy to inspect if a run fails and makes cleanup a single directory.
- Clean-up is optional — it's a gitignored dir, and keeping the last payload is often useful for debugging.

### Git commit rules

**Never publish without an explicit user request in the current turn.** This rule covers `git commit`, `git push`, `git tag`, `gh pr create`, posting GitHub comments, and requesting reviews — each new action needs its own go-ahead, even if the user just approved one a turn ago. "Explicit" means the user told you to do it this turn (e.g. "commit", "push", "create the PR"). Finishing edits, passing tests, or a clean working tree are not requests. When in doubt, stop and ask.

- **Never commit playground scratch.** Inside `Tests/Tests.Playground/`, two kinds of edits are PR-acceptable: structural updates to `Tests.Playground.csproj` (SDK / package / property changes that keep the project building) and updates to `TestTemplate.cs` (keeping the template current). Everything else is local scratchpad and must not be committed:
  - **No new source files** under `Tests/Tests.Playground/` — tests belong in `Tests/Linq/`, playground access is via `<Compile Include>` link.
  - **No new `<Compile Include>` test-fixture references** in `Tests.Playground.csproj` — those are `test-writer`'s `playgroundLink` entries, fast-iteration scratch that belongs on disk for the session, not in history.

  When staging a commit, audit `Tests/Tests.Playground/` for new files and added `<Compile Include>` lines and exclude them (`git restore --staged …`); if the user explicitly asks to commit one, stop and confirm before proceeding. Same gate applies to `git push` of any branch where these are dirty.
- **Amending a commit on a non-checked-out branch with a dirty current tree.** Don't `stash` → `switch` → `--amend` → `switch -` → `stash pop` — the pop can conflict on overlapping files. Build a replacement commit object and atomically retarget the branch ref while staying on the current branch:
  ```
  git show -s --format='%T%n%P%n%an%n%ae%n%aI' <branch>   # tree, parent, author name/email, date
  GIT_AUTHOR_NAME='...' GIT_AUTHOR_EMAIL='...' GIT_AUTHOR_DATE='...' \
    GIT_COMMITTER_NAME='...' GIT_COMMITTER_EMAIL='...' GIT_COMMITTER_DATE='...' \
    git commit-tree <tree> -p <parent> -m '<message>'      # prints <new-sha>
  git update-ref refs/heads/<branch> <new-sha> <old-sha>   # 3rd arg = expected old SHA, safety check
  ```
  Add `-S` to `git commit-tree` if the original was GPG-signed.

### Push to remote rules

Detail-heavy mechanics live in [`pr-and-push.md`](pr-and-push.md). One-line triggers — when one fires, read the doc:

- **After every successful push, check for a PR on the branch.** If one exists, diff the new commits against its body and propose a body edit (as a diff, never a full rewrite); when the PR's original author is someone else (or this is a follow-up on a PR the user already has prose in), append a `## Follow-up commit` subsection rather than rewriting their text. If no PR exists, propose creating one (see **Pull request rules**).
- **After every successful push, re-request Copilot review** — auto-trigger is unreliable: `gh pr edit <N> --repo linq2db/linq2db --add-reviewer copilot-pull-request-reviewer`. Don't pass the slug `Copilot` here (errors); don't fall back to the REST `requested_reviewers` endpoint (silently no-ops when Copilot already reviewed an earlier commit on the same PR).

### Pull request rules

Detail-heavy mechanics live in [`pr-and-push.md`](pr-and-push.md). One-line triggers when creating a PR on `linq2db/linq2db`:

- **Always `--draft`**, **always confirm title + body**, **always `--assignee @me`**. Include `Fixes #<n>` / `Closes #<n>` for any linked issue/task.
- **Milestone:** reuse the linked issue's milestone if any; otherwise ask the user to pick from a numbered list ordered next-version → other versioned → non-versioned alphabetical (open milestones via `gh api repos/linq2db/linq2db/milestones?state=open`).
- **CI run:** after `gh pr create`, propose `/azp run test-all` (see [`ci-tests.md`](ci-tests.md)) and wait for confirmation before posting.
- **Follow-up commits extending an open PR go on that PR's branch**, never a parallel branch. For fork PRs requires `maintainerCanModify: true`; if `false`, stop and ask rather than opening a sibling branch.

### Docker containers: start/stop/create only

Provider docker containers (`oracle11`, `hana2`, `postgres*`, `mysql*`, `db2`, etc.) are managed by the user; the agent's scope is limited to `docker start` / `docker stop` / `docker create` / `docker ps` to see state. **Do not** read docker-compose files, `docker inspect` env/config, read setup scripts under `Build/`, or propose changes to container configuration. Connection strings in `UserDataProviders.json` are the authoritative spec — trust them even when hostnames don't resolve locally.

**Start the container the test needs.** When a test target lists a provider whose container is in `docker ps -a` but stopped, `docker start <name>` it before running the test. Don't fall back to "run on whatever's currently up" — that gives a partial result and obscures coverage. The "containers managed by the user" boundary above applies to *configuration* (compose files, env, setup scripts) and *creation* of new containers — not to starting an existing, already-configured one. Only ask the user when the container doesn't exist (no row in `docker ps -a`), since that requires a `docker create` plus initial setup the user owns.

If a test fails to connect after the container is started, report the failure and wait for user direction. Don't chase credentials / ports / hostnames by inspecting the container — it usually ends in guessing at a setup that doesn't match the user's actual environment.

**Scope-change prompt for session-started containers.** Every `docker start <name>` run during the session is captured by the `track-docker-start` PostToolUse hook into `.build/.claude/docker-session-started.txt`; the `cleanup-docker-session` SessionEnd hook stops each of them when the session exits. Before running a command that changes working-tree scope — `git checkout`, `git switch`, `git worktree add`, `gh pr checkout`, or invoking a skill that switches branches for you (`/fix-issue`, a different-PR `/review-pr`, etc.) — read that state file. If it lists containers the session started, stop and ask the user whether to stop them before the scope change; name the containers in the question. Do not stop them silently — scope change doesn't always mean the user is done with the providers. Containers that were already running when the session started are not tracked and are out of scope.

### GitHub content authoring

Detail-heavy mechanics (retraction endpoints, PATCH verification, encoding traps, wording style) live in [`github-authoring.md`](github-authoring.md). One-line triggers — when one fires, read the doc:

- **Never edit content authored by other users** (issue/PR bodies, comments, commit messages, CHANGELOG attribution). Reply / new-comment only. Metadata changes (labels, milestones, assignees, close/reopen) are exempt.
- **Never overwrite your own submitted reviews / comments.** Retract via reply with `Retraction:` / `Correction:` prefix and one link to evidence. Exception: typo / broken-link / formatting-only fixes that don't change meaning.
- **HTTP 422 "internal error" from `gh api`** = transient GitHub-side outage on that endpoint. Report once with in-flight context, preserve scratch under `.build/.claude/`, wait for user direction. Don't retry-loop, don't poll `githubstatus.com`.
- **`gh api -f body=@<file>` does NOT read the file** — it stores the literal `@<file>` as the body. Same trap as `gh … --body @-` (banned in [`windows-gotchas.md`](windows-gotchas.md)). Use `--input <json-file>` with a pwsh-built wrapper, or `-F body=@<file>` (capital F) for the specific endpoints where gh CLI documents field coercion.
- **After every manual `gh api PATCH` / `PUT`**, re-fetch and verify the body prefix matches what you intended (`gh api repos/<o>/<r>/issues/comments/<id> --jq '.body[:200]'`). The API's success response only confirms the request was accepted, not that the right body was stored.
- **Wording style:** terse, fact-dense, lead with what changed + why. Review comments lead with `**<Severity> · <ID>**`, state finding, state fix. No apologies, no diff-restating prose, no puffed adjectives ("comprehensive", "robust", "clean", "proper" — replace with the concrete fact or drop).

### Agent Guardrails

Operational rules for how agents should act on this codebase. The codebase design invariants these rules protect — public-API contract, cross-cutting internals, SQL AST namespace placement, column-aligned formatting — live in [`code-design.md`](code-design.md). Read that first; it defines what these guardrails exist to preserve.

- **Don't reformat, rename, or clean up unrelated code.** The repo's column-aligned formatting is intentional (see `code-design.md` → **Column-aligned formatting is intentional**). *Unrelated* is the key word: the rule forbids touching lines the current task doesn't already modify — it does **not** suppress review findings on lines the PR itself adds or modifies. On PR-introduced lines, flag any of: **trailing whitespace on a line**, **3+ consecutive blank lines**, **mixed tabs/spaces that visibly misalign**, or **indentation not matching the enclosing scope**. These are new noise the codebase didn't have before, and the fix is a one-line ```suggestion. Don't flag the same patterns on lines the PR doesn't touch — that *is* reformatting unrelated code.
- **Don't reshape cross-cutting internals for a local fix.** When a task scoped to one provider or test seems to need a change in the SQL AST, `IDataProvider`, or translator interfaces, raise the question explicitly before making the change — the blast radius is the whole product (see `code-design.md` → **Cross-cutting internals are shared**).
- **Surface trade-offs on non-local choices.** If a decision affects public API, generated SQL, or provider behavior, describe the options in the conversation rather than picking silently. For SQL AST signature changes specifically, also flag whether the type's current namespace placement is correct — see `code-design.md` → **SQL AST types live in `LinqToDB.Internal.SqlQuery`**.
- **Build configurations: `== 'Release'` is not `!= 'Debug'`.** The repo defines four configurations (`Testing;Debug;Release;Azure`). When proposing MSBuild edits that should fire only in production-style builds, gate with `Condition="'$(Configuration)' == 'Release'"` — the existing `RunAnalyzersDuringBuild` line at `Directory.Build.props:110` is the canonical pattern. The looser `!= 'Debug'` form leaves the property enabled for `Testing` and `Azure`, which is rarely the intent (Testing in particular is the fast-iteration single-TFM CI build that should match Debug behavior).
- **Document arbitrary values explicitly.** If a change requires picking an arbitrary constant (timeout, threshold, version cutoff) or making an assumption, leave a short comment or `// TODO` at the call site so a reviewer can verify it. This is a deliberate exception to Claude Code's default "no comments" policy — the value is inherently questionable and the comment is the signal for review.
- **Never hand-edit API baseline files.** `Source/**/CompatibilitySuppressions.xml` is generated output owned by the ApiCompat tool. Do not use `Edit`, `Write`, `sed`, or any other direct mutation on these files — not to add/remove a single suppression, not to "fix up" formatting, not to resolve a merge conflict. The only supported way to change them is the `api-baselines` skill (`.claude/skills/api-baselines/SKILL.md`), which regenerates them via `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` and applies the `LinqToDB.Internal.*` policy check. If a task seems to require editing these files directly (for example, an existing PR's baseline conflicts with `master`), stop and invoke `api-baselines` instead. Applies equally to the main agent, subagents, and any generated scripts.
- **Default to script + doc guardrails before hooks.** When proposing a guardrail against a class of agent error (recurring footguns, silent encoding traps, mangling-prone CLI shapes), build a helper script under `.claude/scripts/` that encodes the right path **and** a blanket rule in this doc that surfaces the script — before reaching for a `PreToolUse` / `PostToolUse` / `SessionEnd` hook. Hooks are opt-in via `.claude/settings.local.json`, add harness surface area, and don't help users who haven't wired them in; scripts + rules cover everyone reading this file. Reach for a hook only when the user explicitly asks for one, or when the failure mode is genuinely undetectable from inside Claude (silent stdout corruption that no script can prevent because it happens after the agent has already typed the wrong thing).
- **Verify subagent output with `git status` after every invocation.** Subagent descriptions (`Read, Grep, Bash`, "never edits source code", etc.) are advisory — the harness does *not* enforce them. A read-only-declared agent can still call `Edit` / `Write` if its prompt nudges it that direction, and the only signal back to the main agent is the structured result it chooses to report. After any `Agent` call that returned, run `git status` once and confirm the only modified files are the ones the agent's task scope justifies. Particularly load-bearing for `test-runner` (declares no file writes), `code-reviewer` / `baselines-reviewer` (declare read-only), and any `Explore` agent. If unexpected files appear, treat the agent's result as suspect, restore the files (`git restore <path>`), and either re-invoke with a tighter prompt or do the work yourself.
- **Provider behavior claims must be verified against translator code.** When agent-authored content — review bodies, release-notes drafts, PR comments, **XML docs on public types/members, inline source-code comments** — makes a specific claim about how a provider translates a member or operation (e.g. "SQL Server 2016+ `DateTimeOffset.UtcNow` emits `SYSDATETIMEOFFSET() AT TIME ZONE 'UTC'`"), verify it by reading the relevant translator at PR HEAD (`Source/LinqToDB/Internal/DataProvider/<Provider>/Translation/<Provider>MemberTranslator.cs`) before writing. The base virtuals' default returns can mislead — e.g. `TranslateNow` defaults to `CURRENT_TIMESTAMP`, but most providers override it to return `null`, so claims like "DateTime.Now is server-side" depend on which providers actually inherit vs override. Don't rely on baseline diffs or memory of older `[SqlFunction]` attributes — they show what the test produces / what *used to* be the dispatch, not what every current code path produces. Audit each per-provider claim against the actual override. (`code-reviewer.md` rule 9 covers XML-doc claims about *external* systems / vendor docs; this rule covers first-party translator behavior.)
