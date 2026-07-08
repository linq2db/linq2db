# Windows gotchas — Claude Code tool specifics

Claude-Code-tool-specific Windows gotchas. The tool-neutral git / gh / docker / dotnet / PowerShell gotchas any agent or contributor hits on Windows live in [`windows-dev-gotchas.md`](windows-dev-gotchas.md); this overlay covers the two that are specific to Claude Code's harness — the `Glob` tool and the Bash-permission allowlist.

## `Glob` may return empty for documented paths on Windows

The Claude Code `Glob` tool can return "No files found" on Windows for paths that the file system actually contains — observed pattern is forward-slash patterns missing files whose canonical paths use backslashes. Symptom: an agent that grepped `Glob` for `.agents/scripts/<name>.ps1` mentioned in `agent-rules.md` got an empty result and reimplemented the script's job with raw `gh pr comment --body-file` instead of using the existing helper. The script existed on disk; Glob just didn't find it.

When CLAUDE.md / a SKILL / `agent-rules.md` mentions a specific script or doc path:

1. **`Read` the documented path directly first.** If the file exists, use it.
2. **If `Read` errors with "file not found"**, then trust the Glob result and proceed without it.
3. **Don't reimplement a documented-but-Glob-missing helper.** The reimplementation usually misses guardrails the original encodes (verification, encoding-safety, body-file discipline). Surfaced 2026-05-10: `.agents/scripts/azp-run.ps1` was reimplemented manually after Glob missed it.

Glob is fine for discovery patterns (`Source/**/*.cs`) in the primary clone; the trap is when the documentation has already named a specific path and Glob "doesn't find" it — or when globbing inside a worktree (next).

**Worktree paths are a second Glob blind spot — even for discovery patterns.** `Glob` with a `path` argument pointing at a linked `git worktree` (e.g. `path: "C:\GitHub\<clone>.<slug>"`) can return "No files found" for files that exist there — including plain discovery patterns like `Source/**/*.xml`, not just documentation-named single paths. Confirmed on #5687: `Glob("Source/**/CompatibilitySuppressions.xml", path=<worktree>)` returned empty while the same pattern in the primary clone found both files, and a direct `Read` of the worktree file succeeded. When globbing inside a worktree comes back empty, `Read` a known path (or run the glob in the primary clone and map the relative path across) before concluding the files are absent.

## Permission-friendly Bash patterns

Patterns that triggered prompts in real sessions and the equivalents that don't. The summary in [`agent-rules.md`](agent-rules.md) → *Bash command rules* names the most-hit ones; this is the full table.

| Avoid | Prefer | Why |
|---|---|---|
| `gh api ... > .build/.agents/foo.json` | `gh api ... --jq '...'` for extraction, or let raw output persist + `Read` | `>` redirect creates a novel command string, misses `Bash(gh api *)`. |
| `pwsh -NoProfile -Command "..."` for "just read one field" | `Grep` / `Read` directly | Inline pwsh is never allowlisted safely. |
| `pwsh -NoProfile -NonInteractive -File .agents/scripts/<name>.ps1` | `pwsh -NoProfile -File .agents/scripts/<name>.ps1` | Every script's allowlist is `pwsh -NoProfile -File .agents/scripts/<name>.ps1 *` (exact prefix + space-asterisk). Inserting `-NonInteractive` between `-NoProfile` and `-File` breaks the prefix match and triggers a prompt. Stdin-fed scripts can't prompt anyway. |
| `ls -la ../linq2db.baselines` to "check if clone exists" | `git -C ../linq2db.baselines fetch origin` (errors loudly if missing) | `ls` on documented sibling paths always prompts. |
| `mkdir -p .build/.agents/pr<n>` before a script that takes `writeDir` | Just call the script — it creates the dir itself | Helper scripts under `.agents/scripts/` create their `writeDir` internally. |
| `git fetch refs/pull/<n>/head:...` or `git fetch origin master` after `pr-context.ps1` | Skip — `pr-context.ps1` already bundles both fetches | `pr-context.ps1` sets `fetchHead: true` and refreshes the base ref in one fetch. |
| `git rev-parse origin/pr/<n>` to find the PR head SHA | Read `headSha` from the `pr-context.ps1` output | `headSha` is populated authoritatively from `git rev-parse` inside the script. |
| Scratch scripts at `/tmp/x.ps1` / `~/script.ps1` | Always under `.build/.agents/*.ps1` (allowlisted, gitignored) | Only `.build/.agents/` is whitelisted for scratch invocations. |
| `gh api ... -f body=@<file>` to PATCH a comment body from a markdown file | Build JSON via pwsh `@{body=Get-Content -Raw <md>} \| ConvertTo-Json -Compress \| Set-Content <json>` then `gh api --method PATCH ... --input <json>`. **For POST replies on review threads (`/pulls/<n>/comments/<id>/replies`) whose body is just `{body: "..."}`, the simpler `gh api ... -F body=@<file>` (capital `F`) form works — gh's `-F` flag interprets `@<file>` as "read file contents", unlike lowercase `-f` which treats `@<file>` as a literal string.** | `-f`'s `@<file>` form is **not** interpreted — it stores the literal string `@<file>` as the body. Same trap as `gh … --body @-` (banned in [`windows-dev-gotchas.md`](windows-dev-gotchas.md)). The `@<file>` shorthand only works on a few specific gh flags (`--body-file`, etc.); for REST PATCH bodies use `--input` with a JSON wrapper file. The capital-`F` form (`-F body=@<file>`) does interpret `@<file>` per gh CLI's documented field-coercion behavior — see `cli.github.com/manual/gh_api` (Type Coercion). |
| `echo '<json>' \| pwsh -File .agents/scripts/<name>.ps1` or `pwsh -File .agents/scripts/<name>.ps1 <<'EOF' ... EOF` to feed a script | Use the script's named-params or `-ManifestFile` form: `pwsh -File .agents/scripts/<name>.ps1 -Pr 5503` (scalar inputs) or `pwsh -File .agents/scripts/<name>.ps1 -ManifestFile <json>` (structured inputs). Write the JSON to `.build/.agents/<script>-<id>.json` first if needed | Stdin pipes / heredocs from Bash create novel command strings that miss the `Bash(pwsh -NoProfile -File <path> *)` allowlist match. Named parameters and `-ManifestFile <path>` keep the invocation a single allowlisted token sequence. Stdin-only invocations from the PowerShell tool (no bash layer) also hang because `[Console]::In.ReadToEnd()` blocks waiting for EOF that never arrives. See [`script-authoring.md`](script-authoring.md) → **Contract** → *Input shape*. |
| `powershell.exe -ExecutionPolicy Bypass -File <script>` | `powershell.exe -NoProfile -File <script>` (script under `.build/.agents/`) | The auto-mode classifier denies `-ExecutionPolicy Bypass` as "Security Weaken"; a locally-created (not downloaded) `.build/.agents/*.ps1` runs under the default policy without it. |
| Running an ad-hoc **SqlCe** ADO.NET probe via the `pwsh` PowerShell tool | `powershell.exe -NoProfile -File <script>` (Windows PowerShell / netfx host) | SqlCe's native engine (`System.Data.SqlServerCe`) fails to load under `pwsh 7` ("Native components … are not loaded"); the netfx host loads it. Access OLE DB (ACE 12/15) works fine in `pwsh`. Ad-hoc probes only — the test process loads SqlCe on any TFM. |

When data is already on disk (e.g. `diff-reader.ps1`'s `writeDir` cache at `.build/.agents/pr<n>/`), `Read` or `Grep` it directly rather than re-fetching via `git show … | tail | cat -A` — the `Read` tool preserves tabs and trailing whitespace literally for whitespace-byte inspection.

When a large file is read and the `Read` tool **truncates** it (e.g. "showing lines 1-N of M total"), an individual long line can come back misrendered -- a single multi-thousand-char `kb-areas.md` table row returned `**/*.cs` where the real on-disk bytes were `**/*Builder.cs`, and two `Edit` calls failed because the `old_string` didn't match. Before composing an `Edit` `old_string` for a line in a large or truncated file, re-fetch the exact bytes with `Grep` (the matching line) or a targeted `Read` (`offset=<line>, limit=1`) -- don't trust a line copied out of a truncated full-file read.
