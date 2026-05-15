<#
release-prefetch.ps1 — parallel pre-fetch of read-only discovery
phases for `/release` orchestrator. Runs the discovery scripts for
tasks 1 (deps), 2 (PublicAPI), 3 (milestone-check), and 5 (release-
notes-validate) concurrently, writing each task's plan JSON to
.build/.claude/release-<ver>-<key>-plan.json so the matching sub-skill
can consume cached results instead of re-running discovery.

Task 0 (branch + version bump) is already complete by the time
pre-fetch runs (it's part of /release step 1). Task 4 (test-matrix)
is heavily user-interactive — no useful discovery to pre-fetch.
Task 6.x (ad-hoc) doesn't have a defined discovery shape.

Each task is independent — they read different inputs and write
different plan files. The PublicAPI sub-task chains its own three
steps (build → discover → plan) inside one parallel runspace so the
caller still sees one logical task.

Action:

  discover-all  Inputs:  -Version <ver>
                         [-Milestone <title>]    (defaults to -Version)
                         [-PrepPR <n>]           (optional; excluded from
                                                 milestone-check audit)
                         [-Repo <owner/repo>]    (default: linq2db/linq2db)
                         [-Tasks deps,publicapi,milestone,notes]
                                                 (subset; default: all four)
                         [-MaxParallel <n>]      (default: 4)
                         [-SkipFresh]            (skip a task if its plan
                                                 file is < FreshnessMin
                                                 minutes old)
                         [-FreshnessMin <n>]     (default: 30)
              Output:    { ok, tasks: [{ key, status, planFile,
                           elapsedSec, fromCache, error? }],
                           totalElapsedSec }

  status        Inputs:  -Version <ver>
              Output:    { ok, tasks: [{ key, planFile, exists,
                           ageMinutes, sizeBytes }] }

Concurrency: ForEach-Object -ThrottleLimit. Each runspace dot-sources
_shared.ps1 fresh so Invoke-Process etc. are available. Stdout from
the inner script is captured to the plan file only if the script
doesn't write its own (currently: milestone-audit emits to stdout
and prefetch redirects it; the other three write their plan files
directly).

Conventions: `.claude/docs/script-authoring.md`.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('discover-all','status')]
    [string]$Action,

    [Parameter(Mandatory)][string]$Version,
    [string]$Milestone,
    [int]$PrepPR,
    [string]$Repo = 'linq2db/linq2db',
    [string]$Tasks,
    [int]$MaxParallel = 4,
    [switch]$SkipFresh,
    [int]$FreshnessMin = 30
)

$global:ScriptBaseName = 'release-prefetch'
. (Join-Path $PSScriptRoot '_shared.ps1')

if (-not $Milestone) { $Milestone = $Version }

# -- paths -------------------------------------------------------------------

function Get-WorkDir {
    $dir = Join-Path (Get-Location) '.build/.claude'
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    return $dir
}

function Get-PlanPath {
    param([string]$Ver, [string]$Key)
    return (Join-Path (Get-WorkDir) "release-$Ver-$Key-plan.json")
}

# -- task catalogue ----------------------------------------------------------

# Each entry describes one sub-task's pre-fetch invocation. Fields:
#   key             short identifier — names the plan file slug
#   label           human-readable name for logging
#   scriptFile      .ps1 under .claude/scripts/ to invoke
#   chainArgs       array of pre-action argument arrays (PublicAPI runs
#                   build → discover → plan as three chained invocations)
#   mainArgs        argument array for the final invocation
#   planFromStdout  $true when the script doesn't write a plan file itself;
#                   pre-fetch redirects stdout to the plan path.
#
# Args are built eagerly in the serial preface (not as scriptblocks) because
# ForEach-Object -Parallel serialises captured objects across runspaces and
# does not preserve scriptblock parameter binding — invoking `& $sb $arg`
# inside a parallel runspace silently no-ops on the bind, producing empty
# arg arrays and "missing mandatory parameter" errors from the child.
function Get-TaskCatalogue {
    param([string]$V, [string]$M, [int]$P, [string]$R)
    $milestoneArgs = @('-Action','audit','-Milestone',$M,'-Repo',$R)
    if ($P) { $milestoneArgs += @('-PrepPR',[string]$P) }
    return @(
        @{
            key            = 'deps'
            label          = 'task 1 — /release-deps discovery'
            scriptFile     = 'release-deps-discover.ps1'
            chainArgs      = @()
            mainArgs       = @('-Action','discover','-Version',$V)
            planFromStdout = $false
        }
        @{
            key            = 'publicapi'
            label          = 'task 2 — /release-publicapi build + plan'
            scriptFile     = 'release-publicapi-reconcile.ps1'
            chainArgs      = @(
                ,@('-Action','build','-Version',$V)
                ,@('-Action','discover','-Version',$V)
            )
            mainArgs       = @('-Action','plan','-Version',$V)
            planFromStdout = $false
        }
        @{
            key            = 'milestone'
            label          = 'task 3 — /release-milestone-check audit'
            scriptFile     = 'release-milestone-audit.ps1'
            chainArgs      = @()
            mainArgs       = $milestoneArgs
            planFromStdout = $true   # script emits JSON to stdout — prefetch captures
        }
        @{
            key            = 'notes'
            label          = 'task 5 — /release-notes-validate audit'
            scriptFile     = 'release-notes-audit.ps1'
            chainArgs      = @()
            mainArgs       = @('-Action','audit','-Milestone',$M,'-Version',$V,'-Repo',$R)
            planFromStdout = $false
        }
    )
}

# -- status ------------------------------------------------------------------

function Do-Status {
    $catalogue = Get-TaskCatalogue -V $Version -M $Milestone -P $PrepPR -R $Repo
    $out = New-Object 'System.Collections.Generic.List[object]'
    foreach ($t in $catalogue) {
        $p = Get-PlanPath -Ver $Version -Key $t.key
        $exists = Test-Path -LiteralPath $p
        $age = $null
        $size = 0
        if ($exists) {
            $f = Get-Item -LiteralPath $p
            $age = [math]::Round(((Get-Date) - $f.LastWriteTime).TotalMinutes, 1)
            $size = $f.Length
        }
        $out.Add([ordered]@{
            key        = $t.key
            label      = $t.label
            planFile   = $p
            exists     = $exists
            ageMinutes = $age
            sizeBytes  = $size
        }) | Out-Null
    }
    Write-JsonOutput @{ ok = $true; action = 'status'; tasks = $out.ToArray() }
}

# -- discover-all ------------------------------------------------------------

function Do-DiscoverAll {
    $catalogue = Get-TaskCatalogue -V $Version -M $Milestone -P $PrepPR -R $Repo
    $selected = @($catalogue)
    if ($Tasks) {
        $wanted = ($Tasks -split ',') | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ }
        $selected = @($catalogue | Where-Object { $wanted -contains $_.key })
    }
    if ($selected.Count -eq 0) { Exit-WithError "no tasks selected (Tasks='$Tasks')" }

    $rootStart = Get-Date

    # Pre-compute plan paths and freshness so the parallel block doesn't
    # have to re-derive them. SkipFresh decisions are made here in the
    # serial preface so the user sees a single "skipping <key>: fresh"
    # message per task rather than racing log lines.
    $jobInputs = @()
    foreach ($t in $selected) {
        $p = Get-PlanPath -Ver $Version -Key $t.key
        $fresh = $false
        if ($SkipFresh -and (Test-Path -LiteralPath $p)) {
            $f = Get-Item -LiteralPath $p
            $age = ((Get-Date) - $f.LastWriteTime).TotalMinutes
            if ($age -lt $FreshnessMin) { $fresh = $true }
        }
        $jobInputs += [pscustomobject]@{ task = $t; planFile = $p; fresh = $fresh }
    }

    $scriptsRoot = $PSScriptRoot

    $results = $jobInputs | ForEach-Object -ThrottleLimit $MaxParallel -Parallel {
        $j = $_
        $t = $j.task
        $planFile = $j.planFile
        $start = Get-Date

        if ($j.fresh) {
            return [pscustomobject]@{
                key        = $t.key
                label      = $t.label
                status     = 'cached'
                fromCache  = $true
                planFile   = $planFile
                elapsedSec = 0
                error      = $null
            }
        }

        $scriptPath = Join-Path $using:scriptsRoot $t.scriptFile
        if (-not (Test-Path -LiteralPath $scriptPath)) {
            return [pscustomobject]@{
                key        = $t.key
                label      = $t.label
                status     = 'error'
                fromCache  = $false
                planFile   = $planFile
                elapsedSec = ((Get-Date) - $start).TotalSeconds
                error      = "script not found: $scriptPath"
            }
        }

        # Helper to invoke pwsh on the inner script with a given args array.
        # CRITICAL: redirect stdin and close it immediately. Without this the
        # child process inherits the parent's stdin handle. Inside a
        # ForEach-Object -Parallel runspace the inherited handle is opaque —
        # the child blocks indefinitely on any incidental Read-Host /
        # [Console]::In.Peek() / startup checks that touch stdin. Closing
        # stdin makes the child see EOF immediately and proceed.
        # NB: param name is `$argList`, NOT `$args` — `$args` is an automatic
        # variable in scriptblocks and cannot be rebound via `param()`.
        # Naming the parameter `$args` produces an empty array at call time
        # and the child pwsh sees no arguments.
        $invoke = {
            param([string]$path, [string[]]$argList)
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = 'pwsh'
            [void]$psi.ArgumentList.Add('-NoProfile')
            [void]$psi.ArgumentList.Add('-NonInteractive')
            [void]$psi.ArgumentList.Add('-File')
            [void]$psi.ArgumentList.Add($path)
            foreach ($a in $argList) { [void]$psi.ArgumentList.Add([string]$a) }
            $psi.RedirectStandardInput  = $true
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError  = $true
            $psi.UseShellExecute = $false
            $psi.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
            $psi.StandardErrorEncoding  = [System.Text.UTF8Encoding]::new($false)
            $p = [System.Diagnostics.Process]::Start($psi)
            try {
                try { $p.StandardInput.Close() } catch { }
                $outTask = $p.StandardOutput.ReadToEndAsync()
                $errTask = $p.StandardError.ReadToEndAsync()
                $p.WaitForExit()
                return [pscustomobject]@{
                    code   = $p.ExitCode
                    stdout = $outTask.GetAwaiter().GetResult()
                    stderr = $errTask.GetAwaiter().GetResult()
                }
            } finally { $p.Dispose() }
        }

        # Verdict on a child invocation: failing exit code OR JSON ok=false.
        # Some scripts (e.g. release-publicapi-reconcile -Action build) exit 0
        # while emitting `{ ok: false, ... }` JSON on stdout to indicate the
        # semantic operation failed. Treating exit-code-0 as success would let
        # prefetch cache a stale/invalid plan. Parse the stdout JSON when
        # present and gate on `.ok`; scripts that emit non-JSON stdout (or
        # nothing) fall back to the exit-code verdict (parse failure = accept).
        $verdictFromResult = {
            param([pscustomobject]$Result, [string]$Label)
            if ($Result.code -ne 0) {
                return [pscustomobject]@{ ok = $false; error = "${Label} exited $($Result.code): $($Result.stderr.Trim())" }
            }
            if (-not $Result.stdout) { return [pscustomobject]@{ ok = $true; error = $null } }
            try {
                $obj = $Result.stdout | ConvertFrom-Json -ErrorAction Stop
            } catch {
                return [pscustomobject]@{ ok = $true; error = $null }
            }
            if ($null -ne $obj -and $obj.PSObject.Properties['ok'] -and $obj.ok -eq $false) {
                $err = if ($obj.PSObject.Properties['error'] -and $obj.error) { [string]$obj.error } else { 'script reported ok=false' }
                return [pscustomobject]@{ ok = $false; error = "${Label} ok=false: $err" }
            }
            return [pscustomobject]@{ ok = $true; error = $null }
        }

        # Run any chain steps first; abort on first failure.
        foreach ($stepArgs in @($t.chainArgs)) {
            $r = & $invoke $scriptPath ([string[]]$stepArgs)
            $v = & $verdictFromResult $r 'chain step'
            if (-not $v.ok) {
                return [pscustomobject]@{
                    key        = $t.key
                    label      = $t.label
                    status     = 'error'
                    fromCache  = $false
                    planFile   = $planFile
                    elapsedSec = ((Get-Date) - $start).TotalSeconds
                    error      = $v.error
                }
            }
        }

        # Run the main step.
        $r = & $invoke $scriptPath ([string[]]$t.mainArgs)
        $v = & $verdictFromResult $r 'main step'
        if (-not $v.ok) {
            return [pscustomobject]@{
                key        = $t.key
                label      = $t.label
                status     = 'error'
                fromCache  = $false
                planFile   = $planFile
                elapsedSec = ((Get-Date) - $start).TotalSeconds
                error      = $v.error
            }
        }

        # For scripts that emit their plan to stdout (milestone-audit),
        # capture and persist it here. The plan-file format is consistent —
        # whatever JSON the script returned, the sub-skill knows how to
        # read.
        if ($t.planFromStdout) {
            [System.IO.File]::WriteAllText($planFile, $r.stdout, [System.Text.UTF8Encoding]::new($false))
        }

        return [pscustomobject]@{
            key        = $t.key
            label      = $t.label
            status     = 'ok'
            fromCache  = $false
            planFile   = $planFile
            elapsedSec = ((Get-Date) - $start).TotalSeconds
            error      = $null
        }
    }

    $rootElapsed = ((Get-Date) - $rootStart).TotalSeconds
    Write-JsonOutput @{
        ok              = $true
        action          = 'discover-all'
        version         = $Version
        milestone       = $Milestone
        tasks           = @($results)
        totalElapsedSec = $rootElapsed
    }
}

# -- dispatch ----------------------------------------------------------------

switch ($Action) {
    'discover-all' { Do-DiscoverAll }
    'status'       { Do-Status }
    default        { Exit-WithError "unknown action: $Action" }
}
