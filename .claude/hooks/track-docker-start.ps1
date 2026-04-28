<#
PostToolUse hook on Bash: track docker containers the current Claude Code
session started. Appends container names parsed from `docker start <name>...`
commands to `.build/.claude/docker-session-started.txt` (deduplicated, UTF-8
without BOM).

Consumers:
 - `.claude/hooks/cleanup-docker-session.ps1` (SessionEnd) — stops each
   tracked container at session exit.
 - Claude's scope-change rule — see `.claude/docs/agent-rules.md` →
   "Docker containers: start/stop/create only" → "Scope-change prompt for
   session-started containers".

Wired in the user's `.claude/settings.local.json` under `hooks.PostToolUse`
with matcher "Bash". Hook receives a JSON payload on stdin whose
`tool_input.command` is the literal Bash command string.
#>

$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

try {
    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

    $payload = $raw | ConvertFrom-Json -ErrorAction Stop
    if ($payload.tool_name -ne 'Bash') { exit 0 }

    $cmd = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($cmd)) { exit 0 }

    # Split on pipe / && / || / ; so `docker start foo && echo done` still matches.
    $segments = [regex]::Split($cmd, '\s*(?:\|\||&&|\||;)\s*')

    $started = New-Object System.Collections.Generic.List[string]
    foreach ($seg in $segments) {
        $s = $seg.Trim()
        if ($s -notmatch '^docker\s+start\b') { continue }

        # Strip leading `docker start`, tokenize, drop empty + flag-like tokens (-a, --attach, ...).
        $tail   = $s -replace '^docker\s+start\s*', ''
        $tokens = $tail -split '\s+' | Where-Object { $_ -and $_ -notlike '-*' }

        foreach ($t in $tokens) { $started.Add($t) }
    }

    if ($started.Count -eq 0) { exit 0 }

    $stateDir = Join-Path (Get-Location) '.build/.claude'
    [void](New-Item -ItemType Directory -Path $stateDir -Force)
    $stateFile = Join-Path $stateDir 'docker-session-started.txt'

    $existing = @()
    if (Test-Path $stateFile) {
        $existing = Get-Content -LiteralPath $stateFile -Encoding UTF8 | Where-Object { $_ -ne '' }
    }

    $all = @($existing + $started) | Sort-Object -Unique
    [System.IO.File]::WriteAllLines($stateFile, $all, [System.Text.UTF8Encoding]::new($false))
}
catch {
    # Never block the tool call on a hook error.
    [Console]::Error.WriteLine("track-docker-start: $_")
}

exit 0
