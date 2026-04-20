## Review conventions

Shared vocabulary for `/review-pr` and `/verify-review` and their subagents. Any rule here is authoritative — the skills and subagents should reference this file rather than restate the rules inline.

### Severity

| ID   | Name       | Use when… |
|------|------------|-----------|
| BLK  | Blocker    | Correctness bug, data loss, security hole, broken public API outside `LinqToDB.Internal.*` (unless overridden by milestone — see `api-surface-classification.md`), broken build, crash. |
| MAJ  | Major      | Wrong behavior in a real scenario, misleading SQL for a supported provider, missing test for a fix that clearly needs one. |
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

### Checkbox semantics

All notes and all body-section findings carry a GitHub task-list checkbox. Per-line and per-file comments do **not** carry a checkbox — the thread-resolved state plays that role.

| State | Meaning |
|-------|---------|
| `[ ]` | Open — not yet addressed. |
| `[x]` | Fully addressed. Set by `/verify-review` after it confirms the fix. |
| `[~]` | Partially or incorrectly addressed. Set by `/verify-review`; a follow-up finding is posted separately. |

### Notes vs findings

- **Note** — an informational item in the review body header, not tied to a severity. Always a checkbox. Example: "API baselines need a refresh."
- **Finding** — an issue identified in the PR. Has a severity and an ID. Location is one of:
  - **Per-line** — attached to a file+line via a review comment. No checkbox.
  - **Per-file** — attached to a file (no specific line) via a file-level review comment. No checkbox.
  - **Body-section** — placed in the review body under the appropriate severity heading. Has a checkbox.

### Output body structure

Review body sections, in order. Omit any section that has no items.

The review **must** lead with the agentic-review disclaimer block below — it is not optional and not abbreviatable. It sets reader expectations before any finding.

```
> [!IMPORTANT]
> **Agentic review — treat with care.** This review was produced by an LLM agent (`/review-pr`) rather than a human reviewer. Individual findings can be wrong, overconfident, or miss context a human would catch. Feel free to disagree, dismiss, or ask for clarification on any finding — the agent's judgement is not authoritative. Findings are starting points for discussion, not verdicts.

## Review notes
- [ ] <open note>
- [x] <satisfied note>

## Findings (not tied to a specific line)
### Blockers (BLK)
- [ ] **BLK001** — <title>
  Why: <why>
  Fix: <fix>
### Major (MAJ)
### Minor (MIN)
### Suggestion (SUG)
### Nit (NIT)

## Baselines
<from baselines-reviewer output, or a single line when skipped / missing>
```

`/verify-review` prepends a verification-update section before the above when posting a follow-up review — see that skill's body template.

No legend table is embedded in the review body. Reviewers who need the abbreviation map can consult this file.
