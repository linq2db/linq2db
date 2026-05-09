# Authoring scripts under `.claude/scripts/`

When and *whether* to reach for a script lives in [agent-rules.md](agent-rules.md) → *PowerShell Core scripts for complex operations*. This doc covers how to write one once you've decided to.

## Why scripts beat raw Bash chains

- **Contract, not a recipe.** The script exposes a single stdin-JSON / stdout-JSON interface. The caller doesn't need to shuttle intermediate values through temp files, environment variables, or shell-variable plumbing — all multi-step state lives inside the script.
- **One permission rule per operation.** The permission system evaluates each unique Bash command string independently, so a sequence of N related commands fires N allowlist prompts even when each piece is individually permitted. A single script invocation matches one rule for all internal calls it makes.
- **No compound-command friction.** The Bash chain rule in `agent-rules.md` forbids `&&`, `;`, and shell control flow. Work that genuinely needs branching, loops, or error-gated sequencing fits naturally inside a script and stays off the Bash surface.
- **Structured error handling.** The script can fail early with a non-zero exit and a diagnostic line on stderr, or emit a partial-success JSON with per-item `ok` flags. The caller reads one result object instead of reconstructing status from several partial Bash outputs.
- **Cross-platform.** `pwsh` behaves identically on Windows (including Git Bash), macOS, and Linux, sidestepping the MSYS path-mangling gotchas that bite raw `gh` / `git` calls.

## Contract

- Single entrypoint, named after the operation in kebab-case (e.g. `pr-context.ps1`, `post-pr-review.ps1`).
- Emit exactly one JSON result to stdout — nothing else. Diagnostics and errors go to stderr.
- **Input shape — pick the form that minimises permission prompts.** Stdin pipes / heredocs from Bash create novel command strings that miss `Bash(pwsh -NoProfile -File <path> *)` allowlist matches and trigger prompts. Prefer one of these single-command forms:
  - **Scalar / few-field inputs:** declare a `param()` block with named CLI parameters (e.g. `-Pr 5503`, `-NoFetch`). Caller invokes `pwsh -NoProfile -File <path> -Pr 5503` — single token sequence, allowlist matches.
  - **Structured / nested inputs (arrays of files, hunks, comments):** accept a `-ManifestFile <path>` parameter and read the JSON via the shared `Read-ManifestFromFileOrStdin` helper. Caller writes the manifest to `.build/.claude/<script>-<id>.json`, then invokes `pwsh -NoProfile -File <path> -ManifestFile <json>`.
  - **Stdin JSON** (legacy): keep accepting it for shell heredoc callers and pre-existing flows. New scripts should not require it.
- Common helpers live in `_shared.ps1`, dot-sourced at the top of each script (`. "$PSScriptRoot/_shared.ps1"`). Use `Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile` for the structured-input pattern — it falls back to stdin when the parameter is empty.
- Invoke as `pwsh -NoProfile -File .claude/scripts/<name>.ps1 -<Param> <value>` from Bash. `-NoProfile` skips user profile load (faster startup, no side effects).
- Do not write scratch files unless the manifest asks for one. When a caller needs to stream a large body into the script, accept either an inline field (`"body"`) or a file path (`"bodyFile"`) and resolve it server-side.
- Fan-out parallelism inside a script uses `Start-ThreadJob` or `ForEach-Object -Parallel` — both require PowerShell 7+ and run independent subprocess invocations without adding any Bash-call cost to the caller.

## Gotchas

Discovered while building the current set of scripts — worth knowing before you add a new one:

- **`$using:` does not nest.** A `Start-ThreadJob` spawned inside `ForEach-Object -Parallel` cannot access `$using:foo` from the outer parallel scope. Flatten to one parallel level, or copy the value into a local variable first.
- **`exit` inside a parallel block only kills that runspace.** Surface fatal errors from inside `Start-ThreadJob` / `ForEach-Object -Parallel` by returning an object with an `error` field and letting the parent check it — do not call the shared `Exit-WithError` helper from inside the scriptblock.
- **`ConvertFrom-Json` parses numbers as `[int64]`, not `[int32]`.** A plain `-is [int]` check fails for every JSON-sourced integer. Use the `Test-IsInteger` helper in `_shared.ps1` (or coerce via `[long]` / `-as [int]`) when validating numeric manifest fields.
- **Empty arrays get unwrapped in single-expression `if/else` assignments.** `$x = if ($cond) { @(...) } else { @() }` stores `$null` in `$x` when the else branch is taken, and `ConvertTo-Json` then serialises the field as `null`. Use statement form: `$x = @(); if ($cond) { $x = @(...) }`.
- **Use forward slashes for dot-source paths.** `. "$PSScriptRoot/_shared.ps1"` works on every platform; backslashes are literal characters in filenames on Linux/macOS and cause the lookup to fail there.
