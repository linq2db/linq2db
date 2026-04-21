#!/usr/bin/env pwsh
<#
One-shot baselines diff reader for the `baselines-reviewer` subagent.

Why this wrapper exists
-----------------------
Baselines lives in a sibling clone at `../linq2db.baselines`. The reviewer
needs the changed-file list, the per-file diff body, and the path parsed
into `(provider, test, params)` per the grammar in
`.claude/docs/baselines-repo-layout.md`. Running that as raw Bash fires
`git -C ../linq2db.baselines …` once per file, each triggering its own
permission prompt. This script answers everything in one call:

    Bash(pwsh -NoProfile -File .claude/scripts/baselines-diff.ps1:*)

Input (stdin, JSON)
-------------------
  {
    "baselinesPath":  "../linq2db.baselines",   // optional, default
    "pr":             5414,                      // required, used to derive branch
    "branch":         "baselines/pr_5414",       // optional, default "baselines/pr_<pr>"
    "baseRef":        "origin/master",           // optional
    "maxDiffBytes":   16384,                     // optional — per-file diff truncation; 0 = no limit
    "fetch":          false,                     // optional, default false — caller already fetched
    "baselineOwner":  "linq2db",                 // optional — owner of baselines repo on GitHub
    "baselineRepo":   "linq2db.baselines"        // optional — baselines repo name on GitHub
  }

Output (stdout, single JSON object): { status, pr, branch, baseRef,
baselineRepo, baselineBranchUrl, baselineCompareUrl, baselineReview,
counts, summary, testGroupSummary[], changePatterns[], sql[], metrics[],
unknown[], testGroups }. When status == "branch_missing", only the
header fields plus an all-zero `counts` are emitted; the rest are
omitted.
  `baselineRepo` — "owner/name" of the baselines repo on GitHub.
  `baselineBranchUrl` — https://github.com/<repo>/tree/<branch>.
  `baselineCompareUrl` — compare view master...branch.
  `baselineReview` — metadata on the baselines repo PR for this branch, or
    null when none exists / gh lookup failed. Shape:
    { number, state, url, title }. Prefers the open PR; falls back to the
    most recently updated closed/merged PR for branch history.
  `summary` — bucket sizes + sorted provider / TFM lists:
    { sqlCount, metricsCount, unknownCount, testGroupCount,
      providers:[...], tfms:[...] }.
  `testGroupSummary` — one entry per logical test, sorted by entry count desc
    then provider count desc then test name asc:
    [{ test, providerCount, entryCount }, ...]. Lets the caller rank groups
    without reading the full `testGroups` map.
  `changePatterns` — SQL entries grouped by (testBase, normalisedDiffHash).
    One entry per distinct change pattern; providers sharing that pattern are
    rolled up into a list. Sorted by providerCount desc, then testBase asc.
    Each entry: { testBase, patternHash, providerCount, providers[],
    sampleProvider, samplePath, sampleDiff, sampleDiffTruncated,
    sampleStatus, sampleUrl, status }.
    Fingerprint is computed from +/- lines only (context dropped) after
    normalising identifier quoting, parameter prefixes, and short alias
    forms — see `Get-DiffFingerprint` in `_shared.ps1`. Divergent providers
    on the same test surface as separate entries with the same `testBase`,
    which is the signal to compare samples manually. Each entry also
    carries `sampleStatus` (the first-seen git status: A/M/D/...) and
    `sampleUrl` (GitHub blob URL on the baselines branch, or null when the
    sample entry is a deletion).
  `testGroups[key]` — gains `providerCount` / `entryCount` alongside the full
    `providers` and `entries` arrays so a consumer that already holds the map
    doesn't need to re-count.

Exit codes
----------
  0 = success
  1 = hard failure
#>

$global:ScriptBaseName = 'baselines-diff'
. "$PSScriptRoot/_shared.ps1"

# Parse an SQL baseline path per .claude/docs/baselines-repo-layout.md.
# Shape: <Provider>/<ns1>/.../<ClassName>/<full.ns.Class>.<Method>(<params>).sql
# Returns $null for paths that don't fit (flagged as unknown).
function ConvertFrom-SqlPath {
    param([string]$p)
    if (-not $p.EndsWith('.sql')) { return $null }
    $slashIdx = $p.IndexOf('/')
    if ($slashIdx -le 0) { return $null }
    $provider = $p.Substring(0, $slashIdx)
    $rest = $p.Substring($slashIdx + 1)

    $lastSlash = $rest.LastIndexOf('/')
    if ($lastSlash -lt 0) { return $null }
    $filename = $rest.Substring($lastSlash + 1)

    $openParen = $filename.IndexOf('(')
    $closeParen = $filename.LastIndexOf(')')
    if ($openParen -lt 0 -or $closeParen -lt 0 -or $closeParen -lt $openParen) { return $null }
    $before = $filename.Substring(0, $openParen)
    $paramsRaw = $filename.Substring($openParen + 1, $closeParen - $openParen - 1)

    $lastDot = $before.LastIndexOf('.')
    if ($lastDot -lt 0) { return $null }
    $method = $before.Substring($lastDot + 1)
    $classFQN = $before.Substring(0, $lastDot)
    $classDot = $classFQN.LastIndexOf('.')
    $className = if ($classDot -lt 0) { $classFQN } else { $classFQN.Substring($classDot + 1) }
    $namespace = if ($classDot -lt 0) { '' } else { $classFQN.Substring(0, $classDot) }

    # Statement form — PS unwraps @() in a single-expression if/else into $null.
    $params = @()
    if ($paramsRaw.Length -gt 0) {
        $params = @(($paramsRaw -split ',') | ForEach-Object { $_.Trim() })
    }

    return [pscustomobject]@{
        provider = $provider
        namespace = $namespace
        class = $className
        method = $method
        params = $params
        testKey = "$classFQN.$method($paramsRaw)"
        testBase = "$classFQN.$method"
    }
}

# Metrics baselines: <TFM>/<Provider>.<OS>.Metrics.txt.
function ConvertFrom-MetricsPath {
    param([string]$p)
    if (-not $p.EndsWith('.Metrics.txt')) { return $null }
    $parts = $p -split '/'
    if ($parts.Length -ne 2) { return $null }
    $tfm = $parts[0]
    $basename = $parts[1].Substring(0, $parts[1].Length - '.Metrics.txt'.Length)
    $lastDot = $basename.LastIndexOf('.')
    if ($lastDot -lt 0) { return $null }
    $os = $basename.Substring($lastDot + 1)
    $provider = $basename.Substring(0, $lastDot)
    return [pscustomobject]@{ tfm = $tfm; provider = $provider; os = $os }
}

function Get-TruncatedDiff {
    param([string]$body, [int]$limit)
    if (-not $limit -or $limit -le 0) { return @{ body = $body; truncated = $false } }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
    if ($bytes.Length -le $limit) { return @{ body = $body; truncated = $false } }
    $slice = New-Object byte[] $limit
    [Array]::Copy($bytes, 0, $slice, 0, $limit)
    $cut = [System.Text.Encoding]::UTF8.GetString($slice)
    return @{
        body = "$cut`n… [truncated; total $($bytes.Length) bytes, showing first $limit]"
        truncated = $true
    }
}

$m = Read-StdinJson

if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) { Exit-WithError 'pr (positive integer) required' }
$pr = [int]$m.pr
$clonePath = if ($m.baselinesPath) { [string]$m.baselinesPath } else { '../linq2db.baselines' }
$branch = if ($m.branch) { [string]$m.branch } else { "baselines/pr_$pr" }
$baseRef = if ($m.baseRef) { [string]$m.baseRef } else { 'origin/master' }
$remoteRef = "origin/$branch"
$maxDiffBytes = if (Test-IsInteger $m.maxDiffBytes) { [int]$m.maxDiffBytes } else { 16384 }
$baselineOwner = if ($m.baselineOwner) { [string]$m.baselineOwner } else { 'linq2db' }
$baselineRepoName = if ($m.baselineRepo) { [string]$m.baselineRepo } else { 'linq2db.baselines' }
$baselineRepoFull = "${baselineOwner}/${baselineRepoName}"
$baselineBranchUrl = "https://github.com/${baselineRepoFull}/tree/${branch}"
$baselineCompareUrl = "https://github.com/${baselineRepoFull}/compare/master...${branch}"

# URL encoder for path segments — GitHub blob URLs handle `/` as separator
# but need encoding on other special characters (parentheses, spaces, etc.).
function ConvertTo-GitHubBlobPath {
    param([Parameter(Mandatory)][string]$Path)
    $parts = $Path -split '/'
    $encoded = $parts | ForEach-Object { [System.Uri]::EscapeDataString($_) }
    return ($encoded -join '/')
}

if ($m.fetch) {
    $f = Invoke-Git @('-C',$clonePath,'fetch','origin')
    if (-not $f.ok) { Exit-WithError "git fetch ${clonePath}: $($f.error)" }
}

# Look up the baselines-repo PR for this branch. Open PR is preferred; fall
# back to the most recently updated closed/merged one so reviewers still get
# a link for history when the branch has already been merged. Non-fatal on
# failure — gh may be offline or unauthorised; we just emit null.
function Find-BaselineReview {
    param([string]$Owner, [string]$Repo, [string]$Head)
    $args = @(
        'pr','list',
        '--repo', "${Owner}/${Repo}",
        '--head', $Head,
        '--state','all',
        '--json','number,state,url,title,updatedAt'
    )
    $res = Invoke-GhJson -ArgumentList $args
    if (-not $res.ok) { return $null }
    $prs = @($res.data)
    if ($prs.Count -eq 0) { return $null }
    $open = $prs | Where-Object { $_.state -eq 'OPEN' }
    $pick = if ($open) { $open | Sort-Object -Property updatedAt -Descending | Select-Object -First 1 }
            else       { $prs  | Sort-Object -Property updatedAt -Descending | Select-Object -First 1 }
    return [pscustomobject]@{
        number = [int]$pick.number
        state = [string]$pick.state
        url = [string]$pick.url
        title = [string]$pick.title
    }
}

$baselineReview = Find-BaselineReview -Owner $baselineOwner -Repo $baselineRepoName -Head $branch

$rev = Invoke-Git @('-C',$clonePath,'rev-parse','--verify',"refs/remotes/$remoteRef")
if (-not $rev.ok) {
    Write-JsonOutput ([pscustomobject]@{
        status = 'branch_missing'
        pr = $pr
        branch = $branch
        baseRef = $baseRef
        baselineRepo = $baselineRepoFull
        baselineBranchUrl = $baselineBranchUrl
        baselineCompareUrl = $baselineCompareUrl
        baselineReview = $baselineReview
        counts = [pscustomobject]@{ added = 0; modified = 0; deleted = 0; renamed = 0; other = 0 }
    })
    return
}

$nameStatusRes = Invoke-Git @('-C',$clonePath,'diff','--name-status',"$baseRef...$remoteRef")
if (-not $nameStatusRes.ok) { Exit-WithError "git diff --name-status: $($nameStatusRes.error)" }

$entries = @()
foreach ($line in ($nameStatusRes.stdout -split "`n")) {
    if (-not $line) { continue }
    $parts = $line -split "`t"
    $status = $parts[0]
    if ($status.StartsWith('R') -or $status.StartsWith('C')) {
        $entries += [pscustomobject]@{ status = $status; oldPath = $parts[1]; path = $parts[2] }
    } else {
        $entries += [pscustomobject]@{ status = $status; path = $parts[1] }
    }
}

$counts = [ordered]@{ added = 0; modified = 0; deleted = 0; renamed = 0; other = 0 }
foreach ($e in $entries) {
    $s = $e.status[0]
    switch ($s) {
        'A' { $counts.added++ }
        'M' { $counts.modified++ }
        'D' { $counts.deleted++ }
        'R' { $counts.renamed++ }
        default { $counts.other++ }
    }
}

# One `git diff` for the whole range — no pathspec. Listing every changed
# path on the command line blows past ENAMETOOLONG on large baseline PRs.
$diffBodies = @{}
if ($entries.Count -gt 0) {
    $diffRes = Invoke-Git @('-C',$clonePath,'diff',"$baseRef...$remoteRef")
    if (-not $diffRes.ok) { Exit-WithError "git diff (bodies): $($diffRes.error)" }
    $diffBodies = Split-DiffByFile -DiffText $diffRes.stdout
}

$sql = @()
$metrics = @()
$unknown = @()
$groupMap = @{}

foreach ($e in $entries) {
    $rawDiff = if ($diffBodies.ContainsKey($e.path)) { $diffBodies[$e.path] } else { '' }
    $trunc = Get-TruncatedDiff -body $rawDiff -limit $maxDiffBytes
    $diff = $trunc.body
    $diffTruncated = $trunc.truncated

    if ($e.path.EndsWith('.sql')) {
        $parsed = ConvertFrom-SqlPath -p $e.path
        if (-not $parsed) {
            $unknown += [pscustomobject]@{ path = $e.path; status = $e.status; reason = 'SQL path does not match grammar' }
            continue
        }
        $sql += [pscustomobject]@{
            path = $e.path
            status = $e.status
            provider = $parsed.provider
            namespace = $parsed.namespace
            class = $parsed.class
            method = $parsed.method
            params = $parsed.params
            testKey = $parsed.testKey
            diff = $diff
            diffTruncated = $diffTruncated
        }

        if (-not $groupMap.ContainsKey($parsed.testBase)) {
            $groupMap[$parsed.testBase] = @{
                test = $parsed.testBase
                providers = [System.Collections.Generic.HashSet[string]]::new()
                entries = @()
            }
        }
        [void]$groupMap[$parsed.testBase].providers.Add($parsed.provider)
        $groupMap[$parsed.testBase].entries += [pscustomobject]@{
            provider = $parsed.provider
            params = $parsed.params
            status = $e.status
            path = $e.path
        }
        continue
    }

    if ($e.path.EndsWith('.Metrics.txt')) {
        $parsed = ConvertFrom-MetricsPath -p $e.path
        if (-not $parsed) {
            $unknown += [pscustomobject]@{ path = $e.path; status = $e.status; reason = 'Metrics path does not match grammar' }
            continue
        }
        $metrics += [pscustomobject]@{
            path = $e.path
            status = $e.status
            tfm = $parsed.tfm
            provider = $parsed.provider
            os = $parsed.os
            diff = $diff
            diffTruncated = $diffTruncated
        }
        continue
    }

    $unknown += [pscustomobject]@{ path = $e.path; status = $e.status; reason = 'unrecognised baseline path' }
}

# Build changePatterns: one entry per unique (testBase, normalisedDiffHash).
# sql[] already has raw `diff` bodies — normalise each, hash, and fold entries
# with the same key together. Keeps the first-seen provider as the sample.
$patternMap = [ordered]@{}
foreach ($s in $sql) {
    $fingerprint = Get-DiffFingerprint -Body $s.diff
    $hash = Get-ShortHash -Text $fingerprint
    $classFQN = if ($s.namespace) { "$($s.namespace).$($s.class)" } else { $s.class }
    $testBase = "$classFQN.$($s.method)"
    $key = "${testBase}::${hash}"
    if (-not $patternMap.Contains($key)) {
        $patternMap[$key] = [pscustomobject]@{
            testBase = $testBase
            patternHash = $hash
            providers = [System.Collections.Generic.List[string]]::new()
            sampleProvider = $s.provider
            samplePath = $s.path
            sampleDiff = $s.diff
            sampleDiffTruncated = [bool]$s.diffTruncated
            sampleStatus = [string]$s.status
            statuses = [System.Collections.Generic.HashSet[string]]::new()
        }
    }
    [void]$patternMap[$key].providers.Add($s.provider)
    [void]$patternMap[$key].statuses.Add($s.status)
}
$changePatterns = @()
foreach ($val in $patternMap.Values) {
    $providersSorted = @($val.providers | Sort-Object -Unique)
    $statusList = @($val.statuses | Sort-Object)
    $statusLabel = if ($statusList.Count -eq 1) { $statusList[0] } else { ($statusList -join ',') }
    # Only produce a blob URL for samples that still exist on the branch.
    # Deletions (status 'D') return 404 at /blob/ so emit null instead.
    $sampleUrl = $null
    $statusHead = if ($val.sampleStatus) { $val.sampleStatus[0] } else { '' }
    if ($statusHead -and $statusHead -ne 'D') {
        $sampleUrl = "https://github.com/${baselineRepoFull}/blob/${branch}/$(ConvertTo-GitHubBlobPath -Path $val.samplePath)"
    }
    $changePatterns += [pscustomobject]@{
        testBase = $val.testBase
        patternHash = $val.patternHash
        providerCount = $providersSorted.Count
        providers = $providersSorted
        sampleProvider = $val.sampleProvider
        samplePath = $val.samplePath
        sampleDiff = $val.sampleDiff
        sampleDiffTruncated = $val.sampleDiffTruncated
        sampleStatus = $val.sampleStatus
        sampleUrl = $sampleUrl
        status = $statusLabel
    }
}
$changePatterns = @($changePatterns | Sort-Object -Property @{ Expression = 'providerCount'; Descending = $true }, @{ Expression = 'testBase'; Descending = $false })

$testGroups = [ordered]@{}
$testGroupSummary = @()
foreach ($key in ($groupMap.Keys | Sort-Object)) {
    $b = $groupMap[$key]
    $providersSorted = @($b.providers | Sort-Object)
    $entriesSorted = @($b.entries | Sort-Object -Property provider)
    $testGroups[$key] = [pscustomobject]@{
        test = $b.test
        providerCount = $providersSorted.Count
        entryCount = $entriesSorted.Count
        providers = $providersSorted
        entries = $entriesSorted
    }
    $testGroupSummary += [pscustomobject]@{
        test = $b.test
        providerCount = $providersSorted.Count
        entryCount = $entriesSorted.Count
    }
}
# Rank-order: largest groups first so a caller reading only the head of the
# persisted output sees the biggest work items.
$testGroupSummary = @($testGroupSummary | Sort-Object -Property @{ Expression = 'entryCount'; Descending = $true }, @{ Expression = 'providerCount'; Descending = $true }, @{ Expression = 'test'; Descending = $false })

# Orientation summary — precomputed so callers don't have to reparse the blob
# (e.g. via an ad-hoc `pwsh -Command "...ConvertFrom-Json..."` probe) just to
# know how big each bucket is or which providers / TFMs are touched.
$providerSet = [System.Collections.Generic.HashSet[string]]::new()
foreach ($s in $sql) { if ($s.provider) { [void]$providerSet.Add($s.provider) } }
foreach ($mm in $metrics) { if ($mm.provider) { [void]$providerSet.Add($mm.provider) } }
$tfmSet = [System.Collections.Generic.HashSet[string]]::new()
foreach ($mm in $metrics) { if ($mm.tfm) { [void]$tfmSet.Add($mm.tfm) } }

$summary = [pscustomobject]@{
    sqlCount       = $sql.Count
    metricsCount   = $metrics.Count
    unknownCount   = $unknown.Count
    testGroupCount = $testGroups.Keys.Count
    providers      = @($providerSet | Sort-Object)
    tfms           = @($tfmSet | Sort-Object)
}

Write-JsonOutput ([pscustomobject]@{
    status = 'changed'
    pr = $pr
    branch = $branch
    baseRef = $baseRef
    baselineRepo = $baselineRepoFull
    baselineBranchUrl = $baselineBranchUrl
    baselineCompareUrl = $baselineCompareUrl
    baselineReview = $baselineReview
    counts = [pscustomobject]$counts
    summary = $summary
    testGroupSummary = @($testGroupSummary)
    changePatterns = @($changePatterns)
    sql = @($sql)
    metrics = @($metrics)
    unknown = @($unknown)
    testGroups = [pscustomobject]$testGroups
})
