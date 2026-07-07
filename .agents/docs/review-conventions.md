## Review conventions

Shared vocabulary for `/review-pr` and `/verify-review` and their subagents. Any rule here is authoritative — the skills and subagents should reference this file rather than restate the rules inline.

### Severity

| ID   | Name       | Use when… |
|------|------------|-----------|
| BLK  | Blocker    | Correctness bug, data loss, security hole, broken public API outside `LinqToDB.Internal.*` (unless overridden by milestone — see `api-surface-classification.md`), broken build, crash. |
| MAJ  | Major      | Wrong behavior in a real scenario, misleading SQL for a supported provider, missing test for a fix that clearly needs one, missing XML doc on a new public (non-`Internal.*`) member, new public API with no test. |
| MIN  | Minor      | Design or maintainability issue, confusing naming, dead code, scope creep. |
| SUG  | Suggestion | Non-blocking alternative approach, refactor idea. |
| NIT  | Nit        | Style, typos, XML-doc wording. |

### Finding IDs

- Form: `<SEV><NNN>` — severity abbreviation followed by a zero-padded 3-digit number.
- Regex for parsing anywhere in text: `\b(BLK|MAJ|MIN|SUG|NIT)(\d{3})\b`.
- Numbering is **independent per severity** (e.g. `BLK001`, `BLK002`, `MAJ001`, `NIT001`).
- IDs are **unique across all reviews on a PR** — `/verify-review` must continue numbering, not restart.

### ID-continuation floor

Used by `/verify-review` (and by `/review-pr` when it finds prior reviews of its own on the PR).

1. Fetch all reviews on the PR authored by the current GitHub user (`gh api user --jq .login`) plus all their review comments.
2. Regex-scan every review body and every review comment body for finding IDs.
3. Per severity, compute `max(NNN)` across all matches.
4. The floor for that severity in the next review is `max + 1`. If no prior matches for a severity, floor is `001`.

**Field name (when computing from `pr-context.ps1` output).** Each entry in `reviews[]` / `reviewComments[]` carries the author login as a **flat `user`** field (the script flattens `user.login` → `user`). Filter with `$_.user -eq $currentUser` — **not** `author.login` or `user.login`, both of which are absent on these objects and silently match nothing, yielding floor `1` on every severity. That is a reproduced cause of ID collisions when re-reviewing a PR the current user already reviewed (surfaced on PR #5450: a fresh `/review-pr` reused `MIN001` / `NIT001` already spent by prior reviews). Also scan `reviewComments[].body`, not just review bodies — findings posted as line / file comments carry their IDs there, not in the review body.

### Checkbox semantics

All notes and all body-section findings carry a GitHub task-list checkbox. Per-line and per-file comments do **not** carry a checkbox — the thread-resolved state plays that role.

| State | Meaning |
|-------|---------|
| `[ ]` | Open — not yet addressed. |
| `[x]` | Fully addressed. Set by `/verify-review` after it confirms the fix. |
| `[~]` | Partially or incorrectly addressed. Set by `/verify-review`; a follow-up finding is posted separately. |

### Notes vs findings

- **Note** — an informational item in the review body header, not tied to a severity. Always a checkbox. Example: "API baselines need a refresh."
  - **Never** emit a note (or any other review output) describing a merge / sync with `master` or any other upstream branch, including mentioning which already-merged PRs are absorbed through that merge. Routine branch maintenance is not in scope — the diff that comes with it was already reviewed when it landed on `master`. This applies even if the merge brought in substantial content, conflict resolutions, or visibly changed the PR's file list.
- **Finding** — an issue identified in the PR. Has a severity and an ID. Location is one of:
  - **Per-line** — attached to a file+line via a review comment. No checkbox.
  - **Per-file** — attached to a file (no specific line) via a file-level review comment. No checkbox.
  - **Body-section** — placed in the review body under the appropriate severity heading. Has a checkbox.

### Audience — write for a human reader with only GitHub

Review bodies and comment bodies are rendered on the GitHub PR page for a maintainer who has a shell, `git`, `gh`, and the repo — **not** Claude Code, not the skill files, and not the `.agents/docs/*.md` instruction set. Every sentence you write must be actionable at that level.

- **Don't tell the reader to run a skill.** `/api-baselines`, `/review-pr`, `/verify-review`, etc. only work inside a Claude Code session. Instead describe the underlying action in plain tooling terms — e.g. "regenerate `Source/**/CompatibilitySuppressions.xml` by running `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` under `Source/`".
- **Don't cite `.agents/docs/...` paths as authority.** The reader may not have access to those files and will not open them to resolve a finding. If the underlying rule comes from a design invariant, restate the rule itself ("public API is a stability contract") rather than pointing at the doc that documents it. Acceptable references in comment bodies are: repo-root paths a maintainer would actually open (`Source/...`, `Tests/...`, `Directory.Build.props`), commit SHAs on the PR, line ranges on changed files, linked issue / PR numbers, and primary-source URLs (vendor docs, RFCs).
- **Don't reference subagent names or internal tooling.** `code-reviewer`, `baselines-reviewer`, `diff-reader.ps1`, `verify-lines.ps1`, `post-pr-review.ps1`, `_shared.ps1`, `writeDir`, `.build/.agents/...` are all internal to the review pipeline and meaningless on GitHub.
- **Keep meta-structure internal.** The ID-continuation floor, the "per-severity numbering" explanation, the "audit of structural vs. textual suggestions" tally — these are bookkeeping for the next agent run, not for the human reader. Never surface them in the review body.

If you catch yourself writing "run the X skill" or "per `.agents/docs/Y`", stop and rewrite the sentence as a direct instruction or a self-contained rule restatement.

### Completeness — never suppress findings to manage noise

Every legitimate finding gets emitted at its true severity. Do not propose, accept, or implement mechanisms that drop findings to make a review easier to scan: no noise budgets, no "70% silence" rules, no "limit Minor findings to N per PR", no "skip nits when total findings > X".

When a review feels long, the levers are *better severity classification* (correctly demoting a misclassified MAJ to MIN), *better grouping* (cluster related findings under a parent heading), or *better dedup* (merge findings that say the same thing). Never *omission* — masking real issues to manage attention trades visible noise for invisible escapes.

When the user says a review is too long, ask: are the findings *wrong / duplicate*, or just *numerous*? Only the former is the reviewer's problem to fix.

### Output body structure

Review body sections, in order. Omit any section that has no items.

The review **must** lead with the agentic-review disclaimer block below — it is not optional and not abbreviatable. It sets reader expectations before any finding.

```
> [!IMPORTANT]
> **Agentic review — treat with care.** This review was produced by an LLM agent rather than a human reviewer, and is **not** the final word: the PR will also receive a human review, so this complements — rather than replaces — human judgement. Individual findings can be wrong, overconfident, or miss context a human would catch. Feel free to disagree, dismiss, or ask for clarification on any finding — the agent's judgement is not authoritative. Findings are starting points for discussion, not verdicts.

## Review notes
- [ ] <open note>
- [x] <satisfied note>

## Prior-review audit
- **Fixed** · <author> — <claim, ≤1 line> (<thread link / "summary">)
- **Inaccurate** · <author> — <claim> — <correct reading>
- **Still actual** · <author> — <claim> (carried into findings below as <ID>)

## Findings (not tied to a specific line)
### Blockers (BLK)
- [ ] **BLK001** — <title>
  Why: <why>
  Fix: <fix>
### Major (MAJ)
### Minor (MIN)
### Suggestion (SUG)
### Nit (NIT)

## Out-of-scope observations
- **<title>** — <description>

## Baselines
<from baselines-reviewer output, or a single line when skipped / missing>
```

The `## Prior-review audit` section is populated from the step-2b audit of prior reviews by **other** authors (bots + humans) — see `review-bot-claim-audit.md`. One line per audited inline thread and per audited review-body-summary claim, prefixed with its verdict (`Fixed` / `Inaccurate` / `Still actual`) and the author. Omit the section entirely when there are no prior reviews from other authors to audit. Still-actual items are also carried into the regular finding stream; cite the assigned finding ID on the audit line so the reader can follow it down.

The `## Out-of-scope observations` section is populated from `code-reviewer`'s `out_of_scope_observations[]` output and only appears when that array is non-empty. Entries have no severity, no checkbox, and no line anchor — they are FYI observations about behavior that exists on `master` without the PR, surfaced because a reviewer might find them useful context. See `.agents/agents/code-reviewer.md` → **Scope discipline** for what qualifies.

`/verify-review` prepends a verification-update section before the above when posting a follow-up review — see that skill's body template.

The severity headings in the review body spell out each abbreviation (`### Blockers (BLK)`, `### Major (MAJ)`, etc.), so a human reader on GitHub can decode the finding IDs without access to this doc. Per-line and per-file comments additionally carry the spelled-out severity inline, since they render without a parent section header.
