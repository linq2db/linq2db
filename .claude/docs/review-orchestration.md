## Review orchestration — shared skeleton

Common orchestration reused by `/review-pr` and `/verify-review`. Everything in this doc is skill-agnostic: the mode-specific logic (scope confirmation, prior-findings parsing, per-finding action table, etc.) lives in each skill's own `SKILL.md`. This doc is the single source of truth for the steps that are word-for-word identical between the two skills.

### Permission-prompt discipline

Every Bash call is evaluated against the allowlist in `.claude/settings.local.json`. Pipes, redirects, inline `pwsh -Command`, `cat` / `head` / `tail`, or `ls` on directories whose layout is already documented each fire a prompt. Before writing a helper script to extract data from a JSON file, ask whether `Grep` on the dumped JSON or `Read` on the file would return the same information — the answer is almost always yes. See [`agent-rules.md`](agent-rules.md) → **Permission-friendly Bash patterns** for the full table.

### Resolving the target PR

Follow [`pr-resolver.md`](pr-resolver.md). The resolver returns the PR **number** only — no standalone `gh pr view` call, because the subsequent context load returns full metadata as part of its main response. If the input branch has no PR:

- `/review-pr`: stop and propose creating one (per `agent-rules.md` → **Pull request rules**).
- `/verify-review`: stop — there's nothing to verify.

### Loading PR context

One call does all of it:

```
pwsh -NoProfile -File .claude/scripts/pr-context.ps1 <<'EOF'
{ "pr": <n> }
EOF
```

Execute the three sections of [`pr-context-prep.md`](pr-context-prep.md) in order: **Context load** (the one script call), **Change summary**, **Baselines clone setup**. Both skills need all three — draft PRs are no different from ready-for-review PRs.

### Spawning the two subagents in parallel

Launch `code-reviewer` and `baselines-reviewer` in a **single assistant turn with two Agent tool calls** so they run concurrently. Never sequence them.

Common fields across both modes, supplied by either skill:

- **`code-reviewer` briefing**
  - PR metadata, linked issues + comments, prior reviews/comments (from the context load).
  - Change summary (from the context load).
  - Head ref / base ref (`origin/pr/<n>`, `origin/master`) and the file list from `nameStatus`. The subagent reads content via `.claude/scripts/diff-reader.ps1` — do not paste the diff into the briefing.
  - `writeDir: .build/.claude/pr<n>` — mandatory on the first `diff-reader.ps1` call so full file bodies land on disk for `Read` / `Grep` navigation.
  - ID-continuation floor per severity (see [`review-conventions.md`](review-conventions.md) → **ID-continuation floor**).
- **`baselines-reviewer` briefing**
  - PR number and head branch.
  - Baselines clone path: `../linq2db.baselines`.
  - Baselines branch: `baselines/pr_<n>`.
  - Change summary.

Mode-specific additions — `scope` for `initial`, `prior_findings` for `verify` — are the only per-skill differences. Each skill adds its own `mode: initial` or `mode: verify` field.

### Classifying public-API surface changes

Apply the decision tree in [`api-surface-classification.md`](api-surface-classification.md) to the `api_changes` returned by `code-reviewer`, using the PR's milestone title and file list from the context load. Produces the deduplicated refresh note and any milestone-gated findings. Both skills run this against **fresh** `api_changes` — never reuse classification from an earlier cycle.

Compute the `suppressions_updated` flag by filtering the in-memory `nameStatus` array for entries matching `Source/**/CompatibilitySuppressions.xml`. Do **not** re-run `git diff --name-only | grep` — the data is already in hand and the pipe would prompt on the allowlist.

### Posting via the wrapper

All posting (initial review, verification follow-up, body PUTs, thread resolves) goes through scripts under `.claude/scripts/`. Mechanics — manifest-script format, invocation, manifest-to-finding mapping, verify semantics, heredoc caveats, and the stdout reporting shape — are defined in [`review-posting.md`](review-posting.md). Each skill supplies only the per-review content that fills the manifest template:

- `/review-pr` → `.build/.claude/pr<n>-manifest.ps1` via `post-pr-review.ps1`.
- `/verify-review` → `.build/.claude/pr<n>-verify-manifest.ps1` via `post-pr-review.ps1`, plus `.claude/scripts/apply-verify-writes.ps1` for prior-review in-place edits.

### Command-usage audit (closing step)

After the draft review (and, for `/verify-review`, its in-place edits) have been reported, ask the user (single prompt):

> Run a command-usage audit for this session? Identifies unnecessary/duplicate commands, opportunities to fold calls into existing scripts, and allowlist/guardrail gaps. [y/N]

On `y`: walk back through the Bash / `gh` / `git` / `pwsh` calls the skill issued in this session. Both `code-reviewer` and `baselines-reviewer` return `callLog[]` — include their entries too, tagged with the subagent name. For each call, classify as:

- **Necessary** — no-op, leave as-is.
- **Redundant** — already covered by a prior call's output or an existing script's output; recommend removing.
- **Batchable** — multiple calls with the same shape that could fold into a single manifest-driven script call; recommend the new / extended script.
- **Guardrail gap** — a call that should have been blocked by `agent-rules.md` or the allowlist but wasn't; recommend the guardrail update.

Report as a table plus a prioritised follow-up list. Do **not** implement fixes in this turn — propose, then wait for a second explicit go-ahead. Multi-file edits to skills / scripts / docs are not something to batch into a review run.

On `N` (or silent): end without further action.
