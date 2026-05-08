# Windows Git Bash gotchas

The shell used by the Bash tool on Windows is Git Bash (MSYS / MINGW). It rewrites and fails on a handful of patterns that work fine on POSIX. This doc is the full reference; [`agent-rules.md`](agent-rules.md) → *Windows Git Bash gotchas* keeps a one-line trigger summary so the agent knows when to come here.

## `gh api` endpoints must not start with `/`

MSYS path-mangles a leading slash into `C:/Program Files/Git/...` and `gh` rejects it. Always write `gh api user`, `gh api repos/linq2db/linq2db/pulls/<n>/reviews` — never `gh api /user` or `gh api /repos/...`. GraphQL calls (`gh api graphql`) are unaffected.

## `git show <ref>:<path>` is path-mangled when the ref contains `/`

Git Bash on Windows treats `<ref>:<path>` as a Unix-style `PATH` list (`:`-separated) when both halves look path-ish, so a ref like `infra/claude` produces `'infra\claude;.claude\hooks\foo.ps1'` and dies with `fatal: ambiguous argument`. Single-token refs (a SHA or `master`) usually escape the heuristic. Workaround: read the blob in two allowlist-friendly steps — `git ls-tree <ref> <path>` returns `<mode> blob <sha> <path>`, then `git cat-file -p <sha>` prints the content. Both match `Bash(git *)` and avoid the colon entirely.

## `gh ... --body` is banned

Always use `--body-file <path>` (file) or `--body-file -` (stdin heredoc). Why: any `gh` subcommand (`gh pr comment`, `gh issue comment`, `gh pr create`, `gh issue create`, `gh pr edit`, `gh issue edit`, …) that receives a body whose first token starts with `/` has the leading `/` rewritten to `C:/Program Files/Git/...` before `gh` sees it — the comment posts successfully with a mangled body and there is no error. Symptom seen in the wild: `gh pr comment <n> --body "/azp run test-all"` landed as `C:/Program Files/Git/azp run test-all`, which doesn't trigger Azure Pipelines. The blanket ban removes the entire failure class:

```
gh pr comment <n> --repo <o>/<r> --body-file - <<'BODY'
/azp run test-all
BODY
```

For the most common case — posting an `/azp run …` CI trigger — use `.claude/scripts/azp-run.ps1` (`.claude\scripts\azp-run.ps1 -Pr <n> [-Pipeline <name>]`); the script feeds the body via stdin and is mangling-immune by construction. Invoke it directly via the PowerShell tool, not wrapped in `Bash(pwsh -NoProfile -File …)`. After posting any `--body-file -` body that starts with `/`, verify with `gh api repos/<o>/<r>/issues/comments/<id> --jq '.body'` — the mangling is invisible from `gh pr comment` stdout (it only prints the comment URL).

## `gh ... --body @-` is not the stdin flag

`gh` treats `@-` as a literal body string, not "read from stdin" — `gh issue edit N --body @- <<'BODY' … BODY` silently sets the body to `@-`. The blanket ban above already requires `--body-file -` for stdin and `--body-file <path>` for a file; the `@-` form is called out separately because it looks plausible enough to slip past the ban.

## Fetch a PR head via `refs/pull/<n>/head` into `origin/pr/<n>`

`git fetch origin <headRefName>:refs/remotes/origin/<headRefName>` is fragile — when the head ref isn't tracked by the local remote's fetch refspec (fork PRs, pruned branches, stale refs), the fetch exits 0 but creates no usable ref, and a later `git diff origin/master...origin/<headRefName>` dies with "ambiguous argument". Instead:

```
git fetch origin refs/pull/<n>/head:refs/remotes/origin/pr/<n>
```

Then diff/log against `origin/pr/<n>` — works for any PR (upstream branch, fork, closed, whatever), never collides with local branch names, and the `pr/<n>` namespace is self-documenting.

## Transient `fatal error — add_item` on parallel fork bursts

When several parallel Bash calls launch Git Bash at once, one may die with `fatal error — add_item (… errno 1)`. Retry the specific failed command individually; it almost always succeeds on the next attempt. This is a MSYS cygheap race, not a command error.

## `docker exec <container> /<container-side-path>` is path-mangled

Git Bash rewrites `/usr/local/bin/foo` to `C:/Program Files/Git/usr/local/bin/foo` before docker sees it, so any `docker exec` that references a container-side absolute path fails with `stat … no such file or directory`. Workaround: prefix the command with `MSYS_NO_PATHCONV=1` **and** invoke through `bash -c '…'` so the path lives inside a single argument that bypasses the rewrite:

```
MSYS_NO_PATHCONV=1 docker exec firebird50 bash -c '/usr/local/firebird/bin/firebird -z'
```

Commands that don't reference container-side paths (`docker exec firebird50 isql -version`) are unaffected.

## Native-command stdout is decoded via the console code page, not UTF-8

Capturing `gh` / `git` / other native-command output that may contain non-ASCII (emoji, em-dash, accented letters) into a pwsh variable mangles the bytes before any string op runs — the robot emoji `🤖` (UTF-8 `F0 9F A4 96`) comes back as the literal 4-character sequence `≡ƒñû`, subsequent `.Contains(robot)` / `.Replace(robot, …)` silently misses, and the garbled bytes get round-tripped back to GitHub. **Don't capture body-ish output into a variable for string surgery.** Options in priority order:

1. **Use an existing helper that goes through `Invoke-Gh` from `_shared.ps1`** — it configures the process pipes as UTF-8, so round-trip is clean. For PR body edits specifically, use `.claude/scripts/pr-body-edit.ps1` (manifest-driven, ASCII-anchor insertions, encoding-safe). Do **not** roll an ad-hoc `gh pr view … | pwsh` pipeline.
2. **File roundtrip.** `gh api repos/<o>/<r>/pulls/<n> --jq '.body' > path` to land raw bytes on disk, then `[System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))` to read them back as UTF-8. Write the modified body with the same UTF-8-no-BOM encoding and post via `gh pr edit --body-file`.
3. **ASCII-only anchors.** When doing any string-match / substitution on content that may have traveled through native-command stdout, use ASCII-only markers (`"Generated with [Claude Code]"`, not the emoji). Relatedly: pwsh captures multi-line native-command stdout as a **string array**, not a joined string — always `-join "\`n"` (or file roundtrip) before `.Contains` / `.Replace`.
4. **Preview before push.** Whatever the mechanism, dump the candidate body to a file and `Read` it before calling `gh pr edit`. Encoding mistakes are invisible from stdout counts alone.

## PowerShell gotchas

These are PowerShell-specific quirks that bit during `/kb-build` work and recur in any PS-heavy operation:

### Bracket-named files trigger PS wildcard handling

`Resolve-Path`, `Get-Item`, and `Test-Path` (without `-LiteralPath`) treat `[` and `]` in path arguments as wildcard metacharacters. A file like `[Internal]-Foo.md` (saw this on a wiki article during step 9) is silently skipped — `Get-Item '[Internal]-Foo.md'` returns nothing, no error. Then downstream code that assumes the result is non-null produces 0-byte writes / null derefs.

**Fix**: always use `-LiteralPath` on these cmdlets when the path could contain brackets:

```powershell
Get-Item -LiteralPath '[Internal]-Foo.md'      # works
Get-Item              '[Internal]-Foo.md'      # silently empty
Test-Path -LiteralPath '[Internal]-Foo.md'     # accurate
[System.IO.File]::WriteAllText('C:\path with [brackets]\file.md', $content, $utf8)   # always literal
```

`[System.IO.File]::*` methods take literal paths natively — no `-LiteralPath` analogue needed.

### `(if ... else ...)` as an expression argument is a parse error

PS5 made `if` usable as an expression (yields a value), but **only as the bare statement**, not when wrapped in parentheses inside a larger expression like a hashtable initializer:

```powershell
# WRONG — parse error: "The term 'if' is not recognized as a name of a cmdlet"
$probes += [pscustomobject]@{ Foo = (if ($x) { 'a' } else { 'b' }) }

# RIGHT — bare if/else evaluates as a value
$probes += [pscustomobject]@{ Foo = if ($x) { 'a' } else { 'b' } }

# RIGHT — extract first
$foo = if ($x) { 'a' } else { 'b' }
$probes += [pscustomobject]@{ Foo = $foo }

# RIGHT (PS7+) — ternary
$probes += [pscustomobject]@{ Foo = $x ? 'a' : 'b' }
```

Note the difference is the parentheses — `(if ...)` is treated as a command-call attempt that fails parsing; bare `if ... else` is recognised as a value-yielding expression.

### Single-line JSON breaks `Grep` (line-based matching)

`ConvertTo-Json -Compress` and large JSON pipelines that get serialized through `Set-Content -NoNewline` can produce multi-MB JSON on a single physical line. ripgrep / `Grep` is line-based — searching for `breaking` in a 7.2 MB single-line JSON file silently returns "Found 0 occurrences" because there's only 1 line and the regex matches on line content.

**Fix**: when emitting JSON for later `Grep` use, omit `-Compress` so each field gets its own line:

```powershell
$obj | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $dst -Encoding utf8
```

Or reformat after-the-fact: `Get-Content $compressed -Raw | ConvertFrom-Json | ConvertTo-Json -Depth 100 | Set-Content $pretty`. The size cost is ~10-15% (whitespace), worth it for grep-ability.

For pure regex matching against the in-memory string, `[regex]::Matches($content, $pattern)` works regardless of line structure — useful when the file structure is locked.

### Function-call scoping in nested closures

Functions defined in the outer scope are visible in nested scopes BUT external `$variables` referenced inside the function may not capture as expected when the function runs in a different scope (pipeline / `ForEach-Object` / nested function). When a function references a parent-scope variable that isn't passed as a parameter, behavior is silent-empty: the function runs without error but returns `@()` or `$null`.

**Fix**: pass external state explicitly via parameters:

```powershell
# RISKY — function relies on outer-scope $threshold being visible at call site
function Filter-Items { ($args[0] | Where-Object { $_.Count -gt $threshold }) }

# SAFE — explicit parameter
function Filter-Items {
    param($Items, [int]$Threshold)
    @($Items | Where-Object { $_.Count -gt $Threshold })
}
```

This bit `Build-Themes` during `/kb-build` step 8 — the function returned 0 themes for closed-issue clusters even when 187 closed issues existed, because an outer `$stopWords` reference wasn't reliably captured. Symptom: silent miss-rate, no parse error, no exception. Hard to detect without separate validation tests.
