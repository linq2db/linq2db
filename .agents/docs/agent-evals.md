# Agent-tooling evals (design spec — not yet built)

**Status: proposed, not implemented.** This doc is a design spec for a component-level eval harness over the `.agents/` agent tooling, captured so the decision and shape are recorded. It is a **build**, not a doc edit — there is no harness, no CI wiring, and no eval cases yet. Don't treat any of the below as live; treat it as the proposal to accept, scope, or reject.

## Why

The corpus has skills, subagents, scripts, and a lot of prose rules, but **no automated check that the tooling behaves as intended**. Regressions are caught only when a human notices a skill misbehaving mid-task. The production-agent literature is consistent on one point: end-to-end "did the task succeed" metrics mask *where* a multi-step agent broke (retrieval vs tool-selection vs context vs output validation), and component-level evals run per change localize the failure. linq2db already trusts an objective oracle for product code — compiling tests + the baselines diff — but the *agent tooling that drives those tests* has no equivalent.

## Proposed minimal eval set

Small, fast, deterministic-where-possible checks — runnable as a smoke suite (target ~20–50 cases), per change to `.agents/`:

1. **Script contract / schema.** For each `.agents/scripts/*.ps1` with a JSON-in / JSON-out contract (`diff-reader.ps1`, `verify-lines.ps1`, `pr-context.ps1`, …), feed a fixture input and assert the output shape (required keys, types) matches the documented schema. Pure mechanical, no model.
2. **"I don't know" / refuse-to-fabricate.** Give a subagent a task it *cannot* complete from the provided inputs (a file that doesn't exist, a PR with no diff) and assert it returns the structured `blocked` / "what's missing" outcome rather than a plausible-looking fabricated result. This is the [`agent-rules.md`](agent-rules.md) → *Frame subagent prompts to allow failure* guardrail, turned into a test.
3. **Tool-selection sanity.** For a handful of canonical asks, assert the agent reaches for the dedicated tool (Grep / Read / Glob, the right script) rather than raw CLI — catches drift against *Dedicated tools over raw CLI*.
4. **Structured-output validation.** Where a subagent must emit a fenced JSON block (`code-reviewer`, `baselines-reviewer`), validate the emitted JSON against its schema on a fixture PR.
5. **Model-swap probe.** Re-run a small case set under a cheaper model tier and compare — answers the "is the expensive model actually load-bearing here, or would `sonnet`/`haiku` do?" question that the per-subagent `model:` pins currently answer only by assumption.
6. **Long-run state consistency** (optional, expensive). For a multi-step skill (`/release`, `kb-build`), assert state files stay consistent across a simulated resume — catches the "state drift after N steps" failure.

## Open questions before building

- **Runner + fixtures.** Where eval cases live (`.agents/evals/`?), how fixtures are pinned, and whether cases that invoke a model run in CI or only on demand (cost / non-determinism).
- **Determinism.** Items 1, 3, 4 can be deterministic; 2, 5, 6 involve a model and need tolerance / sampling, not exact-match.
- **CI integration.** Whether this runs in the linq2db CI (it touches no product code) or as a local `/`-skill the curator runs before landing `.agents/` changes on `infra/claude-curation`.
- **Worth-it threshold.** A corpus this size may not justify a full harness; the cheapest first step is items 1 + 2 (script-schema + refuse-to-fabricate) as a single script, measured before expanding.

Until built, the existing safeguards stand in for this: `git status` verification after every subagent ([`agent-rules.md`](agent-rules.md) → *Agent guardrails*), the `/audit-agents` static checks, and human review of `.agents/` edits.
