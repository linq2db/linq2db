# Windows tooling gotchas (git / gh / docker / dotnet / PowerShell)

Git Bash (MSYS / MINGW) on Windows rewrites and fails on a handful of `git` / `gh` / `docker` patterns that work fine on POSIX, and PowerShell / dotnet carry their own Windows-specific traps. These apply to **any** agent or contributor working on this repo from Windows. Claude-Code-tool-specific gotchas (the `Glob` tool, the Bash-permission allowlist) live in the overlay [`windows-gotchas.md`](windows-gotchas.md).

> Some entries cite Claude Code helpers (`.agents/scripts/*.ps1`, the PowerShell / Bash tools, `.build/.agents/` scratch). Those describe the Claude path; other agents substitute their own shell / tooling — the underlying git / gh / OS behavior is what each entry is really about.

## `gh api` endpoints must not start with `/`

MSYS path-mangles a leading slash into `C:/Program Files/Git/...` and `gh` rejects it. Always write `gh api user`, `gh api repos/linq2db/linq2db/pulls/<n>/reviews` — never `gh api /user` or `gh api /repos/...`. GraphQL calls (`gh api graphql`) are unaffected.

## `gh api graphql --paginate` output + query passing

Two traps when paginating a GraphQL connection (e.g. a PR's `reviewThreads`, which exceeds the default `first:100` on heavily-reviewed PRs):

- **`--paginate` emits one JSON object per page**, concatenated. Piping the whole thing to `ConvertFrom-Json` fails with *"Additional text encountered after finished reading JSON content"*. Add `--jq '<filter>'` so `gh` applies the filter **per page** and streams newline-delimited records you parse line-by-line — or paginate the cursor yourself in a script (the `pr-context.ps1` `reviewThreads` job does the latter). The query must expose `pageInfo { hasNextPage endCursor }` and take an `$endCursor: String` variable for `--paginate` to advance — **naming that variable anything else (e.g. `$cursor`) does not error; `--paginate` silently fetches only the first page**, so items on later pages vanish with no signal (on a heavily-reviewed PR the *newest* threads are exactly what's lost).
- **Pass the query via a variable, not `@file`.** `gh api graphql -f query=@path.graphql` does **not** read the file — it sends the literal `@path.graphql`, and the server rejects it with *"Expected one of SCHEMA, SCALAR, TYPE, … actual: DIR_SIGN (\"@\")"*. Read the file into a variable first (`$q = Get-Content path.graphql -Raw`) and pass `-f query=$q`.
- **Don't backslash-escape regex metachars in a `--jq` filter passed through the Bash tool.** A jq `test("…\.cs$")` (or `\\.`) written through the Bash tool on Windows loses the backslash before jq sees it, so jq aborts with *"jq: invalid escape sequence \".\" in string literal"*. Use an unescaped `.` (which matches a literal dot too) or a character class `[.]` instead of `\.`. Applies to any `--jq` regex, not just graphql.

## `git show <ref>:<path>` is path-mangled when the ref contains `/`

Git Bash on Windows treats `<ref>:<path>` as a Unix-style `PATH` list (`:`-separated) when both halves look path-ish, so a ref like `infra/agents-curation` produces `'infra\agents-curation;.agents\hooks\foo.ps1'` and dies with `fatal: ambiguous argument`. Single-token refs (a SHA or `master`) usually escape the heuristic. Workaround: read the blob in two allowlist-friendly steps — `git ls-tree <ref> <path>` returns `<mode> blob <sha> <path>`, then `git cat-file -p <sha>` prints the content. Both match `Bash(git *)` and avoid the colon entirely.

## Cloning the `linq2db.wiki` repo fails on a colon-named page

The wiki repo contains a page named `[Internal]-Azure-Pipelines:-Open-Tasks.md`. The `:` is illegal in NTFS filenames, so a plain `git clone` **fails at checkout** ("invalid path … : …") and leaves an empty / inconsistent working tree — do **not** `git add` / commit from that state (every other page shows as a staged deletion, and committing would delete them). Clone with no checkout, restrict to the page(s) you need via sparse-checkout, then check out with NTFS protection disabled (the bad file is `skip-worktree`, so it's never written to disk):

```
git clone --no-checkout https://github.com/linq2db/linq2db.wiki.git ../linq2db.wiki
git -C ../linq2db.wiki sparse-checkout set --no-cone Releases-and-Roadmap.md
git -C ../linq2db.wiki -c core.protectNTFS=false checkout master
```

After this, `git status` is clean and only the sparse page(s) are materialized; `apply-wiki` + commit + push work normally **for a page already in the sparse set** (they never touch the colon-named blob, which stays in the tree untouched). Release-notes usage of the clone: [`release/external-repos.md`](release/external-repos.md).

**Editing or adding a page that is _not_ already in the sparse set needs one extra step first.** The "works normally" above holds only for a page materialized in the cone (e.g. `Releases-and-Roadmap.md`, which `/release-notes` edits). For any other page — a brand-new rule page, or an existing page you haven't sparse-checked-out — `git add --sparse <page>.md` **clears the `skip-worktree` bit on out-of-cone entries**, so the un-checkout-able colon page immediately shows as a staged deletion and the next `git commit` deletes it. Pushing that commit would delete a live wiki page. Two safe paths:

- **Preventive — bring the page into the cone before touching it:**

  ```
  git -C ../linq2db.wiki sparse-checkout add <page>.md
  ```

  then edit / `git add <page>.md` / commit / push normally; the colon blob stays `skip-worktree` and is never staged.

- **Recovery — bad commit already made but not yet pushed.** Rebuild the commit via plumbing so the working tree's missing colon page is never consulted (run as separate calls — the Bash tool doesn't persist shell vars, so capture each printed SHA and substitute it into the next call):

  ```
  git -C ../linq2db.wiki -c core.protectNTFS=false read-tree origin/master   # index := remote; working tree untouched
  git -C ../linq2db.wiki hash-object -w <page>.md                            # -> <blob>
  git -C ../linq2db.wiki update-index --cacheinfo 100644,<blob>,<page>.md
  git -C ../linq2db.wiki -c core.protectNTFS=false write-tree                # -> <tree>
  git -C ../linq2db.wiki commit-tree <tree> -p origin/master -F <msg-file>   # -> <commit>
  git -C ../linq2db.wiki update-ref refs/heads/master <commit>
  ```

  Verify `git -C ../linq2db.wiki -c core.protectNTFS=false diff --stat origin/master master` shows **only** your page (no colon-page deletion) before `git -C ../linq2db.wiki push origin master`.

**Before creating a wiki page for a task** (e.g. an analyzer descriptor's `helpLinkUri` target), fetch and check whether it already exists — `git -C ../linq2db.wiki fetch origin` then `git -C ../linq2db.wiki ls-tree origin/master <page>.md`. Pages are often authored out-of-band; drafting one that already exists wastes effort and risks overwriting a maintainer's page. **A PR body's "post-merge follow-up: create wiki page X" bullet is *not* evidence the page is absent** — such follow-up lists are aspirational and frequently lag the already-authored page. Run the `ls-tree` check before telling the user a page doesn't exist; don't infer its status from PR-body prose (an instance of *fetched content is a claim, verify it*). Corrected on PR #5703, where the `LINQ2DB1001` page existed despite the PR body listing it as a to-do.

**Renaming a wiki page** (e.g. a diagnostic id changed on the PR): after the sparse-checkout above, `git mv --sparse <old>.md <new>.md` — plain `git mv` fails with a "Use the --sparse option" hint because the destination isn't in the sparse set. Then edit + `git -C ../linq2db.wiki add --sparse <new>.md` (the `--sparse` is likewise required), commit, push. Also `git -C ../linq2db.wiki grep -n "<oldId>" HEAD -- "*.md"` first to catch any *other* page linking the old name (the rename would break those links). Done on PR #5703 (`LINQ2DB1001.md` → `L2DB1001.md`, plus its `helpLinkUri`/editorconfig id references).

## `gh ... --body` is banned

Always use `--body-file <path>` (file) or `--body-file -` (stdin heredoc). Why: any `gh` subcommand (`gh pr comment`, `gh issue comment`, `gh pr create`, `gh issue create`, `gh pr edit`, `gh issue edit`, …) that receives a body whose first token starts with `/` has the leading `/` rewritten to `C:/Program Files/Git/...` before `gh` sees it — the comment posts successfully with a mangled body and there is no error. Symptom seen in the wild: `gh pr comment <n> --body "/azp run test-all"` landed as `C:/Program Files/Git/azp run test-all`, which doesn't trigger Azure Pipelines. The blanket ban removes the entire failure class:

```
gh pr comment <n> --repo <o>/<r> --body-file - <<'BODY'
/azp run test-all
BODY
```

For the most common case — posting an `/azp run …` CI trigger — use `.agents/scripts/azp-run.ps1` (`.agents\scripts\azp-run.ps1 -Pr <n> [-Pipeline <name>]`); the script feeds the body via stdin and is mangling-immune by construction. Invoke it directly via the PowerShell tool, not wrapped in `Bash(pwsh -NoProfile -File …)`. After posting any `--body-file -` body that starts with `/`, verify with `gh api repos/<o>/<r>/issues/comments/<id> --jq '.body'` — the mangling is invisible from `gh pr comment` stdout (it only prints the comment URL).

## `gh ... --body @-` is not the stdin flag

`gh` treats `@-` as a literal body string, not "read from stdin" — `gh issue edit N --body @- <<'BODY' … BODY` silently sets the body to `@-`. The blanket ban above already requires `--body-file -` for stdin and `--body-file <path>` for a file; the `@-` form is called out separately because it looks plausible enough to slip past the ban.

## Multi-line `git commit` messages: write to a file + `-F`

The Bash tool is Git Bash (POSIX sh), **not** PowerShell — a PowerShell here-string `git commit -m @'…'@` is not a heredoc there, so the `@'` / `'@` markers leak into the message (mangled subject line `@`, trailing `@` footer), forcing an amend. Bash double-quoted `-m "…"` also command-substitutes backticks (`` `word` `` vanishes). Robust on Windows regardless of shell or content: `Write` the message to `.build/.agents/commit-msg.txt`, then `git commit -F <path>` (and `git commit --amend -F <path>` to fix a botched one). Same trap/fix the `gh … --body-file` rules above address for issue/PR bodies.

## Fetch a PR head via `refs/pull/<n>/head` into `origin/pr/<n>`

`git fetch origin <headRefName>:refs/remotes/origin/<headRefName>` is fragile — when the head ref isn't tracked by the local remote's fetch refspec (fork PRs, pruned branches, stale refs), the fetch exits 0 but creates no usable ref, and a later `git diff origin/master...origin/<headRefName>` dies with "ambiguous argument". Instead:

```
git fetch origin refs/pull/<n>/head:refs/remotes/origin/pr/<n>
```

Then diff/log against `origin/pr/<n>` — works for any PR (upstream branch, fork, closed, whatever), never collides with local branch names, and the `pr/<n>` namespace is self-documenting.

## Finding whether an open PR adds a token (`gh search code` indexes only `master`)

`gh search code "<token>" --repo <o>/<r>` searches the **default branch only**, so a token that exists only on an unmerged PR branch (a new convention, a renamed symbol) returns zero matches even though the PR adds it. Two consequences:

- To find *which* open PR introduces a token, list open PRs (`gh pr list --state open --json number,title,author,headRefName`) and scan titles/bodies, then confirm against the actual patch.
- To check whether a specific PR's diff contains a token, **don't** `gh pr diff <n> --patch | grep <token>` — the pipe misses allowlist matching, and `gh pr diff` truncates large diffs (it returned a false `0` count on a 112k-line PR this way). Use the files API, which reads the real per-file patches in one allowlisted call:

```
gh api repos/<o>/<r>/pulls/<n>/files --paginate --jq 'select(.patch != null) | select(.patch | contains("<token>")) | .filename'
```

Files with very large patches have `.patch` omitted by the API (hence the `!= null` guard); for those, read the file at the PR head via `git ls-tree` + `git cat-file` instead. Same rule for bounding output generally: prefer a command's own `--limit` / `--jq` over piping to `| head`, which is a novel command string that misses the allowlist.

## Transient `fatal error — add_item` on parallel fork bursts

When several parallel Bash calls launch Git Bash at once, one may die with `fatal error — add_item (… errno 1)`. Retry the specific failed command individually; it almost always succeeds on the next attempt. This is a MSYS cygheap race, not a command error.

It also fires on **sequential, single** Bash calls — not only parallel bursts — and a retry does **not** always clear it (it can fail several times in a row). When it recurs, stop retrying and **route the call through the PowerShell tool** instead (or, for `gh`, the `Invoke-Gh` helpers): that bypasses the Git-Bash/MSYS layer entirely and the command runs clean. (Surfaced on PR #5605: `gh pr checks` failed twice via Bash, then succeeded immediately via the PowerShell tool, as did `post-pr-review.ps1`.)

## `docker exec <container> /<container-side-path>` is path-mangled

Git Bash rewrites `/usr/local/bin/foo` to `C:/Program Files/Git/usr/local/bin/foo` before docker sees it, so any `docker exec` that references a container-side absolute path fails with `stat … no such file or directory`. Workaround: prefix the command with `MSYS_NO_PATHCONV=1` **and** invoke through `bash -c '…'` so the path lives inside a single argument that bypasses the rewrite:

```
MSYS_NO_PATHCONV=1 docker exec firebird50 bash -c '/usr/local/firebird/bin/firebird -z'
```

Commands that don't reference container-side paths (`docker exec firebird50 isql -version`) are unaffected.

## Native-command stdout is decoded via the console code page, not UTF-8

Capturing `gh` / `git` / other native-command output that may contain non-ASCII (emoji, em-dash, accented letters) into a pwsh variable mangles the bytes before any string op runs — the robot emoji `🤖` (UTF-8 `F0 9F A4 96`) comes back as the literal 4-character sequence `≡ƒñû`, subsequent `.Contains(robot)` / `.Replace(robot, …)` silently misses, and the garbled bytes get round-tripped back to GitHub. **Don't capture body-ish output into a variable for string surgery.** Options in priority order:

1. **Use an existing helper that goes through `Invoke-Gh` from `_shared.ps1`** — it configures the process pipes as UTF-8, so round-trip is clean. For PR body edits specifically, use `.agents/scripts/pr-body-edit.ps1` (manifest-driven, ASCII-anchor insertions, encoding-safe). Do **not** roll an ad-hoc `gh pr view … | pwsh` pipeline. A further trap when hand-rolling: `gh … > .build/.agents/x.json` lands the file in the **Bash tool's cwd**, which may be a **worktree** (e.g. `…/<clone-dir>.<slug>`), not the primary clone — so a follow-up reader (a python/pwsh script written via the Write tool to the primary-clone absolute path) fails with `FileNotFound`. `pwsh -File` scripts read stdin manifests and stage under a `workDir` they control, sidestepping this; if you must hand-roll, run `pwd` first and use one consistent absolute path for the round-trip.
2. **File roundtrip.** `gh api repos/<o>/<r>/pulls/<n> --jq '.body' > path` to land raw bytes on disk, then `[System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))` to read them back as UTF-8. Write the modified body with the same UTF-8-no-BOM encoding and post via `gh pr edit --body-file`. Note: `ReadAllText(path, encoding)` always auto-detects and strips a UTF-8 BOM if one is present — the `encoding` argument is the *fallback* used when no BOM exists, not a "decode without BOM detection" flag. Passing `UTF8Encoding($false)` doesn't skip BOM-stripping; it just controls what encoding to assume for the BOM-less case.
3. **ASCII-only anchors.** When doing any string-match / substitution on content that may have traveled through native-command stdout, anchor on an ASCII-only marker (a plain heading like `"## Summary"`, or a `Co-authored-by:` trailer line) — never on an emoji-bearing line, whose bytes get mangled. Relatedly: pwsh captures multi-line native-command stdout as a **string array**, not a joined string — always `-join "\`n"` (or file roundtrip) before `.Contains` / `.Replace`.
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

### `-UseBasicParsing` is deprecated, not removed

`Invoke-WebRequest` and `Invoke-RestMethod` still accept `-UseBasicParsing` in PowerShell Core 7+. It became a no-op (PS Core uses the basic parser by default), but the parameter itself remains for back-compat with PS 5.1 scripts. Verified on pwsh 7.6.1: `(Get-Command Invoke-WebRequest).Parameters.ContainsKey('UseBasicParsing')` returns `True`; a live HTTPS call with the flag returns status 200. When a review flags a `-UseBasicParsing` call as "will throw at runtime", treat as **Inaccurate** unless you can reproduce a `ParameterBindingException` against pwsh 7+. Surfaced 2026-05-14 on PR #5521 (Copilot review comments 3244572156 / 3244572193 / 3244572209).

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

### `Get-ChildItem -Filter` only supports `*` and `?`

The `-Filter` parameter is passed to the FileSystem provider's native search pattern, which accepts ONLY `*` and `?` wildcards. Character classes like `[0-9]`, `[abc]`, ranges, and POSIX-style brackets are treated **literally** — so `-Filter 'pkg.[0-9]*.nupkg'` looks for a file whose name literally starts with `pkg.[0-9]` and silently returns zero matches.

**Fix**: keep `-Filter` broad (only the `*`/`?` patterns it understands) and do the precise filtering in `Where-Object` against `FileInfo.Name`:

```powershell
# WRONG — -Filter treats [0-9] literally; matches nothing
Get-ChildItem -Path $src -Filter 'pkg.[0-9]*.nupkg'

# RIGHT — broad -Filter + regex Where-Object
Get-ChildItem -Path $src -Filter 'pkg.*.nupkg' |
    Where-Object { $_.Name -match '^pkg\.\d.*\.nupkg$' }
```

Bit `NuGet/TestToolLocal.ps1` on PR #5539 round-4: the bracket-class filter would have made every `dotnet tool install` from the local source fail with "Cannot find linq2db.cli pointer nuget". Caught by Copilot before merge, but the failure was silent — no exception, just an empty match. Use the broad-filter + regex pattern when you need character-class precision.

### `%USERPROFILE%` is cmd-only — PowerShell passes it literally

cmd.exe expands `%USERPROFILE%` to the actual profile path; PowerShell does NOT — `--tool-path %USERPROFILE%\tools\x86` invoked from a pwsh prompt passes the literal text `%USERPROFILE%\tools\x86`, and the tool / file system tries to use a directory named `%USERPROFILE%`.

**Fix**: don't put `%USERPROFILE%` in docs/help text that pwsh users will read. Use one of:

- **`$env:USERPROFILE`** (PowerShell-only — interpolates inside double quotes: `"$env:USERPROFILE\tools\x86"`)
- **Shell-neutral absolute path** (`C:\tools\linq2db-x86`) — works in both, cheapest for cross-shell docs
- **Explicit shell label** when both are shown (`PowerShell: "$env:USERPROFILE\..."` / `cmd: "%USERPROFILE%\..."`)

Quote any path that may contain spaces. Bit PR #5539 rounds 2 + 4 (Copilot caught it twice — the side-by-side `--tool-path` install recipe initially used `%USERPROFILE%` without naming the shell).

## Iterative-build gotchas

Failure modes that surface when running `dotnet build` (or `/test`, or `/release-verify`) in a session:

**`error: Access to the path '<dll>' is denied`** (typically `Microsoft.Build.Tasks.Git.dll` or another transient build dep). Root cause: VBCSCompiler / MSBuild server is holding the file from the previous build. Resolution: `dotnet build-server shutdown` (kills both servers cleanly), then re-run. The `analyzer-profile-build.ps1` script does this automatically; ad-hoc `dotnet build` invocations don't and must be done manually when the lock surfaces. Don't retry the build without the shutdown — it'll keep failing on the same file.

**The lock holder is a testhost, not the build server.** When the `Access denied` / `MSB3021` copy failure names `The file is locked by: "linq2db.Tests (<pid>)"`, the holder is an orphaned MTP testhost left over from a cancelled `dotnet test` run — `dotnet build-server shutdown` does **not** release it (it only kills VBCSCompiler / MSBuild server). Confirm with `Get-Process -Id <pid>`, then `Stop-Process -Id <pid> -Force`, then re-run. The error message names the holding process and PID — that's the discriminator from the build-server case above.

**A run that *appears* hung — check the heartbeat before concluding "deadlock".** A long `dotnet test` / `/test` run that seems stuck: look for its `test-progress.<tfm>.<pid>.json` under `.build/.agents/`. **No heartbeat written at all = the run never reached test execution — it stalled in the build/discovery phase** (almost always a build-server file lock from a prior cancelled run, per the cases above), *not* a test deadlock. The idle process is the `dotnet test` driver waiting on a build that can't acquire the lock; CPU stays near-zero (a real infinite loop pegs a core). Confirm with `dotnet build-server shutdown` then a standalone build of the test project — it rebuilds in seconds once the lock clears. Separately: a `test-runner` invocation that drops `-f <tfm>` and/or the `--filter` silently runs the **full suite across all four TFMs** (~8× the intended scope) — minutes of runtime that also read as a hang. Pass `-f net10.0` and the filter explicitly for `Tests/Linq` (see [`../agents/test-runner.md`](../agents/test-runner.md)).

**`error MSB3021/MSB3027: There is not enough space on the disk`** during the post-compile copy step. Root cause: `.build/bin/` accumulates ~9 GB per Release build of `linq2db.slnx` and is not pruned between iterations. When iterating against a near-full C: drive (especially with a worktree, which adds a second `.build/bin/`), clean before re-running:

```
Remove-Item -Recurse -Force <repo-root>/.build/bin <repo-root>/.build/obj
```

Don't try to outwait a transient disk-space failure or ignore it as "the build mostly succeeded" — compilation may have passed but the dll-copy step's failure leaves the test project unrunnable until the disk has headroom.

**`MSBUILD : error MSB4166: Child node "<n>" exited prematurely`** part-way through a long `dotnet test` run — distinct from the clean `MSB3021/MSB3027` above. `dotnet test` builds before running, and that build's MSBuild child node crashes (disk / memory pressure on a near-full box), aborting the whole run mid-suite: a full YDB run truncated at ~3.3 k of ~7.9 k tests this way, twice. Fix: build the test project once on its own, then run with `dotnet test … --no-build` so the test run spawns no MSBuild child nodes (it also won't re-lock the just-built DLL, and is faster). A full suite that kept truncating then completed cleanly (7904 tests) under `--no-build`.

**Repeated `dotnet test` cycles against a worktree progressively slow the build until it times out.** Running several `dotnet test` iterations back-to-back on a worktree (e.g. a red→green→regression verification loop) accumulates orphaned `dotnet` / `testhost` child processes — each cancelled or completed MTP run can leave one behind — and the pileup starves later builds until one exceeds the invocation timeout and is SIGTERM'd, surfacing the `MSB4166` *child node exited prematurely* errors above as a side-effect of the cancellation (not a genuine pressure crash). Discriminator from the real MSB4166 case: the earlier runs in the same session built in normal time (~3 min) and only the later ones slow down. Fix: run `dotnet build-server shutdown` between iterations (and, when many have accumulated, `Get-Process dotnet,testhost,csc -ErrorAction SilentlyContinue | Stop-Process -Force`) — a build that timed out at 10 min completed in ~3 min on the very next run after the shutdown. (Surfaced reviewing PR #5555: the 7th consecutive worktree test run timed out; the retry after a build-server shutdown ran normally.)

**A backgrounded build piped to `tail`/`head` reports the pipe's exit code, not the build's.** `dotnet build … | tail -N` run via `run_in_background` surfaces a completion exit code of `0` (tail succeeded) even when the build FAILED — a red build reads as green. Don't trust the notification's exit code on a piped build; `Read` the output file and check for `Build FAILED` / `N Error(s)`. Better: don't pipe at all — let the full output persist and `Read` it (mirrors the "read the whole log, don't pipe to head/tail" rule for test runs in [`../agents/test-runner.md`](../agents/test-runner.md)). Observed verifying a PR's Release build: a failed build (6 `IDE0306` errors) reported `exit code 0` because the pipe's tail succeeded.

**`error MSB4025: The project file could not be loaded. An XML comment cannot contain '--'`** when a `.csproj` / `.fsproj` / `.props` / `.slnx` comment quotes a CLI double-dash option (`--nrt`, `--target-language`). A literal `--` is illegal *inside* an XML `<!-- … -->` comment, so the project file fails to load before any build starts. Reword the comment to drop the dashes (`nrt=true`, `target-language f#`). (Surfaced on #1553 three times writing baseline-gate `.fsproj` comments that referenced the scaffold CLI flags — each `--nrt` / `--target-language` in a comment aborted the load.)

## TFM API availability (net462 / netstandard2.0)

The `Testing` configuration builds **net10.0 only**, so a local `dotnet build -c Testing` (and any single-`net10.0` fast iteration) will **not** catch APIs missing on the older TFMs in `<TargetFrameworks>` (`net462`, `netstandard2.0`). Before pushing source that calls a BCL API / extension method newer than .NET Standard 2.0 (e.g. `Enumerable.ToHashSet`, span overloads, `string.Contains(char)`), build one portable TFM locally first:

```
dotnet build Source/LinqToDB/LinqToDB.csproj -c Release -f netstandard2.0
```

CI's `build` check builds every TFM and otherwise fails with `CS1061 … are you missing a using directive` on the `net462`/`netstandard2.0` leg (`Build Examples (verify)`), costing a full red CI cycle.

When the API is genuinely missing on an older TFM, **prefer enabling the matching `Meziantou.Polyfill` entry** in the `<Polyfill>` opt-in list in `Directory.Build.props` over reworking the call — keep the idiomatic BCL form. The polyfill is TFM-conditional (newer TFMs use the real method), and the supported-polyfill ID list is linked in the props header. Reworking the call (`new HashSet<T>(seq, cmp)` instead of `seq.ToHashSet(cmp)`, etc.) is the fallback when no polyfill exists.

## Analyzers are Release-only — build Release before push when a change can trip one

Roslyn analyzers, banned-API checks, and Meziantou (`MA*`) rules run **only in Release** (`Testing`/`Debug` fast-iteration skips them). So a `dotnet build -c Testing` / `/test` run can be fully green while CI's Release `build` leg fails. Before pushing a change that can plausibly trip an analyzer — new/changed public API, `Equals`/`GetHashCode`, nullable annotations (`[NotNullWhen]` etc.), banned-API-adjacent calls — do one local Release build first:

```
dotnet build Source/LinqToDB/LinqToDB.csproj -c Release -f net10.0
```

(net10.0 alone is enough to surface the diagnostic — analyzers are TFM-independent.) Otherwise CI fails on e.g. `MA0186` (Equals parameter missing `[NotNullWhen(true)]`) after a green local run, costing a red CI cycle.

**PublicAPI RS0016/RS0017 are a *separate* opt-in the Release build above does NOT run.** The `Microsoft.CodeAnalysis.PublicApiAnalyzers` diagnostics — RS0016 (public symbol missing from `PublicAPI.Unshipped.txt`) and RS0017 (entry in the declared API but not in code) — are gated behind `RunApiAnalyzersDuringBuild`, which defaults to **`false`** in `Source/Directory.Build.props`. A plain `dotnet build -c Release` reports **0 errors even when new public members are unregistered**. To surface them locally after adding/removing/renaming public surface, add the flag:

```
dotnet build Source/LinqToDB/LinqToDB.csproj -c Release -f net10.0 -p:RunApiAnalyzersDuringBuild=true
```

Each RS0016 message quotes the exact line to paste into `PublicAPI.Unshipped.txt` (verbatim, incl. the `static ` / `params ` prefixes). Normal PR CI (`build.yml`) runs with the flag **off**; only the comprehensive `default.yml` / `/azp run test-all` leg turns it on — so missing entries slip through fast iteration and a plain Release build, then fail the comprehensive leg. (Distinct from `CompatibilitySuppressions.xml` / ApiCompat, handled by `/api-baselines`.)

## Bisecting across SDK upgrades

When checking out historic commits to bisect a regression or to confirm "after which PR did the test start passing", the historic code may compile cleanly on the SDK it was written against but trip newer compiler warnings on the current SDK. Combined with `TreatWarningsAsErrors=true` (the default in `Directory.Build.props`), these warnings become build-blocking errors and the test never runs.

Observed in this repo with `CS9336: The pattern is redundant` when bisecting commits older than .NET SDK 10 — the warning was introduced in a later compiler version and didn't exist when the code was written.

Pass both flags to escape:

```
dotnet test --project ... -p:TreatWarningsAsErrors=false -p:NoWarn=CS9336
```

`TreatWarningsAsErrors=false` is the broad escape; `NoWarn=<id>` silences the specific code so the rest of the warning-as-error policy still surfaces real new issues during the bisect. Add additional IDs as needed when later iterations of the bisect hit different historic-code warnings.

Don't disable the policy in `Directory.Build.props` (it would commit to the bisect worktree and pollute future builds) — pass the flags on the `dotnet test`/`build` command line only.
