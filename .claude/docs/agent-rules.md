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

### Batching and user interaction

Reduce round-trips and preserve the user's attention span.

- **Batch independent tool calls.** When multiple reads, searches, or shell commands don't depend on each other's output, issue them in a single assistant turn (multiple tool calls in one message). Sequential calls are only for true dependencies. This applies to Read, Grep, Glob, Bash, and any other non-mutating tool.
- **Ask-ask-do-all, not ask-do-ask-do.** When a task requires multiple user decisions, don't interleave question → action → question → action. Front-load every question you can anticipate into a single turn (numbered list so the user can reply by number), wait for all answers, then execute all resulting actions in one batch. Only fall back to interleaving when a later question genuinely depends on the outcome of an earlier action.
- **Do not batch code-change reviews.** Each unrelated code change should be proposed in its own review turn, even if that means more round-trips. Mixing several unrelated diffs into one confirmation forces the user to context-switch between concerns and makes "approve partially" awkward. Group diffs only when they belong to the same logical change.

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

- **Preserve public API, architecture, and behavior.** This is a library — types, method signatures, and observable SQL output in `Source/LinqToDB/` are contracts. Don't modify them without a clear, explicit reason.
- **Don't touch cross-cutting internals for a local fix.** The SQL AST (`SqlQuery/`), `IDataProvider`, and translator interfaces (`Linq/Translation/`) are shared by every provider. A fix scoped to one provider or test shouldn't reshape them — raise the question first.
- **Don't reformat, rename, or clean up unrelated code.** Column-aligned formatting in this repo is intentional (see `CLAUDE.md` → **Code Conventions**). Only flag formatting when it is clearly broken: 3+ blank lines, mixed tabs/spaces causing misalignment, broken indentation.
- **Surface trade-offs on non-local choices.** If a decision affects public API, generated SQL, or provider behavior, describe the options rather than picking silently.
- **Document arbitrary values explicitly.** If a change requires picking an arbitrary constant (timeout, threshold, version cutoff) or making an assumption, leave a short comment or `// TODO` at the call site so a reviewer can verify it. This is a deliberate exception to Claude Code's default "no comments" policy — the value is inherently questionable and the comment is the signal for review.
