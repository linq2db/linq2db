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
