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
| `gh api ... -f body=@<file>` to PATCH a comment body from a markdown file | Build JSON via pwsh `@{body=Get-Content -Raw <md>} \| ConvertTo-Json -Compress \| Set-Content <json>` then `gh api --method PATCH ... --input <json>` | `-f`'s `@<file>` form is **not** interpreted — it stores the literal string `@<file>` as the body. Same trap as `gh … --body @-` (already banned above). The `@<file>` shorthand only works on a few specific gh flags (`--body-file`, etc.); for REST PATCH bodies use `--input` with a JSON wrapper file. |

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

### Batching and user interaction

Reduce round-trips and preserve the user's attention span.

- **Batch independent tool calls.** When multiple reads, searches, or shell commands don't depend on each other's output, issue them in a single assistant turn (multiple tool calls in one message). Sequential calls are only for true dependencies. This applies to Read, Grep, Glob, Bash, and any other non-mutating tool.
- **Ask-ask-do-all, not ask-do-ask-do.** When a task requires multiple user decisions, don't interleave question → action → question → action. Front-load every question you can anticipate into a single turn (numbered list so the user can reply by number), wait for all answers, then execute all resulting actions in one batch. Only fall back to interleaving when a later question genuinely depends on the outcome of an earlier action.
- **Do not batch code-change reviews.** Each unrelated code change should be proposed in its own review turn, even if that means more round-trips. Mixing several unrelated diffs into one confirmation forces the user to context-switch between concerns and makes "approve partially" awkward. Group diffs only when they belong to the same logical change.
- **On a surprising failure, stop and wait.** If a command, test run, or agent invocation fails in a way the plan didn't anticipate (connection refused, unexpected parser error, container not running, permission denied, etc.), don't improvise alternative paths to keep the session flowing — report what happened in one or two sentences and wait for user direction. Workarounds invented mid-failure often mask a real signal (wrong premise, wrong tool, wrong target); the user's redirect is usually faster than the bot's recovery.
- **Batch edits on a single config file.** When reshaping multiple sections of one file (enabling / disabling several providers across TFMs in `UserDataProviders.json`, toggling several `<PackageReference>` versions in `Directory.Build.props`, rewriting a handful of keys in `settings.json`, etc.), read the file once, plan the full set of edits, then apply them as a single `Edit` call with enough surrounding context to disambiguate each target — or, when `Edit` can't cover multiple distinct section headings in one shot, a back-to-back sequence with no intermediate re-reads. Incremental nibbles — edit a line, read back, edit another line, read back — burn permission surface and miss cluster-level invariants across the sections.

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

- **Never run `git commit` without an explicit user request.** "Explicit" means the user told you to commit in the current turn (e.g. "commit", "commit this", "commit changes"). Finishing edits, passing tests, or a clean working tree are not requests. When in doubt, stop and ask.
- This applies even when the preceding turn ended with a commit — each new change needs its own explicit go-ahead.
- Same rule for `git push`, `git tag`, `gh pr create`, and any other publishing action.
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

- **Never `git push` without an explicit user request.** Same rule as commits — each push needs its own go-ahead.
- **After every successful push**, check for a PR on that branch (`gh pr list --head <branch> --json number,title,body,url`):
  - If **no PR exists**, propose creating one (see **Pull request rules**) and wait for confirmation.
  - If **a PR exists**, diff the newly pushed commits against the current PR body. If the body no longer accurately describes the work (new summary bullets, new linked issues, etc.), propose a concrete edit and wait for confirmation before calling `gh pr edit`. **Show the proposed change as a diff between the current body and the new one** (e.g. a unified diff or `- old line` / `+ new line` markers) — do not just paste the new body in full. If the body is still accurate, say so and move on — don't edit gratuitously.
  - **When the body update follows a follow-up commit on the user's own PR, append — don't rewrite.** Add a new subsection (typically `## Follow-up commit` or similar) summarising the new commit's deltas and leave the original prose verbatim. Don't paraphrase, restructure, or "neutralise" content the human author already wrote. The "preserve, don't rewrite" rule is suspended only when the user explicitly asks for a tone or structure change to the existing body.

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
- **Commits that extend an open PR's scope go on that PR's branch**, not a new parallel branch. When a review session surfaces an ancillary fix (apostrophe-escape bug found while reviewing #5463, a test regression caused by the PR, a missing guardrail) and the user asks for it as a follow-up, push it onto the PR's existing head branch — don't create a sibling `feature/*` branch and propose a second PR. Mechanics:
  - Check `gh pr view <n> --json maintainerCanModify,headRepository,headRefName`. If `maintainerCanModify: true` and `headRepository` is a fork, add the author's fork as a git remote if not already present (`git remote add <owner> https://github.com/<owner>/<repo>.git`) and push via refspec: `git push <owner> <local-branch>:<headRefName>`. The PR auto-updates with the new commit. Propose a body update when the new commit extends the PR's originally described scope (follow the **Push to remote rules** diff-based flow).
  - If `maintainerCanModify: false`, stop and ask — either the author has to apply the change themselves, or the work needs a separate PR. Don't unilaterally open a parallel branch when the intent was a follow-up commit.
  - When pushing to someone else's fork, neutralize accidental pushes afterward if the remote is no longer needed (`git remote set-url --push <owner> no_push` as a guard, or `git remote remove <owner>` if you want it gone). Confirm with the user which — "disable" can mean either.

### Docker containers: start/stop/create only

Provider docker containers (`oracle11`, `hana2`, `postgres*`, `mysql*`, `db2`, etc.) are managed by the user; the agent's scope is limited to `docker start` / `docker stop` / `docker create` / `docker ps` to see state. **Do not** read docker-compose files, `docker inspect` env/config, read setup scripts under `Build/`, or propose changes to container configuration. Connection strings in `UserDataProviders.json` are the authoritative spec — trust them even when hostnames don't resolve locally.

If a test fails to connect after the container is started, report the failure and wait for user direction. Don't chase credentials / ports / hostnames by inspecting the container — it usually ends in guessing at a setup that doesn't match the user's actual environment.

**Scope-change prompt for session-started containers.** Every `docker start <name>` run during the session is captured by the `track-docker-start` PostToolUse hook into `.build/.claude/docker-session-started.txt`; the `cleanup-docker-session` SessionEnd hook stops each of them when the session exits. Before running a command that changes working-tree scope — `git checkout`, `git switch`, `git worktree add`, `gh pr checkout`, or invoking a skill that switches branches for you (`/fix-issue`, a different-PR `/review-pr`, etc.) — read that state file. If it lists containers the session started, stop and ask the user whether to stop them before the scope change; name the containers in the question. Do not stop them silently — scope change doesn't always mean the user is done with the providers. Containers that were already running when the session started are not tracked and are out of scope.

### GitHub content authored by others

Never edit, PATCH, or overwrite GitHub content authored by a user other than the current `gh`-authenticated user. This covers:

- issue bodies
- PR bodies
- issue-comment bodies
- review-comment bodies
- commit messages
- CHANGELOG entries attributed to others (only amend your own lines)

To respond to or add to someone else's content, post a new comment / reply / review — don't modify the original. Retractions and corrections happen in a reply on the same thread, not by overwriting the thing you're retracting.

Retraction mechanics (also for your **own** submitted review/comment — overwriting erases public history):

- **Check `state` + `submitted_at` before any `PUT` / `PATCH`** (`gh api repos/<o>/<r>/pulls/<n>/reviews/<id> --jq '{state, submitted_at}'`). A submitted review (`submitted_at` populated, `state` ∈ {APPROVED, CHANGES_REQUESTED, COMMENTED}) must be retracted via reply, not edited; a truly `PENDING` (`submitted_at: null`) is still editable in place.
- **Line / file review comments:** reply via `POST /repos/{o}/{r}/pulls/{n}/comments/{comment_id}/replies` (or GraphQL `addPullRequestReviewComment` with `inReplyTo`). Body starts with `Retraction:` or `Correction:` and states the correct reading in one line.
- **Review body (top-level):** post a new review or PR issue comment that references the prior; never `PUT` the original.
- Exception: typo / broken-link / formatting-only fixes that don't change meaning are OK to edit in place.

Metadata changes — closing/reopening, labels, milestones, assignees — are **not** content edits and remain allowed under their usual confirmation rules (commits need explicit user ask, pushes need explicit user ask, etc.).

**After any manual `gh api PATCH` / `PUT` on a comment or review body, re-fetch and verify.** The API's success response only confirms the request was accepted — it doesn't confirm the body you intended was actually stored. Two known traps:

- `gh api -f body=@<file>` does **not** read the file; it stores the literal string `@<file>` as the body. Same trap as `gh … --body @-`. Use `--input <json-file>` with a properly-escaped wrapper instead — build it via pwsh (`@{body=Get-Content -Raw <md>} | ConvertTo-Json -Compress | Set-Content <json>`), then `gh api --method PATCH ... --input <json>`.
- Stdin encoding via Bash pipes can mangle non-ASCII (em-dash → `ΓÇö` etc.) on Windows.

After every manual PATCH/PUT, run `gh api repos/<o>/<r>/issues/comments/<id> --jq '.body[:200]'` (or equivalent) and confirm the prefix matches what you intended. Skill-driven posts via `post-pr-review.ps1` already do this byte-compare via `verify: true`; manual calls don't, so verify by hand.

### GitHub wording discipline

Issue bodies, PR bodies, review comments, and replies are terse and fact-dense — a record of what changed and why, not a place for framing, apologies, or summaries of what the diff already shows.

**Cut:** restating the diff in prose; apologetic framing ("sorry for the churn", "I wasn't sure"); puffed adjectives ("comprehensive", "robust", "clean", "thorough", "elegant", "proper" — replace with the concrete fact or drop); anticipatory reassurance ("I made sure not to break anything"); meta-narrative about the process ("I originally tried X then switched to Y" belongs in a commit message at most).

**Keep:** what changed (bullets, imperative); why it changed (constraint / upstream / linked issue, with a link); non-obvious trade-offs the reviewer must notice (new public type, deferred test-plan item, baselines refresh); `Fixes #<n>` / `Closes #<n>` for auto-closing.

Review comments: lead with `**<Severity> · <ID>**`, state the finding, state the fix — no "I noticed that…" / "this might be worth looking at…", the severity label already says "I think this matters". Retraction / correction replies: state what was wrong, the correct reading, one link to evidence — no apologies (the retraction is the apology). Your own prior posts authored by the current `gh` user are editable without this guardrail applying.

**Provider behavior claims must be verified against translator code.** When agent-authored prose (review bodies, release-notes drafts, PR comments) makes a specific claim about how a provider translates a member or operation — e.g. "SQL Server 2016+ `DateTimeOffset.UtcNow` emits `SYSDATETIMEOFFSET() AT TIME ZONE 'UTC'`" — verify it by reading the relevant translator at PR HEAD (`Source/LinqToDB/Internal/DataProvider/<Provider>/Translation/<Provider>MemberTranslator.cs`) before posting. The base virtuals' default returns can mislead — e.g. `TranslateNow` defaults to `CURRENT_TIMESTAMP`, but most providers override it to return `null`, so claims like "DateTime.Now is server-side" depend on which providers actually inherit vs override. Don't rely on baseline diffs alone — they show what the test produces, not necessarily what every code path produces. Audit each per-provider claim against the actual override. The `code-reviewer.md` rule covers code-comment claims; this extends the discipline to agent-authored prose in review bodies and PR comments.

### Agent Guardrails

Operational rules for how agents should act on this codebase. The codebase design invariants these rules protect — public-API contract, cross-cutting internals, SQL AST namespace placement, column-aligned formatting — live in [`code-design.md`](code-design.md). Read that first; it defines what these guardrails exist to preserve.

- **Don't reformat, rename, or clean up unrelated code.** The repo's column-aligned formatting is intentional (see `code-design.md` → **Column-aligned formatting is intentional**). *Unrelated* is the key word: the rule forbids touching lines the current task doesn't already modify — it does **not** suppress review findings on lines the PR itself adds or modifies. On PR-introduced lines, flag any of: **trailing whitespace on a line**, **3+ consecutive blank lines**, **mixed tabs/spaces that visibly misalign**, or **indentation not matching the enclosing scope**. These are new noise the codebase didn't have before, and the fix is a one-line ```suggestion. Don't flag the same patterns on lines the PR doesn't touch — that *is* reformatting unrelated code.
- **Don't reshape cross-cutting internals for a local fix.** When a task scoped to one provider or test seems to need a change in the SQL AST, `IDataProvider`, or translator interfaces, raise the question explicitly before making the change — the blast radius is the whole product (see `code-design.md` → **Cross-cutting internals are shared**).
- **Surface trade-offs on non-local choices.** If a decision affects public API, generated SQL, or provider behavior, describe the options in the conversation rather than picking silently. For SQL AST signature changes specifically, also flag whether the type's current namespace placement is correct — see `code-design.md` → **SQL AST types live in `LinqToDB.Internal.SqlQuery`**.
- **Document arbitrary values explicitly.** If a change requires picking an arbitrary constant (timeout, threshold, version cutoff) or making an assumption, leave a short comment or `// TODO` at the call site so a reviewer can verify it. This is a deliberate exception to Claude Code's default "no comments" policy — the value is inherently questionable and the comment is the signal for review.
- **Never hand-edit API baseline files.** `Source/**/CompatibilitySuppressions.xml` is generated output owned by the ApiCompat tool. Do not use `Edit`, `Write`, `sed`, or any other direct mutation on these files — not to add/remove a single suppression, not to "fix up" formatting, not to resolve a merge conflict. The only supported way to change them is the `api-baselines` skill (`.claude/skills/api-baselines/SKILL.md`), which regenerates them via `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` and applies the `LinqToDB.Internal.*` policy check. If a task seems to require editing these files directly (for example, an existing PR's baseline conflicts with `master`), stop and invoke `api-baselines` instead. Applies equally to the main agent, subagents, and any generated scripts.
- **Default to script + doc guardrails before hooks.** When proposing a guardrail against a class of agent error (recurring footguns, silent encoding traps, mangling-prone CLI shapes), build a helper script under `.claude/scripts/` that encodes the right path **and** a blanket rule in this doc that surfaces the script — before reaching for a `PreToolUse` / `PostToolUse` / `SessionEnd` hook. Hooks are opt-in via `.claude/settings.local.json`, add harness surface area, and don't help users who haven't wired them in; scripts + rules cover everyone reading this file. Reach for a hook only when the user explicitly asks for one, or when the failure mode is genuinely undetectable from inside Claude (silent stdout corruption that no script can prevent because it happens after the agent has already typed the wrong thing).
