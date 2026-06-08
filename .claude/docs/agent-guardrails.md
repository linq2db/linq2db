# Agent guardrails

Operational rules for how agents should act on this codebase. The codebase design invariants these rules protect — public-API contract, cross-cutting internals, SQL AST namespace placement, column-aligned formatting — live in [`code-design.md`](code-design.md). Read that first; it defines what these guardrails exist to preserve.

The most-violated guardrails (don't reformat unrelated code; don't reshape cross-cutting internals; verify subagent output with `git status` after every invocation; never hand-edit API baseline files) stay inline in [`agent-rules.md`](agent-rules.md) → *Agent guardrails (anchor set)*. The rest live here.

## Surface trade-offs on non-local choices

If a decision affects public API, generated SQL, or provider behavior, describe the options in the conversation rather than picking silently. For SQL AST signature changes specifically, also flag whether the type's current namespace placement is correct — see [`code-design.md`](code-design.md) → **SQL AST types live in `LinqToDB.Internal.SqlQuery`**.

## Build configurations: `== 'Release'` is not `!= 'Debug'`

The repo defines four configurations (`Testing;Debug;Release;Azure`). When proposing MSBuild edits that should fire only in production-style builds, gate with `Condition="'$(Configuration)' == 'Release'"` — the existing `RunAnalyzersDuringBuild` line at `Directory.Build.props:110` is the canonical pattern. The looser `!= 'Debug'` form leaves the property enabled for `Testing` and `Azure`, which is rarely the intent (Testing in particular is the fast-iteration single-TFM CI build that should match Debug behavior).

## Document arbitrary values explicitly

If a change requires picking an arbitrary constant (timeout, threshold, version cutoff) or making an assumption, leave a short comment or `// TODO` at the call site so a reviewer can verify it. Deliberate exception to Claude Code's default "no comments" policy — the value is inherently questionable and the comment is the signal for review.

## Prepend the UTF-8 BOM after `Write` on a `.cs` / `.csx` / `.vb` / `.vbx` file

The repo `.editorconfig` mandates `charset = utf-8-bom` for those extensions, but Claude Code's `Write` tool creates files without a BOM. After creating a new code file, prepend the BOM via `[System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($true))` from pwsh, or read-prepend-`EF BB BF`-write. `Edit` preserves whatever BOM state the file already has, so this only applies to *new* files. Cross-check with `Read`-then-byte-inspection on the first three bytes if in doubt.

## Provider behavior claims must be verified against translator code

Any agent-authored content (reviews, release notes, PR comments, XML docs, inline comments) that claims how a provider translates a member or operation must be verified against the translator at PR HEAD (`Source/LinqToDB/Internal/DataProvider/<Provider>/Translation/<Provider>MemberTranslator.cs`) before writing. The base virtuals can mislead — `TranslateNow` defaults to `CURRENT_TIMESTAMP` but most providers override it to `null`, so claims like "DateTime.Now is server-side" depend on which providers inherit vs override. Audit each per-provider claim against the actual override; don't rely on baseline diffs or older `[SqlFunction]` attributes. ([`code-reviewer.md`](../agents/code-reviewer.md) rule 9 covers external-system claims; this rule covers first-party translator behavior.)

## Codebase-state claims must be verified before writing

Any agent-authored content that asserts a quantified fact about the repo ("all X are Y", "no Z uses W", "Q is BOM-less") must be backed by a `Glob`+`Read` pass or one-off pwsh census before writing — bad censuses produced via memory bypass review (e.g. a "PublicAPI files are BOM-less" claim that was wrong on 71/72 files).

## Default to script + doc guardrails before hooks

When proposing a guardrail against a class of agent error (recurring footguns, silent encoding traps, mangling-prone CLI shapes), build a helper script under `.claude/scripts/` that encodes the right path **and** a blanket rule in [`agent-rules.md`](agent-rules.md) that surfaces the script — before reaching for a `PreToolUse` / `PostToolUse` / `SessionEnd` hook. Hooks are opt-in via `.claude/settings.local.json`, add harness surface area, and don't help users who haven't wired them in; scripts + rules cover everyone reading the corpus. Reach for a hook only when the user explicitly asks for one, or when the failure mode is genuinely undetectable from inside Claude (silent stdout corruption that no script can prevent because it happens after the agent has already typed the wrong thing).

## Distinct lenses for parallel reviewers — avoid the echo chamber

When two or more review subagents run in parallel (`/review-pr` spawns `code-reviewer` + `baselines-reviewer`, and any future fan-out), give each a genuinely distinct lens — not the same checklist. Identical-prompt agents converge on the same blind spots and rubber-stamp the same errors: N agents agreeing tells you nothing more than one did ("echo chamber"). `code-reviewer` (correctness / SQL / API / test rubric) and `baselines-reviewer` (SQL + metrics baseline diff) already split this way; preserve the split when adding reviewers, and prefer an adversarial framing for at least one (one finds, one tries to *refute* — pairs with *Frame subagent prompts to allow failure* in [`agent-rules.md`](agent-rules.md)). A second model or agent that only re-confirms the first's findings adds confidence theater, not signal — a downstream LLM asked "is this finding real?" tends to convert the upstream probability into an assertion. Trust a cross-check only when it could have disagreed.

## Cross-model verification for high-blast-radius core changes

For a change to shared cross-cutting core — the SQL AST, `IDataProvider`, the optimizer / SQL-builder base classes, or a translator interface (see [`code-design.md`](code-design.md) → **Cross-cutting internals are shared**) — single-model review is the weakest link: the model that wrote or confidently reasoned about the change shares its own blind spots when reviewing it. When such a change rests on static reasoning rather than a red→green test or a CI run, a second-model cross-check is worth the cost before asserting it's correct. Keep it cost-aware — reserve it for genuinely product-wide blast radius, not a provider-local fix — and note it never substitutes for the empirical proof the *verify before asserting* rule demands; it's a second opinion layered on top.

## Persona framing earns its keep only when concrete

A bare role opener ("act as a senior reviewer") adds little on a current capable model and field reports suggest it can even *hurt* (more hedging, more invented detail) versus a prompt that just states the concrete task, the constraints, and the allowed-failure outcome. The repo's subagents (`code-reviewer`, `test-writer`, …) already lead with a specific contract and rubric, not a personality — keep it that way: spend the prompt budget on what to check / produce and what "I couldn't" looks like, not on who to pretend to be. (Anecdotal and model-dependent; if a persona line genuinely lifts a specific subagent's output, keep it.)
