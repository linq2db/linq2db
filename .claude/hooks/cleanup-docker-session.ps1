<#
SessionEnd hook: stops docker containers that were started during the current
Claude Code session (as captured by `track-docker-start.ps1`), then removes
the state file.

Reads `.build/.claude/docker-session-started.txt` — one container name per
line. Runs `docker stop <names...>` once for the whole set. Silent no-op
when the file is missing or empty. Errors go to stderr but never change
the exit code (Claude Code should not treat cleanup failures as fatal).

Wired in the user's `.claude/settings.local.json` under `hooks.SessionEnd`.
#>

$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

try {
    $stateFile = Join-Path (Get-Location) '.build/.claude/docker-session-started.txt'
    if (-not (Test-Path -LiteralPath $stateFile)) { exit 0 }

    $names = Get-Content -LiteralPath $stateFile -Encoding UTF8 |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -ne '' }

    if ($names.Count -gt 0) {
        & docker stop @names 2>&1 | Out-Null
    }

    Remove-Item -LiteralPath $stateFile -Force -ErrorAction SilentlyContinue
}
catch {
    [Console]::Error.WriteLine("cleanup-docker-session: $_")
}

exit 0
