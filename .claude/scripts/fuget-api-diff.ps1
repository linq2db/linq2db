<#
fuget-api-diff.ps1 — bulk fuget API-surface diff fetcher.

For each (package id, old version, new version) tuple in the input manifest,
queries a fuget server's per-TFM API-diff endpoint for every TFM the new
version supports, parses each diff, and merges the per-TFM additions /
removals into one consolidated change-set per package.

Used by `/release-deps` step 4c (Fuget API surface diffs) and by
`/release-verify` if API drift surfaces post-build. Self-hosted fuget URL
defaults to the recorded user value in `.claude/docs/release/external-repos.md`
→ user-specific paths; override via -FugetBase. Public fuget.org also works.

Action:

  diff      Fetch + merge per-package across TFMs. Inputs:
              -ManifestFile <path>     # JSON array of { id, old, new } objects
              [-FugetBase <url>]       # default https://www.fuget.org
              [-MaxParallel <n>]       # parallel fan-out (default 8)
              [-TimeoutSec <n>]        # per-request timeout (default 90)
              [-WriteDir <path>]       # raw-HTML cache dir (default .build/.claude/fuget-<run>/)
            Output: { ok, runDir, packages: [
              { id, old, new, tfmsHit, tfmsErr, sumAdditions, sumRemovals,
                mergedAdditions, mergedRemovals }
            ] }

Conventions: `.claude/docs/script-authoring.md`.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('diff')]
    [string]$Action,

    [Parameter(Mandatory)][string]$ManifestFile,
    [string]$FugetBase   = 'https://www.fuget.org',
    [int]$MaxParallel    = 8,
    [int]$TimeoutSec     = 90,
    [string]$WriteDir
)

$global:ScriptBaseName = 'fuget-api-diff'
. (Join-Path $PSScriptRoot '_shared.ps1')

if (-not (Test-Path -LiteralPath $ManifestFile)) {
    Exit-WithError "Manifest file not found: $ManifestFile"
}

# Trim a trailing slash so we can compose URLs uniformly.
$FugetBase = $FugetBase.TrimEnd('/')

# Run-cache dir for raw HTML responses (gitignored under .build/.claude/).
if (-not $WriteDir) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $WriteDir = Join-Path (Get-Location) (Join-Path '.build/.claude' "fuget-$stamp")
}
if (-not (Test-Path -LiteralPath $WriteDir)) {
    New-Item -ItemType Directory -Path $WriteDir -Force | Out-Null
}

# Parse manifest. Accept either a JSON array of objects or a single object.
$manifestRaw = Get-Content -Raw -LiteralPath $ManifestFile
try {
    $pairs = $manifestRaw | ConvertFrom-Json
} catch {
    Exit-WithError "manifest is not valid JSON: $($_.Exception.Message)"
}
if ($pairs -isnot [System.Array]) { $pairs = @($pairs) }
foreach ($p in $pairs) {
    if (-not $p.id -or -not $p.old -or -not $p.new) {
        Exit-WithError "manifest entry missing required field id/old/new: $($p | ConvertTo-Json -Compress)"
    }
}

[Console]::Error.WriteLine("Fuget base: $FugetBase")
[Console]::Error.WriteLine("Manifest entries: $($pairs.Count)")
[Console]::Error.WriteLine("Run cache: $WriteDir")

# ---- Phase 1: discover TFMs per package ------------------------------------

$pkgInputs = @($pairs | ForEach-Object { [pscustomobject]@{ id = $_.id; old = $_.old; new = $_.new } })

$pkgPages = $pkgInputs | ForEach-Object -ThrottleLimit $MaxParallel -Parallel {
    $p = $_
    $base = $using:FugetBase
    $timeout = $using:TimeoutSec
    $writeDir = $using:WriteDir
    $url = "$base/packages/$($p.id)/$($p.new)/"
    try {
        $resp = Invoke-WebRequest -Uri $url -TimeoutSec $timeout -UseBasicParsing
        $html = [string]$resp.Content
        $cachePath = Join-Path $writeDir ("page__{0}__{1}.html" -f $p.id, $p.new)
        [System.IO.File]::WriteAllText($cachePath, $html, [System.Text.UTF8Encoding]::new($false))
        # Extract Frameworks block: <h4 ...>Frameworks</h4>...<...>tfm</a>...
        # Use a non-greedy match from the Frameworks header to the closing </nav>
        # (or end of document if missing) and pick out each ">tfm</a>" inside.
        $tfms = @()
        $start = [regex]::Match($html, 'Frameworks</h4>')
        if ($start.Success) {
            $tail = $html.Substring($start.Index + $start.Length)
            $endNav = $tail.IndexOf('</nav>')
            if ($endNav -ge 0) { $tail = $tail.Substring(0, $endNav) }
            $tfms = [regex]::Matches($tail, '>([a-z][a-z0-9.\-]+)</a>') |
                ForEach-Object { $_.Groups[1].Value } |
                Sort-Object -Unique
        }
        [pscustomobject]@{ id = $p.id; old = $p.old; new = $p.new; tfms = @($tfms); err = $null }
    } catch {
        [pscustomobject]@{ id = $p.id; old = $p.old; new = $p.new; tfms = @(); err = $_.Exception.Message }
    }
}

# ---- Phase 2: build per-(package, TFM) fetch tasks -------------------------

$tasks = @()
foreach ($p in $pkgPages) {
    if ($p.err) {
        [Console]::Error.WriteLine("[$($p.id)] page failed: $($p.err)")
        continue
    }
    if (@($p.tfms).Count -eq 0) {
        [Console]::Error.WriteLine("[$($p.id)] no TFMs discovered on $($p.new) page")
        continue
    }
    foreach ($tfm in $p.tfms) {
        $tasks += [pscustomobject]@{
            id  = $p.id
            old = $p.old
            new = $p.new
            tfm = $tfm
            url = "$FugetBase/packages/$($p.id)/$($p.new)/lib/$tfm/diff/$($p.old)/"
        }
    }
}
[Console]::Error.WriteLine("Fetching $($tasks.Count) TFM diffs across $($pkgPages.Count) packages...")

# ---- Phase 3: parallel fetch + parse ---------------------------------------

$diffs = $tasks | ForEach-Object -ThrottleLimit $MaxParallel -Parallel {
    $t = $_
    $timeout = $using:TimeoutSec
    $writeDir = $using:WriteDir
    try {
        $resp = Invoke-WebRequest -Uri $t.url -TimeoutSec $timeout -UseBasicParsing
        if ($resp.StatusCode -ne 200) {
            return [pscustomobject]@{ task = $t; ok = $false; reason = "http $($resp.StatusCode)" }
        }
        $html = [string]$resp.Content
        $cachePath = Join-Path $writeDir ("diff__{0}__{1}__{2}__to__{3}.html" -f $t.id, $t.tfm, $t.old, $t.new)
        [System.IO.File]::WriteAllText($cachePath, $html, [System.Text.UTF8Encoding]::new($false))
        # Fuget marks the diff blocks with explicit classes. Parse those, don't
        # rely on text reflow — the inner signatures are split into per-token
        # <span> elements which a flat tag-strip mangles.
        $addCount = 0; $remCount = 0
        $mAdd = [regex]::Match($html, '<strong>(\d+)</strong>\s*Additions?')
        if ($mAdd.Success) { $addCount = [int]$mAdd.Groups[1].Value }
        $mRem = [regex]::Match($html, '<strong>(\d+)</strong>\s*Removals?')
        if ($mRem.Success) { $remCount = [int]$mRem.Groups[1].Value }
        # Per-namespace blocks: <h3>NS</h3><ul class="diff-Types">...<li class="diff-X diff-Type">
        # Members inside a type block: <li class="diff-X diff-Member"> (or similar).
        # Strategy: walk each <li class="diff-(Add|Remove)[^"]*"> block, take its
        # text content as the signature, attribute it to the most recently-seen
        # <h3>...</h3> namespace header.
        $sigPattern = [regex]'<h3[^>]*>([^<]+)</h3>|<li[^>]*\bclass="[^"]*\bdiff-(Add|Remove)\b[^"]*"[^>]*>([\s\S]*?)</li>'
        $additions = @()
        $removals  = @()
        $namespace = ''
        foreach ($m in $sigPattern.Matches($html)) {
            if ($m.Groups[1].Success) {
                # h3 namespace marker
                $namespace = $m.Groups[1].Value.Trim()
                continue
            }
            $kind = $m.Groups[2].Value      # Add or Remove
            $body = $m.Groups[3].Value
            # Strip inline tags down to text. Replace tags with single space.
            $sigText = ($body -replace '<[^>]+>',' ' `
                              -replace '&nbsp;',' ' `
                              -replace '&amp;','&' `
                              -replace '&lt;','<' `
                              -replace '&gt;','>' `
                              -replace '&quot;','"' `
                              -replace '&#39;',"'" `
                              -replace '\s+',' ').Trim()
            if (-not $sigText) { continue }
            $entry = if ($namespace) { "$namespace :: $sigText" } else { $sigText }
            if ($kind -eq 'Add')    { $additions += $entry }
            elseif ($kind -eq 'Remove') { $removals  += $entry }
        }
        [pscustomobject]@{
            task      = $t
            ok        = $true
            addCount  = $addCount
            remCount  = $remCount
            additions = @($additions)
            removals  = @($removals)
        }
    } catch {
        [pscustomobject]@{ task = $t; ok = $false; reason = $_.Exception.Message }
    }
}

# ---- Phase 4: merge per-package across TFMs --------------------------------

$grouped = $diffs | Group-Object { '{0}|{1}|{2}' -f $_.task.id, $_.task.old, $_.task.new }
$result = $grouped | ForEach-Object {
    $first = $_.Group[0].task
    $okGroup  = @($_.Group | Where-Object ok)
    $errGroup = @($_.Group | Where-Object { -not $_.ok })
    $tfmsHit  = @($okGroup | ForEach-Object { $_.task.tfm }) | Sort-Object -Unique
    $tfmsErr  = @($errGroup | ForEach-Object {
        [ordered]@{ tfm = $_.task.tfm; reason = $_.reason }
    })
    $sumAdd   = ($okGroup | Measure-Object -Property addCount -Sum).Sum
    $sumRem   = ($okGroup | Measure-Object -Property remCount -Sum).Sum
    $mergedAdd = @($okGroup | ForEach-Object { $_.additions }) | Sort-Object -Unique
    $mergedRem = @($okGroup | ForEach-Object { $_.removals  }) | Sort-Object -Unique
    [pscustomobject]@{
        id              = $first.id
        old             = $first.old
        new             = $first.new
        tfmsHit         = @($tfmsHit)
        tfmsErr         = @($tfmsErr)
        sumAdditions    = $sumAdd
        sumRemovals     = $sumRem
        mergedAdditions = @($mergedAdd)
        mergedRemovals  = @($mergedRem)
    }
}

Write-JsonOutput @{
    ok       = $true
    action   = 'diff'
    runDir   = (Resolve-Path -LiteralPath $WriteDir).Path
    packages = @($result)
}
