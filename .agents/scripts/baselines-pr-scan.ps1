#!/usr/bin/env pwsh
<#
Whole-diff structural scan of a PR's baselines branch.

Why this wrapper exists
-----------------------
`/review-pr` step 8 rule 4 requires the *parent skill* (not the
`baselines-reviewer` subagent) to ground the baselines section in its own
measurements: headline A/M/D counts from the three-dot range, a net-line
split so cosmetic churn is separated from real SQL, a per-file delta for the
structural signal the PR's mechanism would move, and a provider histogram
bounding the blast radius. The subagent's grouped summary is sample-based and
has repeatedly under-reported these.

Hand-rolling the scan means several `git diff` calls plus throwaway analysis
scripts on every review. This does it in one call.

The rename map is the part that generalises: it normalises every parameter
identifier on both sides of each changed line-pair and byte-compares the
result, which turns "this change is name-only" from an author's claim into a
measurement — `mismatches` is 0 exactly when nothing but identifiers moved.

Invocation
----------
    pwsh -NoProfile -File .agents/scripts/baselines-pr-scan.ps1 -Pr 5733

Parameters
----------
  -Pr <int>            required — PR number, resolves branch `baselines/pr_<n>`
  -Clone <path>        default `../linq2db.baselines`
  -BaseRef <ref>       default `origin/master`
  -Signal <regex>      default `^\s*DECLARE\s` — the structural signal counted
                       per file on the added and removed sides. Override for a
                       mechanism whose fingerprint is not a parameter DECLARE
                       (e.g. `^\s*ORDER BY` , `\bas \[c\d+\]`).
  -IdentifierPrefixes  default `@:?$` — characters that introduce a parameter
                       identifier across providers. Used by the normaliser.
  -NoFetch             skip the `git fetch` of the baselines branch.
  -MaxRenameTests <int> default 40 — cap on test names listed per rename.

Output (stdout, single JSON object)
-----------------------------------
  {
    "status": "ok" | "branch_missing",
    "branch": "baselines/pr_5733",
    "branchTip":  { "sha": "...", "dateIso": "...", "subject": "..." },
    "counts": { "added": 0, "modified": 1335, "deleted": 0 },
    "lines":  { "added": 6784, "removed": 6784,
                "netZeroFiles": 1335, "netPositiveFiles": 0, "netNegativeFiles": 0 },
    "netPositive": [ { "file": "...", "add": 9, "del": 3 }, ... ],
    "netNegative": [ ... ],
    "signal": { "pattern": "^\\s*DECLARE\\s", "added": 2267, "removed": 2267,
                "filesWithDelta": [ { "file": "...", "added": 4, "removed": 2 } ] },
    "providers": [ { "provider": "SQLite.Classic", "files": 41 }, ... ],
    "renames": {
      "mismatches": 0,
      "mismatchSamples": [ { "file": "...", "old": "...", "new": "..." } ],
      "map": [ { "from": "ValueStr", "to": "param_1", "hits": 3150,
                 "providers": 40, "tests": [ "..." ] } ]
    }
  }

`renames.mismatches` counts changed line-pairs that still differ after
identifier normalisation — i.e. real SQL changes. A non-zero value means the
delta is NOT name-only, and `mismatchSamples` shows the first few.

Note: comparisons are Ordinal throughout. A case-only rename (`@Usage` ->
`@usage`) is a real rename and PowerShell's default case-insensitive `-ne`
would silently drop it — see `.agents/docs/script-authoring.md` → Gotchas.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][int] $Pr,
    [string]   $Clone              = '../linq2db.baselines',
    [string]   $BaseRef            = 'origin/master',
    [string]   $Signal             = '^\s*DECLARE\s',
    [string]   $IdentifierPrefixes = '@:?$',
    [switch]   $NoFetch,
    [int]      $MaxRenameTests     = 40
)

. "$PSScriptRoot/_shared.ps1"
$global:ScriptBaseName = 'baselines-pr-scan'

if (-not (Test-Path -LiteralPath $Clone)) {
    Exit-WithError -Message "baselines clone not found at '$Clone'" `
        -NextAction "clone https://github.com/linq2db/linq2db.baselines.git to '$Clone', or pass -Clone <path>"
}

$branch    = "baselines/pr_$Pr"
$remoteRef = "origin/$branch"

function Git {
    param([string[]]$GitArgs, [switch]$AllowFail)
    # Invoke-Process returns { ok, stdout, stderr, code, error } — lowercase.
    $r = Invoke-Process -FilePath 'git' -ArgumentList (@('-C', $Clone) + $GitArgs)
    if ($r.code -ne 0 -and -not $AllowFail) {
        Exit-WithError -Message "git $($GitArgs -join ' ') failed: $($r.stderr.Trim())"
    }
    return $r
}

if (-not $NoFetch) { [void](Git -GitArgs @('fetch', 'origin', $branch) -AllowFail) }

$verify = Git -GitArgs @('rev-parse', '--verify', '--quiet', $remoteRef) -AllowFail
if ($verify.code -ne 0 -or -not $verify.stdout.Trim()) {
    [Console]::Out.WriteLine((@{ status = 'branch_missing'; branch = $branch } | ConvertTo-Json -Depth 4))
    exit 0
}

$tip = (Git -GitArgs @('log', '-1', '--format=%H%x1f%cI%x1f%s', $remoteRef)).stdout.Trim() -split "`u{001f}"
$range = "$BaseRef...$remoteRef"

# ---- name-status -----------------------------------------------------------
$counts    = @{ added = 0; modified = 0; deleted = 0 }
$providers = [System.Collections.Generic.Dictionary[string, int]]::new([System.StringComparer]::Ordinal)

foreach ($line in ((Git -GitArgs @('diff', '--name-status', $range)).stdout -split "`n")) {
    if (-not $line.Trim()) { continue }
    $parts = $line -split "`t"
    switch ($parts[0].Substring(0, 1)) {
        'A' { $counts.added++ }
        'D' { $counts.deleted++ }
        'M' { $counts.modified++ }
    }
    $p = ($parts[-1] -split '/')[0]
    if (-not $providers.ContainsKey($p)) { $providers[$p] = 0 }
    $providers[$p]++
}

# ---- per-file line / signal / rename analysis over the -U0 diff ------------
$diff = (Git -GitArgs @('diff', '-U0', $range)).stdout

$signalRe = [regex]::new($Signal)
$normRe   = [regex]::new("(?<=[$([regex]::Escape($IdentifierPrefixes))])[A-Za-z_][A-Za-z0-9_]*")

$stats           = [System.Collections.Generic.List[object]]::new()
$renameMap       = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
$mismatchCount   = 0
$mismatchSamples = [System.Collections.Generic.List[object]]::new()

$curFile = $null
$add = [System.Collections.Generic.List[string]]::new()
$del = [System.Collections.Generic.List[string]]::new()

function Flush-File {
    param([string]$File, $Added, $Removed)
    if (-not $File) { return }

    $sigAdd = 0; $sigDel = 0
    foreach ($l in $Added)   { if ($signalRe.IsMatch($l)) { $sigAdd++ } }
    foreach ($l in $Removed) { if ($signalRe.IsMatch($l)) { $sigDel++ } }

    $script:stats.Add([pscustomobject]@{
        file = $File; add = $Added.Count; del = $Removed.Count
        sigAdd = $sigAdd; sigDel = $sigDel
    })

    # Rename extraction only makes sense on a symmetric change-block.
    if ($Added.Count -ne $Removed.Count) { return }

    $provider = ($File -split '/')[0]
    $test     = (($File -split '/')[-1] -replace '\(.*$', '')

    for ($i = 0; $i -lt $Added.Count; $i++) {
        $a = $Added[$i]; $d = $Removed[$i]
        if ([string]::Equals($script:normRe.Replace($a, 'X'), $script:normRe.Replace($d, 'X'), [System.StringComparison]::Ordinal)) {
            $am = $script:normRe.Matches($a); $dm = $script:normRe.Matches($d)
            if ($am.Count -ne $dm.Count) { continue }
            for ($k = 0; $k -lt $am.Count; $k++) {
                # Ordinal on purpose: a case-only rename is a real rename.
                if ([string]::Equals($dm[$k].Value, $am[$k].Value, [System.StringComparison]::Ordinal)) { continue }
                $key = "$($dm[$k].Value)`u{001f}$($am[$k].Value)"
                if (-not $script:renameMap.ContainsKey($key)) {
                    $script:renameMap[$key] = [pscustomobject]@{
                        from  = $dm[$k].Value
                        to    = $am[$k].Value
                        hits  = 0
                        provs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                        tests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                    }
                }
                $e = $script:renameMap[$key]
                $e.hits++
                [void]$e.provs.Add($provider)
                [void]$e.tests.Add($test)
            }
        }
        else {
            $script:mismatchCount++
            if ($script:mismatchSamples.Count -lt 25) {
                $script:mismatchSamples.Add([pscustomobject]@{ file = $File; old = $d; new = $a })
            }
        }
    }
}

foreach ($line in ($diff -split "`n")) {
    $line = $line.TrimEnd("`r")
    if ($line.StartsWith('diff --git ')) {
        Flush-File -File $curFile -Added $add -Removed $del
        $curFile = ($line -split ' b/', 2)[-1]
        $add = [System.Collections.Generic.List[string]]::new()
        $del = [System.Collections.Generic.List[string]]::new()
        continue
    }
    if (-not $curFile) { continue }
    if ($line.StartsWith('+++') -or $line.StartsWith('---') -or
        $line.StartsWith('@@')  -or $line.StartsWith('index ')) { continue }
    if     ($line.StartsWith('+')) { $add.Add($line.Substring(1)) }
    elseif ($line.StartsWith('-')) { $del.Add($line.Substring(1)) }
}
Flush-File -File $curFile -Added $add -Removed $del

# ---- assemble --------------------------------------------------------------
$netPositive = @(); $netNegative = @(); $netZero = 0
foreach ($s in $stats) {
    if     ($s.add -gt $s.del) { $netPositive += [pscustomobject]@{ file = $s.file; add = $s.add; del = $s.del } }
    elseif ($s.add -lt $s.del) { $netNegative += [pscustomobject]@{ file = $s.file; add = $s.add; del = $s.del } }
    else                       { $netZero++ }
}

$sigFiles = @()
foreach ($s in $stats) {
    if ($s.sigAdd -ne $s.sigDel) {
        $sigFiles += [pscustomobject]@{ file = $s.file; added = $s.sigAdd; removed = $s.sigDel }
    }
}

$renames = @()
foreach ($e in ($renameMap.Values | Sort-Object -Property hits -Descending)) {
    $renames += [pscustomobject]@{
        from      = $e.from
        to        = $e.to
        hits      = $e.hits
        providers = $e.provs.Count
        tests     = @($e.tests | Sort-Object | Select-Object -First $MaxRenameTests)
    }
}

$providerList = @()
foreach ($k in ($providers.Keys | Sort-Object { -$providers[$_] }, { $_ })) {
    $providerList += [pscustomobject]@{ provider = $k; files = $providers[$k] }
}

$result = [ordered]@{
    status    = 'ok'
    branch    = $branch
    range     = $range
    branchTip = [ordered]@{ sha = $tip[0]; dateIso = $tip[1]; subject = $tip[2] }
    counts    = [ordered]@{ added = $counts.added; modified = $counts.modified; deleted = $counts.deleted }
    lines     = [ordered]@{
        added            = ($stats | Measure-Object -Property add -Sum).Sum
        removed          = ($stats | Measure-Object -Property del -Sum).Sum
        netZeroFiles     = $netZero
        netPositiveFiles = $netPositive.Count
        netNegativeFiles = $netNegative.Count
    }
    netPositive = $netPositive
    netNegative = $netNegative
    signal      = [ordered]@{
        pattern        = $Signal
        added          = ($stats | Measure-Object -Property sigAdd -Sum).Sum
        removed        = ($stats | Measure-Object -Property sigDel -Sum).Sum
        filesWithDelta = $sigFiles
    }
    providers = $providerList
    renames   = [ordered]@{
        mismatches      = $mismatchCount
        mismatchSamples = @($mismatchSamples)
        map             = $renames
    }
}

[Console]::Out.WriteLine(($result | ConvertTo-Json -Depth 6))
