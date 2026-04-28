---
name: api-baselines
description: Refresh API compatibility baselines (CompatibilitySuppressions.xml) under Source/. Deletes existing suppression files, regenerates them via `dotnet pack -p:ApiCompatGenerateSuppressionFile=true`, then reviews the diff and flags any non-`LinqToDB.Internal.*` API changes for explicit user approval.
---

# api-baselines

User-triggered workflow to regenerate the `CompatibilitySuppressions.xml` baselines that ApiCompat uses to track intentional public-API changes. Equivalent to running `UpdateBaselines.cmd` at the repo root, plus policy checks.

## When to run

Only when the user explicitly invokes this skill or asks to refresh / update the API baselines. Do not run it as part of unrelated work — regenerating these files masks real API breakages.

## Steps

### 1. Choose the branch

Check the current branch (`git rev-parse --abbrev-ref HEAD`).

- If the current branch **is** `master`: stop and ask the user where to land the change. Refreshing baselines on `master` directly is not appropriate. Offer to create a new branch `infra/refresh-api-baselines` from `origin/master` (per **Creating a new branch** in `.claude/docs/agent-rules.md`). Wait for confirmation.
- If the current branch **is not** `master`: ask the user whether to:
  1. Refresh baselines on the **current branch**, or
  2. Create a new branch `infra/refresh-api-baselines` from `origin/master` (per the agent-rules branching workflow — `git fetch origin master`, handle dirty tree by asking, then branch).

  Wait for the user's choice before proceeding.

Do not regenerate anything until the branch decision is made.

### 2. Delete existing suppression files

Equivalent to `DEL /S Source\CompatibilitySuppressions.xml` from `UpdateBaselines.cmd`. Find every `CompatibilitySuppressions.xml` under `Source/` (use Glob: `Source/**/CompatibilitySuppressions.xml`) and delete each one.

### 3. Regenerate baselines

Run from the repo root (use `-p:` not `/p:` — on Git Bash / MSYS, `/p:...` is path-mangled into a Windows path and MSBuild rejects it with `MSB1009: Project file does not exist`):

```
dotnet pack -p:ApiCompatGenerateSuppressionFile=true
```

This re-runs ApiCompat across all packable projects and writes fresh `CompatibilitySuppressions.xml` files next to each project that has API differences against its baseline package.

If the command fails, surface the failure and stop. Do not proceed to the policy check on partial output.

### 4. Inspect the diff for policy violations

After regeneration, diff the suppression files against `HEAD`:

```
git diff -- 'Source/**/CompatibilitySuppressions.xml'
```

**Pair adds against removes first.** Before classifying anything, walk through the diff and pair each added `<Suppression>` block with a removed block that has the **same `(DiagnosticId, Target, Left, Right)` tuple**. These are re-orderings inside the file (the regenerator may sort entries differently than the previous run) and represent **no semantic change** — drop them from both the "added" and "removed" sets. Apply the namespace check only to the residual additions.

For every **residual added** `<Suppression>` block (lines beginning with `+` that contain a `<Target>` element and have no matching removal), extract the symbol DocId from the `<Target>` value. The DocId has the form `<kind>:<namespace>.<name>[(...)]` where `<kind>` is one of `T`, `M`, `P`, `F`, `E`, `N`.

Determine the **containing namespace**:

- For `T:Ns.Sub.TypeName` → namespace is `Ns.Sub`.
- For `M:Ns.Sub.TypeName.Method(...)` / `P:` / `F:` / `E:` → strip the trailing `(...)` parameter list (if any), then drop the last two segments (member name + containing type). Namespace is what remains.
- For `N:Ns.Sub` → namespace is the value itself.
- For nested types (`T:Ns.Outer+Inner` or `T:Ns.Outer.Inner` where `Inner` is nested) — only the leading namespace segments matter; if in doubt, treat the longest leading dotted prefix that appears before the first PascalCase type-looking segment as the namespace. When ambiguous, prefer flagging over silently allowing.

A change is **policy-allowed** iff the namespace equals `LinqToDB.Internal` or starts with `LinqToDB.Internal.`. Any other namespace is a **policy violation** — this matches the rule that `/review-pr` applies per [`.claude/docs/api-surface-classification.md`](../../docs/api-surface-classification.md), so the two skills stay consistent about what "public API change" means.

Also consider **removed** `<Suppression>` blocks (lines beginning with `-`): a removed suppression usually just means a previously-broken API was fixed or removed and no longer needs suppression. These are informational, not violations — do not block on them, but mention them in the summary if any non-`LinqToDB.Internal.*` ones disappeared.

### 5. Report and gate on policy violations

Compose a summary in this shape:

```
API baseline refresh — diff summary

Files changed:
  <list of CompatibilitySuppressions.xml paths with +N/-M counts>

Allowed (LinqToDB.Internal.*):
  +N suppressions added
  -M suppressions removed

Policy violations (non-LinqToDB.Internal.* changes):
  + <DiagnosticId> <Target>          (file: <relative path>)
  + <DiagnosticId> <Target>          (file: <relative path>)
  ...
```

If there are **no policy violations**, stop here and report success — the refresh is done. The user can decide separately whether to commit (per the repo's commit rules — never auto-commit).

If there **are** policy violations, end the report with this exact warning, then ask for explicit approval:

> ⚠️  Changes to public APIs outside the `LinqToDB.Internal.*` namespace are **against project policy in general** and should be allowed only for **major release** changes. Review the list above carefully.
>
> Do you want to **approve** these changes and keep the regenerated baselines, or **revert** them?

Wait for the user's response.

- If the user **approves**: leave the regenerated files in place and report done. Do not commit.
- If the user **rejects** / asks to revert: restore the prior baselines with `git checkout -- 'Source/**/CompatibilitySuppressions.xml'` (and `git clean -f` for any newly-created suppression files that didn't exist before — check `git status` first to identify them). Confirm the working tree matches `HEAD` for these paths after the revert.

### 6. Do not commit, push, or open a PR

Per `CLAUDE.md` rules, all of those actions need their own explicit user request. This skill's job ends after the diff is reviewed and either kept or reverted.
