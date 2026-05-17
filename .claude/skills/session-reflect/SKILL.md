---
name: session-reflect
description: Harvest the current conversation for insights worth capturing. Scans the transcript for user corrections, confirmed approaches, ad-hoc procedures, permission-prompt patterns, and knowledge gaps, then proposes concrete additions to `.claude/` (docs, skills, agents, scripts, feedback rules) or the user-level auto-memory system. Read-only until per-finding confirmation.
---

# /session-reflect

User-triggered. Treats the current conversation as the raw material for durable knowledge: things learned, patterns noticed, rules the user corrected you on, procedures you invented on the fly. Sorts candidates by where they belong, shows the proposed additions, and waits for explicit go-ahead before writing anything.

This skill is the mirror of `/audit-claude`. Audit finds drift in existing instructions; reflect proposes *new* instructions based on this session.

## Scope

**In-scope source material.** Only the current conversation. The skill does not read prior session transcripts, commit history beyond this session's commits, or the memory directory's existing contents (beyond checking whether a proposed memory duplicates an existing one).

**Out-of-scope.** Anything that would require harness-level hooks (file-change diff against master, automatic end-of-session triggers, cross-session correlation). If the user wants those, the skill flags them as a limitation and suggests manual invocation instead.

## Shared reference material

- **Agent rules** (Bash, branching, commit, GitHub content): `.claude/docs/agent-rules.md`
- **Claude Code setup** (`.claude/` layout, settings): `.claude/docs/claude-setup.md`
- **Auto-memory system** — documented in the system prompt under *auto memory*; stored at the user-level memory directory (`C:\Users\<user>\.claude\projects\<repo>\memory\`) with an `MEMORY.md` index plus per-fact `.md` files using the four types `user` / `feedback` / `project` / `reference`.
- **Sibling skills** to propose hooking into: `/audit-claude` (static lint), `/fewer-permission-prompts` (permission allowlist tuning), `/update-slnx` (solution sync).

## When to run

Only when the user explicitly asks to reflect / harvest / capture learnings from the current session. Do not run spontaneously at end-of-turn or when the context is nearly full.

Reasonable triggers (when the user might invoke this):
- A long session just concluded with several corrections / discoveries.
- A new skill or doc landed and the user wants to capture related learnings that weren't in its scope.
- The user noticed they keep re-explaining the same preference and wants it codified.

Bad triggers (don't invoke even if tempted):
- Generic end-of-turn reflection — there's rarely enough signal in a single-topic turn.
- Immediately after another session-reflect run — the sources overlap and the same findings will surface.

## Six candidate buckets

Every finding gets routed to exactly one bucket plus a destination (project `.claude/` or user-level auto-memory). Severity here is about **how confident** we are that the signal is real, not about urgency.

| Bucket | Destination | What it looks like | Severity |
|---|---|---|---|
| **feedback** | auto-memory (if personal) / `.claude/docs/agent-rules.md` or a skill's "Don'ts" section (if project-scoped) | User corrected an approach ("stop summarizing"; "don't mock the DB"; "use Y instead of X"). Also includes **confirmations** of non-obvious approaches the user explicitly validated ("yes, exactly that"). | strong / medium / weak |
| **doc** | `.claude/docs/<new-or-existing>.md` | Knowledge you had to grep / web-search / ask the user for that isn't documented. Cross-cutting facts about the codebase, external tools, workflows. | strong / medium / weak |
| **script** | `.claude/scripts/<name>.ps1` | A multi-step Bash / `gh` / `git` sequence invoked more than once, or a one-shot sequence complex enough that repeating it would incur permission prompts. Follows the contract in `agent-rules.md` → *PowerShell Core scripts*. | strong / medium / weak |
| **skill** | `.claude/skills/<name>/SKILL.md` | A user-triggered workflow that appeared in this session (either done manually or ad-hoc) and is likely to recur. Must be user-invoked (the `/command` shape), not a behind-the-scenes agent. | strong / medium / weak |
| **agent** | `.claude/agents/<name>.md` | A specialized read-or-write task delegated to a subagent during this session (or that *should* have been delegated). Typically created when a task's tool profile is narrower than the main agent's. | strong / medium / weak |
| **permission** | instruction edit (`agent-rules.md`) OR script creation / extension (`.claude/scripts/`) OR allowlist (defer to `/fewer-permission-prompts`). Pick per **Diagnosing permission prompts** below. | Bash patterns that triggered permission prompts this session. Analyze root cause per prompt — don't just aggregate. | strong / medium / weak |

### Strong vs medium vs weak

- **Strong** — clear signal, low false-positive risk. User explicitly stated the rule, or the exact same pattern recurred 3+ times, or a correction was made with "always" / "never" framing.
- **Medium** — one clear instance, could reasonably be codified, but isn't obviously general. Worth flagging; lean toward **info** tone in the report.
- **Weak** — noticed only once, ambiguous generality. Mention as a footnote, don't propose a specific patch.

Skip weak findings unless the user asks for full coverage.

## Routing: project `.claude/` vs auto-memory

This is the hardest judgment call in the skill — a bad routing pollutes one system with content meant for the other. Default heuristics:

| Content type | Goes to |
|---|---|
| Facts about *this codebase* (linq2db architecture, conventions, invariants) | `.claude/` |
| Facts about *this user's role / preferences / workflow* | auto-memory |
| Procedural rules that any agent working on linq2db should follow | `.claude/` (usually `agent-rules.md` or a skill's "Don'ts") |
| Personal quirks of this user's workflow that aren't universally right | auto-memory (`feedback` type) |
| External resources (issue tracker, Slack, dashboards) | auto-memory (`reference` type) |
| Current-project state (in-progress initiatives, deadlines) | auto-memory (`project` type) |

When a finding could plausibly go either way (e.g. "user prefers X over Y" — is that personal or project consensus?), ask the user explicitly during the per-finding confirmation step.

**Never write to auto-memory without routing confirmation.** The auto-memory directory is user-owned and persists across sessions for this user only; silently adding project-wide content there means the rule won't apply for other users of the same codebase.

## Diagnosing permission prompts

A permission prompt is a signal the agent picked the wrong shape, not just that an allowlist entry is missing. Triage each prompted command one of four ways, in priority order:

1. **Wrong tool.** The agent used `grep` / `cat` / `head` / `find` / `sed` when a dedicated Claude Code tool (`Grep`, `Read`, `Glob`, `Edit`) would have been both cheaper and non-prompting. Fix: add or sharpen a rule in `agent-rules.md` → **Dedicated tools over raw CLI**; don't allowlist the raw CLI variant.
2. **Pipe / redirect / compound.** A `|`, `>`, `;`, `&&` broke allowlist matching that would otherwise have worked on each half. Fix: rewrite with the command's own flag (`gh api --jq`, `git log -n N`, `Grep head_limit`) — surface the pattern under `agent-rules.md` → **Avoiding pipes** or **Permission-friendly Bash patterns**.
3. **Multi-step ad-hoc sequence.** The agent invoked 3+ related Bash calls that passed data between them (load → transform → post). Fix: propose a new PowerShell script under `.claude/scripts/` per **PowerShell Core scripts for complex operations**. If a similar script already exists, propose extending it rather than spawning a new one.
4. **Genuinely novel, one-off, unavoidable.** None of the above fits; the command really is a plain single-step operation that isn't on the allowlist. Fix: defer to `/fewer-permission-prompts` for an allowlist addition — this is the **fallback**, not the default.

When in doubt between 1–3 and 4, prefer the allowlist fix (category 4) only when at least two similar calls prompted in the session. A single prompt on a genuinely one-off command is acceptable cost and doesn't warrant an allowlist entry.

For each prompt, report which category + the proposed edit path. Aggregate the fallback (category 4) counts into the single `/fewer-permission-prompts` recommendation at the end.

## Steps

### 1. Survey the transcript

Build an internal index of session signals:

- **Corrections** — messages where the user said "no", "stop", "don't", "not that", "instead", "use X", corrected a tool call, rejected a proposed plan, or narrowed scope after you went broad.
- **Confirmations** — messages where the user said "yes exactly", "that's right", "good call", "keep doing that", or accepted an unusual approach without pushback.
- **Discovered facts** — things you had to read / grep / web-search for that weren't in `.claude/docs/` or `CLAUDE.md`. Pay attention to what sent you searching: those are documentation gaps.
- **Ad-hoc procedures** — multi-step sequences of Bash / `gh` / `git` / tool calls you composed by hand that might repeat. If you invoked the same pattern 2+ times, it's a script candidate.
- **Permission prompts** — tool calls you issued that required user approval. Include command prefix + whether an allowlist entry would have avoided it.
- **Delegated vs inline work** — tasks you handled in the main thread that would have fit a subagent's profile better.
- **User-triggered workflows** — you noticed the user repeat the same shape of request 2+ times; that's a skill candidate.

Read the transcript forward once, keeping this index in memory. For a 50+ turn conversation, note structure: which early turns set up the goal, which middle turns drove correction, which late turns produced confirmed approaches. Corrections that came *before* confirmations matter more than corrections the user later walked back.

### 2. Classify each signal into a bucket + destination

Run every indexed signal through the bucket table. A single signal can produce at most one finding (if it produces more than one, pick the most specific destination — `.claude/skills/<name>/SKILL.md` beats `.claude/docs/agent-rules.md` beats auto-memory, when the signal fits all three).

Emit a finding record:

```json
{
  "id": "<bucket>-<short-slug>",
  "bucket": "feedback|doc|script|skill|agent|permission",
  "destination": "<file path relative to repo root, or `memory:<file>`>",
  "severity": "strong|medium|weak",
  "signal": "<the transcript evidence — quote the user or cite the turn>",
  "rationale": "<why this crosses the threshold for capture>",
  "proposedContent": "<draft of the text to add, or null for aggregated buckets like permission>",
  "insertStrategy": "append|insert-under-heading|new-file|route-to-skill",
  "routingConfirmationNeeded": true|false
}
```

Skip weak findings by default. Show them in a footnote only.

### 3. Check for duplicates against existing content

Before presenting, for each finding:

- **`.claude/` destinations** — `Grep` the target file (or the relevant dir) for overlap with `proposedContent`. If the rule is already in place with similar wording, drop the finding; if it's in place with diverging wording, switch the bucket to `feedback` and route to `/audit-claude`'s duplicated-rule category instead.
- **auto-memory destinations** — `Read` the memory directory's `MEMORY.md` index + any same-title file. If an existing memory covers it, propose an **update** to the existing file rather than a new one. If it contradicts an existing memory, surface the conflict to the user and ask how to reconcile.

This step kills the most common false-positive of the skill: proposing a memory that already exists with slightly different wording.

### 4. Assemble the report

Numbered list grouped by bucket, strong findings first. Per-finding line:

```
[<bucket>:<severity>] <destination> — <one-line summary>
  Signal: <quote>
  Proposal: <2–4 lines or "see detail block"; for long proposals, reference a footnote>
  Routing: project / personal / ambiguous (needs confirmation)
```

Print a footnote block for weak findings with a "worth mentioning?" one-liner each.

### 5. Offer patches (per-finding flow)

Default mode: per-finding confirmation. For each strong finding, prompt:

- `apply` — write the proposed content (`Edit` for an existing file, `Write` for a new one). For memory destinations, follow the two-step memory protocol: write the per-fact file, add the pointer line to `MEMORY.md`. Never write directly into `MEMORY.md` beyond the pointer line.
- `skip` — drop this finding.
- `revise` — user edits the proposed content inline; apply the edited version.
- `reroute` — move destination between project `.claude/` and auto-memory; re-show for confirmation.
- `batch-strong` — apply all remaining **strong** findings without further prompt. Medium and ambiguous-routing findings still pause.
- `abort` — stop the loop; already-applied edits remain.

For findings where `routingConfirmationNeeded: true`, ask the routing question first ("this looks personal — memory, or `.claude/agent-rules.md`?") before showing the patch.

For `permission` bucket findings, triage each prompt per **Diagnosing permission prompts** above. Categories 1–3 (wrong-tool / pipe / ad-hoc sequence) produce individual patches — treat them like any other finding in the per-finding flow. Category 4 (genuinely novel one-off) aggregates into a single recommendation at the end:

> 4 of this session's permission prompts fell in the allowlist-fallback category (mostly `gh api repos/.../comments`). Run `/fewer-permission-prompts` to emit a suggested allowlist.

### 6. Report

End with:
- **Applied:** N findings (by bucket, with destination paths so the user can review).
- **Skipped / deferred:** M findings.
- **Rerouted:** findings whose destination changed (from proposed).
- **Permission candidates:** total count, next-step pointer.
- **Limitations noted this session:** any signal you wanted to capture but couldn't cleanly classify.

Don't commit. Per `.claude/docs/agent-rules.md` → *Git commit rules*, commits need an explicit user request. For auto-memory writes, remind the user that those files aren't git-tracked (they live under the user's home directory) and take effect in the next session automatically.

## Don'ts

- Do not run spontaneously. Even if the current turn "feels like" a reflection opportunity, wait for `/session-reflect`.
- Do not write to the user's auto-memory directory without explicit routing confirmation. Personal state is sensitive and cross-session.
- Do not overwrite existing memory files or existing `.claude/` content. Propose an **update**, show the diff, wait for approval.
- Do not generate findings for things already documented — the duplicate check in step 3 is not optional.
- Do not propose a new skill / agent when an existing skill already covers the workflow (e.g. a bash-allowlist candidate belongs with `/fewer-permission-prompts`, not a new skill).
- Do not flag grammatical or stylistic preferences. This skill captures behavior and knowledge, not writing-style drift.
- Do not commit. Changes stay in the working tree (for `.claude/`) or under the user home directory (for memory). The user commits on their terms.
- Do not emit weak findings as first-class proposals. They're footnotes; if the user wants to pursue one they'll say so.
