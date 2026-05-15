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

    Bash(pwsh -NoProfile -File .claude/scripts/baselines-diff.ps1 *)

Input — two forms (preferred first)
-----------------------------------

(1) Named parameters (preferred — single allowlist-friendly command line):

      pwsh -NoProfile -File .claude/scripts/baselines-diff.ps1 -Pr 5414

    Optional named parameters:
      -BaselinesPath <path>     — default "../linq2db.baselines"
      -Branch <name>            — default "baselines/pr_<pr>"
      -BaseRef <ref>            — default "origin/master"
      -MaxDiffBytes <int>       — per-file diff truncation; 0 = no limit
      -Fetch                    — switch; fetch the baselines branch before diffing
      -BaselineOwner <name>     — owner of baselines repo on GitHub
      -BaselineRepo <name>      — baselines repo name on GitHub

(2) Stdin JSON (legacy, still accepted — heredoc form). JSON manifest shape:

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
counts, summary, testGroupSummary[], changePatterns[], sizeOutliers[],
regressionCandidates[], sql[], metrics[], unknown[], testGroups }. When
status == "branch_missing", only the header fields plus an all-zero
`counts` are emitted; the rest are omitted.
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
  `sizeOutliers` — top-20 patterns ranked by absolute net byte delta. Surfaces
    tests whose PR shape grew (or shrank) dramatically vs master, including
    single-provider rare patterns the providerCount-sorted main list hides.
    Threshold: include a pattern if `|netDelta| >= 200` bytes OR an extreme
    `growthRatio` — `>= 3.0` for growth, or `<= 0.33` for shrink (the latter
    only when `removedBytes > 0`, so single-line additions don't trivially
    flag). Each entry mirrors the `changePatterns[]` row plus `netDelta`,
    `addedBytes`, `removedBytes`, `growthRatio`, `addedLines`, `removedLines`,
    `regressionArchetypes`.
  `regressionCandidates` — every pattern where the heuristic archetype scan
    flagged at least one suspect shape on the + side that doesn't appear on
    the - side. Archetypes:
      `nested-<func>` — LCase/Lower/Coalesce/Trim wrap around itself.
      `literal-is-null` — testing a SqlValue literal for NULL.
      `subtract-vs-zero` — `x - n <op> 0` algebraic identity not folded.
      `concat-is-null-not-pushed-down` — `(col + literal) IS NULL` not pushed
        to `col IS NULL`.
      `new-case-when-is-null` — null-propagation CASE scaffold bolted on.
      `coalesce-around-literal-fn` — Coalesce wrap around a function call
        whose args are all numeric/string literals (typically annotation-
        driven, validate with user rather than auto-suspect).
    The reviewer must classify each — suspect or expected-with-rationale —
    before returning. No cap; the list is the worklist.
  Each `changePatterns[]` entry also carries `sizeMetrics` (the per-pattern
    byte/line deltas) and `regressionArchetypes` (string array of archetype
    names, empty when none fired).

Exit codes
----------
  0 = success
  1 = hard failure
#>

param(
    [int]$Pr = 0,
    [string]$BaselinesPath,
    [string]$Branch,
    [string]$BaseRef,
    [int]$MaxDiffBytes = -1,
    [switch]$Fetch,
    [string]$BaselineOwner,
    [string]$BaselineRepo,
    [string]$ManifestFile
)

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
    $totalBytes = [System.Text.Encoding]::UTF8.GetByteCount($body)
    if ($totalBytes -le $limit) { return @{ body = $body; truncated = $false } }
    $cut = Get-Utf8SafeTruncate -Text $body -MaxBytes $limit
    return @{
        body = "$cut`n… [truncated; total $totalBytes bytes, showing first $limit]"
        truncated = $true
    }
}

# Split a unified diff body into added / removed line text (drops @@ headers
# and context lines). Used by the size-metric and archetype detectors below.
function Split-DiffSides {
    param([string]$Diff)
    $added = New-Object System.Collections.Generic.List[string]
    $removed = New-Object System.Collections.Generic.List[string]
    if (-not $Diff) { return @{ added = ''; removed = '' } }
    foreach ($line in ($Diff -split "`n")) {
        if ($line.Length -eq 0) { continue }
        $c = $line[0]
        if ($c -eq '+' -and -not $line.StartsWith('+++')) {
            $added.Add($line.Substring(1)) | Out-Null
        } elseif ($c -eq '-' -and -not $line.StartsWith('---')) {
            $removed.Add($line.Substring(1)) | Out-Null
        }
    }
    return @{
        added       = ($added -join "`n")
        removed     = ($removed -join "`n")
        addedLines  = $added.Count
        removedLines = $removed.Count
    }
}

# Per-pattern size deltas. Net byte / line growth from master to PR. Used to
# rank `sizeOutliers[]` so single-test SQL blowups (e.g. 1 line → nested CASE
# scaffold) surface even when only one provider is affected.
function Get-PatternSizeMetrics {
    param([hashtable]$Sides)
    $addedBytes = [System.Text.Encoding]::UTF8.GetByteCount([string]$Sides.added)
    $removedBytes = [System.Text.Encoding]::UTF8.GetByteCount([string]$Sides.removed)
    $net = $addedBytes - $removedBytes
    $ratio = if ($removedBytes -gt 0) { [math]::Round([double]$addedBytes / [double]$removedBytes, 2) } else { [double]$addedBytes }
    return [ordered]@{
        addedBytes   = $addedBytes
        removedBytes = $removedBytes
        netDelta     = $net
        growthRatio  = $ratio
        addedLines   = [int]$Sides.addedLines
        removedLines = [int]$Sides.removedLines
    }
}

# Heuristic scan for SQL-shape regression archetypes that hide inside an
# otherwise-expected diff. Each archetype that fires returns one entry with
# `name` (machine label) and `snippet` (short evidence cut from the +/- side).
# The reviewer agent treats every flagged pattern as a must-classify item —
# either move it to `changed_suspect`, or carry an explicit per-entry note
# explaining why the archetype is correct in this PR's context.
#
# Conservative on purpose: each archetype requires the pattern to appear on
# the + side AND be absent (or strictly less prevalent) on the - side, so
# pure stylistic shuffles don't trip the detectors.
function Get-RegressionArchetypes {
    param([hashtable]$Sides)
    $added = [string]$Sides.added
    $removed = [string]$Sides.removed
    if (-not $added) { return @() }

    $hits = New-Object System.Collections.Generic.List[object]

    # 1. Nested same-function calls — `LCase(LCase(...))`, `Lower(...Lower(...))`,
    #    `Coalesce(Coalesce(...))` etc. Function-call wraps are idempotent for
    #    these and should collapse at the optimizer.
    #
    #    Pattern requires the inner open to appear before any closing paren of
    #    the outer call, so sibling calls like `Coalesce(a, '') + Coalesce(b, '')`
    #    don't fire. `[^)]` is a coarse approximation — a nested subexpression
    #    `Foo(x)` between the two opens would break the match, accepting the
    #    occasional false negative in exchange for killing the dominant false
    #    positive class (sibling sequential calls).
    $nestFuncs = @('Lower','LCase','Upper','UCase','Coalesce','Trim','LTrim','RTrim')
    foreach ($fn in $nestFuncs) {
        $pattern = "(?i)\b$fn\s*\([^)]{0,200}\b$fn\s*\("
        $addedHits = [regex]::Matches($added, $pattern).Count
        $removedHits = [regex]::Matches($removed, $pattern).Count
        if ($addedHits -gt $removedHits) {
            $m = [regex]::Match($added, $pattern)
            $hits.Add([ordered]@{
                name = "nested-$($fn.ToLower())"
                snippet = Get-Utf8SafeTruncate -Text ($m.Value) -MaxBytes 180
            }) | Out-Null
            break
        }
    }

    # 2. Literal IS NULL — testing a SqlValue string literal against NULL.
    #    Statically false / true; the optimizer should fold it. Catches the
    #    Sybase concat fallback that wraps every operand in IS NULL including
    #    compile-time constants.
    $litPattern = "(?i)\b[N]?'[^']*'\s+IS\s+NULL"
    $litAdded = [regex]::Matches($added, $litPattern).Count
    $litRemoved = [regex]::Matches($removed, $litPattern).Count
    if ($litAdded -gt $litRemoved) {
        $m = [regex]::Match($added, $litPattern)
        $hits.Add([ordered]@{
            name = 'literal-is-null'
            snippet = Get-Utf8SafeTruncate -Text ($m.Value) -MaxBytes 180
        }) | Out-Null
    }

    # 3. Subtraction-against-zero — `x - n <op> 0` where master had `x <op> n`.
    #    Algebraic identity not folded. SqlServer `LEN(... + '.') - 1 <> 0`
    #    case (#5529).
    $subPattern = '-\s*\d+\s*(?:<>|<=|>=|=|<|>)\s*0\b'
    if (($added -match $subPattern) -and -not ($removed -match $subPattern)) {
        $m = [regex]::Match($added, '\S+\s*' + $subPattern)
        $snippet = if ($m.Success) { $m.Value } else { '- N <op> 0' }
        $hits.Add([ordered]@{
            name = 'subtract-vs-zero'
            snippet = Get-Utf8SafeTruncate -Text $snippet -MaxBytes 180
        }) | Out-Null
    }

    # 4. Concat IS NULL — `(col + literal) IS NULL` where master had
    #    `col IS NULL`. SqlServer `+` is null-propagating so the literal-side
    #    of the concat doesn't change the IS NULL semantics; optimizer should
    #    push IS NULL down to the column. SqlServer LengthTest1 case (#5529).
    $concatNullPattern = '\([^()]{1,80}\+[^()]{1,80}\)\s+IS\s+NULL'
    if (($added -match $concatNullPattern) -and -not ($removed -match $concatNullPattern)) {
        $m = [regex]::Match($added, $concatNullPattern)
        $hits.Add([ordered]@{
            name = 'concat-is-null-not-pushed-down'
            snippet = Get-Utf8SafeTruncate -Text ($m.Value) -MaxBytes 180
        }) | Out-Null
    }

    # 5. New `CASE WHEN ... IS NULL ... THEN NULL` scaffolds that don't
    #    appear on the - side. Catches null-propagation fallbacks bolted on
    #    in the PR (Sybase MakeDateTime case, #5530).
    $casePattern = '(?i)CASE\s+WHEN[\s\S]{0,200}\bIS\s+NULL\b[\s\S]{0,400}\bTHEN\s+NULL\b'
    $caseAdded = [regex]::Matches($added, $casePattern).Count
    $caseRemoved = [regex]::Matches($removed, $casePattern).Count
    if ($caseAdded -gt $caseRemoved) {
        $m = [regex]::Match($added, $casePattern)
        $hits.Add([ordered]@{
            name = 'new-case-when-is-null'
            snippet = Get-Utf8SafeTruncate -Text ($m.Value) -MaxBytes 180
        }) | Out-Null
    }

    # 6. `Coalesce(<fn(only-literal-args)>, '<literal>')` — Coalesce wrap
    #    added around a function call whose arguments are all numeric or
    #    string literals (no column reference). Typically expected — it's a
    #    consequence of the function's nullability annotation (e.g.
    #    `SqlFn.Space(int?)` declared `string?` for the negative-arg edge
    #    case) and static null analysis can't see that the actual call
    #    site uses a positive literal. The reviewer should surface this for
    #    user validation rather than auto-classify as suspect; if the
    #    annotation is genuinely too loose it's a separate annotation-bug
    #    discussion, not a translator bug. See SpaceTest in PR #5504.
    $coalLitFnPattern = "(?i)\bCoalesce\s*\(\s*\b\w+\s*\(\s*(?:\d+|[N]?'[^']*')(?:\s*,\s*(?:\d+|[N]?'[^']*'))*\s*\)\s*,\s*[N]?'[^']*'\s*\)"
    $coalLitFnAdded = [regex]::Matches($added, $coalLitFnPattern).Count
    $coalLitFnRemoved = [regex]::Matches($removed, $coalLitFnPattern).Count
    if ($coalLitFnAdded -gt $coalLitFnRemoved) {
        $m = [regex]::Match($added, $coalLitFnPattern)
        $hits.Add([ordered]@{
            name = 'coalesce-around-literal-fn'
            snippet = Get-Utf8SafeTruncate -Text ($m.Value) -MaxBytes 180
        }) | Out-Null
    }

    return ,$hits.ToArray()
}

$m = if ($Pr -gt 0) {
    [PSCustomObject]@{
        pr             = $Pr
        baselinesPath  = $BaselinesPath
        branch         = $Branch
        baseRef        = $BaseRef
        maxDiffBytes   = if ($MaxDiffBytes -ge 0) { $MaxDiffBytes } else { $null }
        fetch          = $Fetch.IsPresent
        baselineOwner  = $BaselineOwner
        baselineRepo   = $BaselineRepo
    }
} else {
    Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile
}

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
            rawDiff = $rawDiff
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
            sampleRawDiff = $s.rawDiff
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
    # Compute size/archetype metrics from the **raw** (pre-truncation) diff
    # so large diffs whose body got clipped to `maxDiffBytes` still produce
    # accurate byte counts and archetype matches — the truncated `sampleDiff`
    # is only the emitted text. Without this, sizeOutliers[]/regressionCandidates[]
    # would under-count exactly the cases they're meant to surface.
    $sides = Split-DiffSides -Diff $val.sampleRawDiff
    $sizeMetrics = Get-PatternSizeMetrics -Sides $sides
    $archetypes = Get-RegressionArchetypes -Sides $sides
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
        sizeMetrics = [pscustomobject]$sizeMetrics
        regressionArchetypes = @($archetypes | ForEach-Object { $_.name })
    }
}
$changePatterns = @($changePatterns | Sort-Object -Property @{ Expression = 'providerCount'; Descending = $true }, @{ Expression = 'testBase'; Descending = $false })

# `sizeOutliers[]` — top N patterns by absolute net byte delta. Surfaces the
# tests where PR shape is dramatically larger or smaller than master, even
# when only one provider is affected (rare-pattern outlier). Threshold:
# include a pattern if `|netDelta| >= 200` bytes OR growth/shrink ratio is
# >= 3.0 or <= 0.33, capped at 20 entries. Sort key is the absolute delta
# so big shrinks rank alongside big growths.
$sizeOutliers = @()
foreach ($p in $changePatterns) {
    $sm = $p.sizeMetrics
    if (-not $sm) { continue }
    $net = [int]$sm.netDelta
    $absNet = [math]::Abs($net)
    $ratio = [double]$sm.growthRatio
    $extremeRatio = ($sm.removedBytes -gt 0 -and ($ratio -ge 3.0 -or ($ratio -gt 0 -and $ratio -le 0.33)))
    if ($absNet -ge 200 -or $extremeRatio) {
        $sizeOutliers += [pscustomobject]@{
            testBase = $p.testBase
            patternHash = $p.patternHash
            providerCount = $p.providerCount
            sampleProvider = $p.sampleProvider
            samplePath = $p.samplePath
            sampleUrl = $p.sampleUrl
            sampleStatus = $p.sampleStatus
            netDelta = $net
            absNetDelta = $absNet
            addedBytes = [int]$sm.addedBytes
            removedBytes = [int]$sm.removedBytes
            growthRatio = $ratio
            addedLines = [int]$sm.addedLines
            removedLines = [int]$sm.removedLines
            regressionArchetypes = $p.regressionArchetypes
        }
    }
}
$sizeOutliers = @($sizeOutliers | Sort-Object -Property @{ Expression = 'absNetDelta'; Descending = $true }, @{ Expression = 'growthRatio'; Descending = $true } | Select-Object -First 20)

# `regressionCandidates[]` — every pattern where any archetype fired. The
# agent MUST classify each (suspect or expected-with-rationale) before
# returning. No cap — the list is the worklist.
$regressionCandidates = @()
foreach ($p in $changePatterns) {
    if (-not $p.regressionArchetypes -or $p.regressionArchetypes.Count -eq 0) { continue }
    $regressionCandidates += [pscustomobject]@{
        testBase = $p.testBase
        patternHash = $p.patternHash
        providerCount = $p.providerCount
        providers = $p.providers
        sampleProvider = $p.sampleProvider
        samplePath = $p.samplePath
        sampleUrl = $p.sampleUrl
        sampleStatus = $p.sampleStatus
        sampleDiff = $p.sampleDiff
        sampleDiffTruncated = $p.sampleDiffTruncated
        archetypes = $p.regressionArchetypes
        sizeMetrics = $p.sizeMetrics
    }
}

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
    sizeOutliers = @($sizeOutliers)
    regressionCandidates = @($regressionCandidates)
    # Strip `rawDiff` from emitted sql entries — it's the unbounded pre-
    # truncation body kept around for accurate size/archetype metrics on
    # `changePatterns[]`, but emitting it would bypass the `maxDiffBytes`
    # budget and bloat stdout on baseline-heavy PRs. Truncated `diff` stays.
    sql = @($sql | ForEach-Object { $_ | Select-Object * -ExcludeProperty rawDiff })
    metrics = @($metrics)
    unknown = @($unknown)
    testGroups = [pscustomobject]$testGroups
})
