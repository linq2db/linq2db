<#
Shared helpers for review scripts (pr-context, diff-reader, verify-lines,
baselines-diff, post-pr-review). Keeps each script small and makes their
Bash-call surface uniform:

    pwsh -NoProfile -File .claude/scripts/<name>.ps1 <<'EOF' … EOF

One permission rule per script, JSON in on stdin, JSON out on stdout.
Nothing here performs writes to the filesystem or the GitHub API beyond
what the caller explicitly asks for.

See `.claude/docs/agent-rules.md` → **PowerShell Core scripts for complex
operations** for the invocation pattern, contract, and the pwsh-specific
gotchas (`$using:` nesting, empty-array unwrap, integer coercion).
#>

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

function Exit-WithError {
    param([string]$Message, [int]$Code = 1)
    $name = if ($global:ScriptBaseName) { $global:ScriptBaseName } else { 'script' }
    [Console]::Error.WriteLine("${name}: $Message")
    exit $Code
}

# NOTE: `exit` inside `Start-ThreadJob` / `ForEach-Object -Parallel` terminates
# only that runspace, not the parent script. Do not call Exit-WithError from
# inside a parallel block — instead return an object with an error field and
# let the parent check it after joining.

# Run a child process with optional stdin, returning a result object. All
# external `gh` / `git` calls go through this so stdio handling stays uniform.
# Stdout/stderr are read asynchronously to prevent pipe-buffer deadlocks.
function Invoke-Process {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [string]$StdinInput,
        [string]$WorkingDirectory
    )
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $FilePath
    foreach ($a in $ArgumentList) { [void]$psi.ArgumentList.Add([string]$a) }
    $psi.RedirectStandardInput  = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true
    $psi.UseShellExecute = $false
    $psi.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $psi.StandardErrorEncoding  = [System.Text.UTF8Encoding]::new($false)
    # Without this, StandardInput.Write(string) encodes via the system code page
    # (cp1252 on Windows), which mangles any non-ASCII character in the payload
    # — em-dash, non-breaking space, anything Unicode. gh then forwards the
    # corrupted bytes to GitHub, which rejects with 400 "Problems parsing JSON".
    $psi.StandardInputEncoding  = [System.Text.UTF8Encoding]::new($false)
    if ($WorkingDirectory) { $psi.WorkingDirectory = $WorkingDirectory }

    try {
        $p = [System.Diagnostics.Process]::Start($psi)
    } catch {
        return [pscustomobject]@{ ok = $false; stdout = ''; stderr = ''; code = -1; error = "spawn ${FilePath}: $($_.Exception.Message)" }
    }

    $stdinErr = $null
    try {
        $outTask = $p.StandardOutput.ReadToEndAsync()
        $errTask = $p.StandardError.ReadToEndAsync()
        if ($null -ne $StdinInput) {
            try { $p.StandardInput.Write($StdinInput) }
            catch { $stdinErr = "stdin write failed: $($_.Exception.Message)" }
        }
        try { $p.StandardInput.Close() } catch { }

        $p.WaitForExit()
        $stdout = $outTask.GetAwaiter().GetResult()
        $stderr = $errTask.GetAwaiter().GetResult()
        $code = $p.ExitCode
    } finally {
        $p.Dispose()
    }

    $ok = ($code -eq 0)
    $err = if (-not $ok) {
        $msg = (($stderr + $stdout)).Trim()
        if ($stdinErr) { $msg = if ($msg) { "${stdinErr}`n$msg" } else { $stdinErr } }
        if (-not $msg) { "$FilePath exited $code" } else { $msg }
    } else { $null }

    return [pscustomobject]@{ ok = $ok; stdout = $stdout; stderr = $stderr; code = $code; error = $err }
}

function Invoke-Gh {
    param([string[]]$ArgumentList, [string]$StdinInput)
    return Invoke-Process -FilePath 'gh' -ArgumentList $ArgumentList -StdinInput $StdinInput
}

function Invoke-GhJson {
    param([string[]]$ArgumentList, [string]$StdinInput)
    $r = Invoke-Gh -ArgumentList $ArgumentList -StdinInput $StdinInput
    if (-not $r.ok) { return $r }
    try {
        $data = $r.stdout | ConvertFrom-Json -Depth 100
        return [pscustomobject]@{ ok = $true; data = $data }
    } catch {
        $snippet = if ($r.stdout.Length -gt 500) { $r.stdout.Substring(0, 500) } else { $r.stdout }
        return [pscustomobject]@{ ok = $false; error = "gh output is not JSON: $($_.Exception.Message)`nraw: $snippet" }
    }
}

function Invoke-Git {
    param([string[]]$ArgumentList, [string]$WorkingDirectory)
    return Invoke-Process -FilePath 'git' -ArgumentList $ArgumentList -WorkingDirectory $WorkingDirectory
}

# `ConvertFrom-Json` parses numeric values as [int64]/[long], not [int32]. A
# plain `-is [int]` check therefore fails for any JSON-sourced number. Use
# these helpers wherever validating or coercing integer manifest fields.
function Test-IsInteger {
    param($Value)
    return ($Value -is [int]) -or ($Value -is [long]) -or ($Value -is [byte]) -or ($Value -is [short])
}

function Read-StdinText {
    # Stdin on Windows defaults to the OEM codepage (CP437/850), which mangles
    # UTF-8 bytes — em-dashes show up as `ΓÇö` on GitHub. Force UTF-8 so
    # non-ASCII characters in heredoc manifests round-trip correctly.
    [Console]::InputEncoding = [System.Text.Encoding]::UTF8
    return [Console]::In.ReadToEnd()
}

function Read-StdinJson {
    $raw = Read-StdinText
    if (-not $raw -or -not $raw.Trim()) { Exit-WithError 'no manifest JSON on stdin' }
    try {
        return $raw | ConvertFrom-Json -Depth 100
    } catch {
        Exit-WithError "invalid JSON on stdin: $($_.Exception.Message)"
    }
}

# Write-JsonOutput: emit JSON on stdout with a trailing newline. Depth is
# generous so deeply nested structures (e.g. linked issues with comments)
# serialize in full.
function Write-JsonOutput {
    param([Parameter(Mandatory)]$InputObject, [int]$Depth = 100)
    $json = $InputObject | ConvertTo-Json -Depth $Depth
    [Console]::Out.WriteLine($json)
}

# Parse the output of `git diff --unified=0 A...B -- <paths>` into per-file
# right-side hunk ranges. Returns a hashtable: path -> @(@{startLine; endLine}).
# A hunk with newcount == 0 is a pure-deletion (no added lines on the right
# side) and is dropped so only ranges containing post-change lines are tracked.
function ConvertFrom-UnifiedDiffHunks {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$DiffText)
    $hunks = @{}
    if (-not $DiffText) { return $hunks }
    $current = $null
    foreach ($line in $DiffText -split "`n") {
        if ($line.StartsWith('diff --git ')) {
            if ($line -match ' b/(.+)$') { $current = $Matches[1] } else { $current = $null }
            if ($current -and -not $hunks.ContainsKey($current)) { $hunks[$current] = @() }
            continue
        }
        if ($line.StartsWith('+++ ')) {
            if ($line -match '^\+\+\+ b/(.+)$') {
                $current = $Matches[1]
                if (-not $hunks.ContainsKey($current)) { $hunks[$current] = @() }
            }
            continue
        }
        if ($current -and $line.StartsWith('@@')) {
            if ($line -match '@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@') {
                $start = [int]$Matches[1]
                $count = if ($Matches[2]) { [int]$Matches[2] } else { 1 }
                if ($count -eq 0) { continue }
                $hunks[$current] += [pscustomobject]@{ startLine = $start; endLine = $start + $count - 1 }
            }
        }
    }
    return $hunks
}

# Split a multi-file `git diff` output into { path -> full per-file diff body }.
# Each value includes its `diff --git` header so the caller can stream it back
# without having to rebuild one.
function Split-DiffByFile {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$DiffText)
    $files = @{}
    if (-not $DiffText) { return $files }
    $currentPath = $null
    $buf = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $DiffText -split "`n") {
        if ($line.StartsWith('diff --git ')) {
            if ($currentPath) { $files[$currentPath] = ($buf -join "`n") }
            if ($line -match ' b/(.+)$') { $currentPath = $Matches[1] } else { $currentPath = $null }
            $buf = [System.Collections.Generic.List[string]]::new()
            [void]$buf.Add($line)
        } elseif ($currentPath) {
            [void]$buf.Add($line)
        }
    }
    if ($currentPath) { $files[$currentPath] = ($buf -join "`n") }
    return $files
}

# Conservative style scan used by diff-reader.ps1 (callable from parallel
# runspaces because it's defined here and _shared.ps1 is dot-sourced at the
# top of each one). Rules match what agent-rules.md → Agent Guardrails
# actually permits flagging — nothing speculative, nothing that conflicts
# with the repo's intentional column-aligned formatting.
#
#   trailing_whitespace    — line ending in spaces/tabs (including blank
#                            lines that contain only spaces/tabs).
#   three_plus_blank_lines — run of 3+ blank lines.
#   mixed_indent           — leading whitespace that starts with spaces and
#                            then has a tab. Tab-then-spaces is legitimate
#                            column alignment and is intentionally skipped.
function Find-StyleIssues {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
    $findings = @()
    if (-not $Body) { return $findings }
    $lines = $Body -split "`n"
    $blankRunStart = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNo = $i + 1
        $isBlank = ($line -match '^\s*$')
        if ($isBlank) {
            if ($blankRunStart -lt 0) { $blankRunStart = $lineNo }
            if ($line.Length -gt 0) {
                $findings += [pscustomobject]@{ kind = 'trailing_whitespace'; line = $lineNo; snippet = $line }
            }
        } else {
            if ($blankRunStart -ge 0) {
                $runLen = $lineNo - $blankRunStart
                if ($runLen -ge 3) {
                    $findings += [pscustomobject]@{ kind = 'three_plus_blank_lines'; line = $blankRunStart; lineEnd = $lineNo - 1 }
                }
                $blankRunStart = -1
            }
            if ($line -match '[ \t]+$') {
                $findings += [pscustomobject]@{ kind = 'trailing_whitespace'; line = $lineNo; snippet = $Matches[0] }
            }
            if ($line -match '^ +\t') {
                $leadMatch = [regex]::Match($line, '^[ \t]+')
                $lead = if ($leadMatch.Success) { $leadMatch.Value } else { '' }
                $findings += [pscustomobject]@{ kind = 'mixed_indent'; line = $lineNo; snippet = $lead }
            }
        }
    }
    if ($blankRunStart -ge 0) {
        $runLen = $lines.Count - $blankRunStart + 1
        if ($runLen -ge 3) {
            $findings += [pscustomobject]@{ kind = 'three_plus_blank_lines'; line = $blankRunStart; lineEnd = $lines.Count }
        }
    }
    return $findings
}

# Fingerprint a per-file diff body so the same logical change across providers
# collapses to the same string. Focus is the *change* — added and removed
# lines only. Context lines are dropped so that two diffs with identical
# insertions/deletions but slightly different surrounding SQL still group.
#
# Normalisation applied to each +/- line's content, in this order:
#   1. parameter prefixes `:p`/`@p`/`$1` → `?p`. Must run before alias
#      normalisation so the alias regex doesn't also devour param names.
#   2. short lowercase alias forms `t1` / `t_1` / `tbl1` / `c_2` / `x_1` /
#      `y1_1` → `ALIAS`. Lookbehind `(?<!\?)` excludes parameter names emitted
#      by step 1. The regex fires even *inside* quoted identifiers, so that a
#      DB2 `"c_2"` and an Oracle `c_2` both normalise toward the same token.
#   3. strip identifier quoting: `"foo"` / `` `foo` `` / `[foo]` → `foo`.
#      Running last unwraps anything that step 2 just turned into `"ALIAS"`,
#      so `"ALIAS"` and bare `ALIAS` compare equal regardless of whether the
#      provider dialect quotes identifiers.
#   4. trim trailing whitespace per line.
#
# Preamble (`diff --git`, `index`, `--- a/…`, `+++ b/…`), `@@` hunk headers,
# and all context lines are dropped entirely.
#
# Intentionally NOT normalised (would risk masking genuine divergence):
# paging syntax (TOP/LIMIT/FETCH/ROWNUM), boolean rendering, N-prefix on
# strings, case of SQL keywords.
function Get-DiffFingerprint {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Body)
    if (-not $Body) { return '' }
    $lines = $Body -split "`n"
    $out = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        if ($line.Length -eq 0) { continue }
        $marker = $line[0]
        # Keep only added/removed lines. Skip preamble lines `--- a/…` /
        # `+++ b/…` which also start with a marker but carry paths.
        if ($marker -ne '+' -and $marker -ne '-') { continue }
        if ($line.StartsWith('--- ') -or $line.StartsWith('+++ ')) { continue }
        $content = $line.Substring(1)
        $content = [regex]::Replace($content, '[:@$]([A-Za-z_][A-Za-z0-9_]*|\d+)', '?$1')
        $content = [regex]::Replace($content, '(?<!\?)\b[a-z][a-z0-9]{0,3}_?\d+\b', 'ALIAS')
        # Drop the three identifier-quoting wrappers last. Their contents
        # (already alias-normalised where applicable) come out as plain
        # tokens that compare equal across providers that quote vs don't.
        $content = [regex]::Replace($content, '"([A-Za-z_][A-Za-z0-9_]*)"', '$1')
        $content = [regex]::Replace($content, '`([A-Za-z_][A-Za-z0-9_]*)`', '$1')
        $content = [regex]::Replace($content, '\[([A-Za-z_][A-Za-z0-9_]*)\]', '$1')
        $content = $content -replace '[ \t]+$',''
        [void]$out.Add("$marker$content")
    }
    return ($out -join "`n")
}

# Short stable hash of an arbitrary string. Used as a grouping key; cryptographic
# strength isn't required. Returns first 16 hex chars of SHA1 (collisions are
# vanishingly unlikely for the cardinalities we group over).
function Get-ShortHash {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $sha = [System.Security.Cryptography.SHA1]::Create()
    try {
        $hashBytes = $sha.ComputeHash($bytes)
    } finally {
        $sha.Dispose()
    }
    $sb = [System.Text.StringBuilder]::new(16)
    for ($i = 0; $i -lt 8; $i++) { [void]$sb.AppendFormat('{0:x2}', $hashBytes[$i]) }
    return $sb.ToString()
}

function Test-RangeInHunks {
    param($Hunks, [int]$Line, $LineEnd)
    if (-not $Hunks -or $Hunks.Count -eq 0) { return $false }
    $a = $Line
    $b = if ($null -ne $LineEnd -and (Test-IsInteger $LineEnd) -and [long]$LineEnd -ge $Line) { [int]$LineEnd } else { $Line }
    foreach ($h in $Hunks) {
        if ($a -le $h.endLine -and $b -ge $h.startLine) { return $true }
    }
    return $false
}

# For parallel fan-out inside a script, dot-source this file again from within
# the `ForEach-Object -Parallel` scriptblock so `Invoke-Gh` / `Invoke-Git` etc.
# are available. The script's own `$PSScriptRoot` should be captured into the
# parallel block with `$using:`:
#
#     $root = $PSScriptRoot
#     $items | ForEach-Object -ThrottleLimit 8 -Parallel {
#         . "$using:root/_shared.ps1"
#         Invoke-Git @('show', "$using:headRef:$_")
#     }
#
# Use forward slashes in the dot-source path — backslashes are literal
# characters in filenames on Linux/macOS and the lookup fails there.
