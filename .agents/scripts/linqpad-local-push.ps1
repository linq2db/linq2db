<#
linqpad-local-push.ps1 — build + push the LINQPad local-driver nuget closure to a test feed.

Packs the LINQPad-driver dependency closure (linq2db core, Tools, Scaffold, LINQPad)
at a single <version>-local.<N> prerelease and pushes all four to a NuGet feed so
LINQPad 7+/9 can install the just-built driver. Mirrors /release-test-matrix Track 4.4;
used for LINQPad verification during release prep or feature work.

The closure MUST be pushed at the same version — LINQPad references Scaffold->Tools->core,
which are not on nuget.org at a -local version. Use `PackageVersion` (NOT `Version`, which
would propagate the prerelease suffix into AssemblyVersion and fail CS7034/CS7035).

The feed is USER-SPECIFIC — there is no default. Pass -FeedUrl (recorded in auto-memory
`user-local.nuget-server` / docs/release/external-repos.md). NuGet versions are immutable:
by default the script queries the feed for the highest existing <version>-local.<N> and
uses N+1 (the feed's read endpoints list prerelease only with semVerLevel=2.0.0).

Params:
  -FeedUrl <url>      (required) push target, e.g. https://host/nuget/nuget/
  -RepoRoot <path>    repo / worktree root to build from (default: current directory)
  -Version <ver>      base version (default: <Version> from <RepoRoot>/Directory.Build.props)
  -LocalSuffix <n>    force the -local.<n> suffix (default: max-on-feed + 1)
  -OutDir <path>      nupkg output dir (default: <RepoRoot>/.build/.agents/linqpad-nuget)
  -NoBuild            pass --no-build to every pack (fast iteration when binaries are current)
  -SkipPush           pack only, do not push

Output (stdout, single JSON): { ok, version, suffix, outDir, packages:[{id,path,pushed,status}] }
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $FeedUrl,
    [string] $RepoRoot,
    [string] $Version,
    [int]    $LocalSuffix = 0,
    [string] $OutDir,
    [switch] $NoBuild,
    [switch] $SkipPush
)

. "$PSScriptRoot/_shared.ps1"
$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) { $RepoRoot = (Get-Location).Path }
if (-not (Test-Path (Join-Path $RepoRoot 'Directory.Build.props'))) {
    Exit-WithError "no Directory.Build.props under -RepoRoot '$RepoRoot' (pass the repo/worktree root)"
}
if (-not $OutDir) { $OutDir = Join-Path $RepoRoot '.build/.agents/linqpad-nuget' }

# closure: order matters only for readability; each pack builds its own deps
$projects = [ordered]@{
    'linq2db'          = 'Source/LinqToDB/LinqToDB.csproj'
    'linq2db.Tools'    = 'Source/LinqToDB.Tools/LinqToDB.Tools.csproj'
    'linq2db.Scaffold' = 'Source/LinqToDB.Scaffold/LinqToDB.Scaffold.csproj'
    'linq2db.LINQPad'  = 'Source/LinqToDB.LINQPad/LinqToDB.LINQPad.Pack.csproj'
}

# --- base version -----------------------------------------------------------
if (-not $Version) {
    $propsText = Get-Content (Join-Path $RepoRoot 'Directory.Build.props') -Raw
    $m = [regex]::Match($propsText, '<Version>\s*([^<\s]+)\s*</Version>')
    if (-not $m.Success) { Exit-WithError 'could not read <Version> from Directory.Build.props; pass -Version' }
    $Version = $m.Groups[1].Value
}

# --- next -local.N ----------------------------------------------------------
$suffix = $LocalSuffix
if ($suffix -le 0) {
    $q = $FeedUrl.TrimEnd('/') + "/FindPackagesById()?id='linq2db.LINQPad'&semVerLevel=2.0.0"
    $max = 0
    try {
        $resp = Invoke-WebRequest $q -TimeoutSec 40 -UseBasicParsing
        foreach ($vm in [regex]::Matches($resp.Content, [regex]::Escape($Version) + '-local\.(\d+)')) {
            $n = [int]$vm.Groups[1].Value
            if ($n -gt $max) { $max = $n }
        }
    } catch {
        Write-Error "version query failed ($($_.Exception.Message)); defaulting suffix to 1 — push will 409 if taken"
    }
    $suffix = $max + 1
}

$pkgVersion = "$Version-local.$suffix"
Write-Error "Packing closure at $pkgVersion -> $OutDir (feed: $FeedUrl)"

# --- pack -------------------------------------------------------------------
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Get-ChildItem $OutDir -Filter "*.nupkg" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

$packArgs = @('-c', 'Release', "-p:PackageVersion=$pkgVersion", '-o', $OutDir, '--nologo', '-v', 'q')
if ($NoBuild) { $packArgs += '--no-build' }

foreach ($id in $projects.Keys) {
    $proj = Join-Path $RepoRoot $projects[$id]
    Write-Error "pack $id"
    & dotnet pack $proj @packArgs 2>&1 | Out-String | Write-Error
    if ($LASTEXITCODE -ne 0) { Exit-WithError "pack failed for $id ($proj)" }
}

# --- push -------------------------------------------------------------------
$results = @()
foreach ($id in $projects.Keys) {
    $path = Join-Path $OutDir "$id.$pkgVersion.nupkg"
    if (-not (Test-Path $path)) { Exit-WithError "expected package not produced: $path" }
    $entry = [ordered]@{ id = $id; path = $path; pushed = $false; status = 'packed' }
    if (-not $SkipPush) {
        $out = (& dotnet nuget push $path -s $FeedUrl 2>&1 | Out-String)
        Write-Error $out
        if ($LASTEXITCODE -eq 0)                                       { $entry.pushed = $true; $entry.status = 'created' }
        elseif ($out -match 'Conflict|already exists|409')             { $entry.status = 'conflict' }
        else                                                            { $entry.status = 'error' }
    }
    $results += [pscustomobject]$entry
}

$anyConflict = @($results | Where-Object { $_.status -eq 'conflict' }).Count -gt 0
$anyError    = @($results | Where-Object { $_.status -eq 'error' }).Count -gt 0

Write-JsonOutput ([ordered]@{
    ok       = (-not $anyError -and -not $anyConflict)
    version  = $pkgVersion
    suffix   = $suffix
    outDir   = $OutDir
    feed     = $FeedUrl
    packages = $results
    hint     = if ($anyConflict) { "version taken — re-run (auto-picks next N) or pass -LocalSuffix" } else { $null }
})
