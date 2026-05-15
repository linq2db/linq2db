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
