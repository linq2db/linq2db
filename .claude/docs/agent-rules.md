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
- **Prefer `git merge` over `git rebase` for multi-commit feature branches that already contain "Merge master" commits.** When a long-lived feature branch (50+ commits, multiple `Merge branch 'master' into <branch>` commits in its history) needs to sync with master before merge, `git rebase origin/master` would replay every commit individually and resolve conflicts at each step — N rebase-conflict resolutions for N intermediate commits, with the merge commits dropped. `git merge origin/master` resolves once, in one commit, matching the branch's existing pattern. The squash-merge into master at the end collapses the merge history anyway, so the choice doesn't affect the final master-side history. Use rebase only on short-lived feature branches (< 10 commits, no prior merge commits) or when the user explicitly wants linear history.

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

Patterns that triggered prompts in real sessions — full table in [`windows-gotchas.md`](windows-gotchas.md) → *Permission-friendly Bash patterns*. The four highest-impact:

- **`>` redirect** (`gh api … > path`) misses `Bash(gh api *)` — prefer `gh api … --jq '…'` or let raw output persist + `Read`.
- **Inline `pwsh -NoProfile -Command "…"`** is never allowlisted safely — use `Grep` / `Read` instead.
- **`gh … --body @-` / `-f body=@<file>`** sets the body to the literal string `@-` or `@<file>` — use `--body-file <path>` (or `--input <json>` for PATCH).
- **Scratch scripts** must live under `.build/.claude/*.ps1` — only that path is allowlisted for ad-hoc invocations.

When data is already on disk (e.g. `diff-reader.ps1`'s `writeDir` cache at `.build/.claude/pr<n>/`), `Read` or `Grep` it directly rather than re-fetching via `git show …` pipelines — the `Read` tool preserves tabs and trailing whitespace literally for whitespace-byte inspection.

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

Before reporting a task as infeasible ("can't bisect", "can't build", "runtime test outside my reach"), do a one-pass environment check:

- `docker ps -a --filter name=<provider>` for provider containers
- `Glob` under `.claude/scripts/` for helper scripts wrapping multi-step sequences
- `UserDataProviders.json` (root) and the sibling clone at `c:\GitHub\linq2db\UserDataProviders.json` for connection strings
- Existing skills (`/test`, `/test-providers`) for workflow coverage

When the capability exists but the runtime cost is real, surface the cost and let the user decide — don't make the call for them.

### Inferring rules from user input

When the user gives a rule that names a specific package / file / case, treat it as scoped to that case unless the user explicitly generalizes ("for all X…", "every Y in this family…"). Do **not** extrapolate to similar-looking cases without asking. Phrasing: "you gave rule X for P; case Q looks similar — apply the same rule, or treat it differently?"

The cost of an extra question is small; silent over-generalization costs a redo of every affected case plus a doc revert.

### Presenting proposed code changes

When showing a snippet that interleaves existing context with new additions in a **non-diff** format (e.g. illustrating a fix against surrounding code), prefix each new line with `+ ` (two-char leading gutter) inside a fenced code block; existing/context lines carry two leading spaces to preserve alignment. Do not use `<mark>` inside `<pre>` (it does not render highlighted in the Claude Code CLI) and do not use trailing-sigil markers (`// ← new`) — the leading gutter is the agreed convention.

The gutter is only needed when context and additions are interleaved on adjacent lines. For a standalone new block or a real diff, use normal fenced code / unified diff.

### Before coding a fix or feature

Before proposing code changes for a bug fix or new feature, enumerate existing tests that already exercise the affected path and surface them to the user. Grep `Tests/` for the target code's keywords (SQL builder type, translator method, provider class); shortlist `<Fixture>.<Test>` entries with a one-line purpose each; flag what the new work will add on top. Do this before writing any code, and before invoking `test-writer` for a new regression test.

The user needs the validation story to sign off on the fix approach — implementing then retrofitting coverage is how bugs slip past review, and guessing at coverage without actually grepping produces a wrong story. When the task has no obvious affected code path yet (pure greenfield feature, vague bug report), say so and ask the user to narrow the target before attempting the enumeration.

### Running tests

When the user asks to run tests, **invoke `Skill(test)`**. Don't call `Agent(test-runner)` directly, and don't run `dotnet build` before the skill — `dotnet test` rebuilds inside the skill, and bypassing it silently skips `CreateDatabase` filter injection and the baselines diff. Project selection (Playground vs Linq), multi-TFM gating, and `playgroundLink` are the skill's responsibility — see [`.claude/skills/test/SKILL.md`](../skills/test/SKILL.md) → step 3.1. Restated here because the rule has been violated across multiple sessions.

### Iterative-build gotchas

When iterated `dotnet build` / `/test` / `/release-verify` runs fail with `Access to the path '<dll>' is denied` (build-server file lock — fix: `dotnet build-server shutdown`) or `MSB3021/MSB3027 not enough space on disk` (`.build/bin/` accumulation — fix: `Remove-Item -Recurse -Force .build/bin .build/obj`), see [`windows-gotchas.md`](windows-gotchas.md) → *Iterative-build gotchas*.

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
- **Amending a commit on a non-checked-out branch with a dirty current tree.** Don't `stash`/`switch`/`--amend`/`switch -`/`pop` — the pop can conflict on overlapping files. Use the `commit-tree` + `update-ref` recipe in [`pr-and-push.md`](pr-and-push.md) → *Amending a commit on a non-checked-out branch with a dirty current tree*.

### Push to remote rules

Detail-heavy mechanics live in [`pr-and-push.md`](pr-and-push.md). One-line triggers — when one fires, read the doc:

- **After every successful push, check for a PR on the branch.** If one exists, diff the new commits against its body and propose a body edit (as a diff, never a full rewrite); when the PR's original author is someone else (or this is a follow-up on a PR the user already has prose in), append a `## Follow-up commit` subsection rather than rewriting their text. If no PR exists, propose creating one (see **Pull request rules**).
- **After every successful push, re-request Copilot review** — auto-trigger is unreliable: `gh pr edit <N> --repo linq2db/linq2db --add-reviewer copilot-pull-request-reviewer`. Don't pass the slug `Copilot` here (errors); don't fall back to the REST `requested_reviewers` endpoint (silently no-ops when Copilot already reviewed an earlier commit on the same PR).
- **When follow-up commits rename / move / delete tests, close the existing baselines PR and delete its branch.** `linq2db.baselines` files are keyed by the fully-qualified test name; the existing baselines PR carries files keyed to the *old* names and never auto-prunes. Leaving it open means the next CI run produces a second baselines PR while the stale one lingers. Close + delete-branch before declaring the publish bundle complete.

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
- **Never delete a release draft (or any other user-owned GitHub artifact) on the assumption that it's redundant.** Drafts may carry user edits not visible in the body excerpt fetched via `gh api`; deleting them is irrecoverable (GitHub API doesn't expose deleted drafts in events, and there is no undo). When multiple drafts collide on the same tag, stop and ask the user which to keep + which to delete. Same rule applies to: closing PRs / issues the agent didn't open, deleting branches the agent didn't push, deleting releases / tags / wiki pages.
- **PATCH on a draft release strips `tag_name`** when omitted from the request body. `gh api -X PATCH repos/<o>/<r>/releases/<id> --input <body>` flips the tag to `untagged-<random>` when the JSON body lacks `tag_name`. Always include `{tag_name, name}` in every PATCH to a draft, even when only updating `body` or `target_commitish`. Verify via `gh api .../releases/<id> --jq '.tag_name'` after each PATCH.
- **Never overwrite your own submitted reviews / comments.** Retract via reply with `Retraction:` / `Correction:` prefix and one link to evidence. Exception: typo / broken-link / formatting-only fixes that don't change meaning.
- **HTTP 422 "internal error" from `gh api`** = transient GitHub-side outage on that endpoint. Report once with in-flight context, preserve scratch under `.build/.claude/`, wait for user direction. Don't retry-loop, don't poll `githubstatus.com`.
- **`gh api -f body=@<file>` does NOT read the file** — it stores the literal `@<file>` as the body. Same trap as `gh … --body @-` (banned in [`windows-gotchas.md`](windows-gotchas.md)). Use `--input <json-file>` with a pwsh-built wrapper, or `-F body=@<file>` (capital F) for the specific endpoints where gh CLI documents field coercion.
- **After every manual `gh api PATCH` / `PUT`**, re-fetch and verify the body prefix matches what you intended (`gh api repos/<o>/<r>/issues/comments/<id> --jq '.body[:200]'`). The API's success response only confirms the request was accepted, not that the right body was stored.
- **Wording style:** terse, fact-dense, lead with what changed + why. Review comments lead with `**<Severity> · <ID>**`, state finding, state fix. No apologies, no diff-restating prose, no puffed adjectives ("comprehensive", "robust", "clean", "proper" — replace with the concrete fact or drop).

### Agent guardrails (anchor set)

The most-violated guardrails stay inline. Lower-traffic guardrails (Surface trade-offs, Build configurations `==` vs `!=`, Document arbitrary values, BOM after `Write`, Default to script over hook, Provider / Codebase claim verification) live in [`agent-guardrails.md`](agent-guardrails.md). Codebase design invariants these protect — public-API contract, cross-cutting internals, SQL AST namespace placement, column-aligned formatting — live in [`code-design.md`](code-design.md).

- **Don't reformat, rename, or clean up unrelated code.** The repo's column-aligned formatting is intentional (see `code-design.md` → **Column-aligned formatting is intentional**). *Unrelated* is the key word: the rule forbids touching lines the current task doesn't already modify — it does **not** suppress review findings on lines the PR itself adds or modifies. On PR-introduced lines, flag any of: **trailing whitespace on a line**, **3+ consecutive blank lines**, **mixed tabs/spaces that visibly misalign**, or **indentation not matching the enclosing scope**. These are new noise the codebase didn't have before, and the fix is a one-line ```suggestion. Don't flag the same patterns on lines the PR doesn't touch — that *is* reformatting unrelated code.
- **Don't reshape cross-cutting internals for a local fix.** When a task scoped to one provider or test seems to need a change in the SQL AST, `IDataProvider`, or translator interfaces, raise the question explicitly before making the change — the blast radius is the whole product (see `code-design.md` → **Cross-cutting internals are shared**).
- **Verify subagent output with `git status` after every invocation.** Subagent descriptions (`Read, Grep, Bash`, "never edits source code", etc.) are advisory — the harness does *not* enforce them. A read-only-declared agent can still call `Edit` / `Write` if its prompt nudges it that direction, and the only signal back to the main agent is the structured result it chooses to report. After any `Agent` call that returned, run `git status` once and confirm the only modified files are the ones the agent's task scope justifies. Particularly load-bearing for `test-runner` (declares no file writes), `code-reviewer` / `baselines-reviewer` (declare read-only), and any `Explore` agent. If unexpected files appear, treat the agent's result as suspect, restore the files (`git restore <path>`), and either re-invoke with a tighter prompt or do the work yourself.
- **Never hand-edit API baseline files.** `Source/**/CompatibilitySuppressions.xml` is generated output owned by the ApiCompat tool. Do not use `Edit`, `Write`, `sed`, or any other direct mutation on these files — not to add/remove a single suppression, not to "fix up" formatting, not to resolve a merge conflict. The only supported way to change them is the `api-baselines` skill (`.claude/skills/api-baselines/SKILL.md`), which regenerates them via `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` and applies the `LinqToDB.Internal.*` policy check. If a task seems to require editing these files directly (for example, an existing PR's baseline conflicts with `master`), stop and invoke `api-baselines` instead. Applies equally to the main agent, subagents, and any generated scripts.
