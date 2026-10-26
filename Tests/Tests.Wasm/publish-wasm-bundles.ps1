<#
publish-wasm-bundles.ps1 — publish the Tests.Wasm.App bundles that the Tests.Wasm host runs under Node.

One `dotnet publish` per bundle mode, each into .build/wasm/<mode>/ (the PublishDir the app project derives
from WasmBundleMode). Each mode also has its own obj/bin (Tests.Wasm.App/Directory.Build.props), and a mode's
output directory is deleted before it is published, so no mode can run a previous publish's bundle.

  Debug             Debug,   untrimmed,        reflection-based JSON on
  TrimPartial       Release, TrimMode=partial, reflection-based JSON on
  TrimFull          Release, TrimMode=full,    reflection-based JSON on
  NoReflectionJson  Debug,   untrimmed,        reflection-based JSON off

Usage:

  pwsh -NoProfile -File Tests/Tests.Wasm/publish-wasm-bundles.ps1 [-Mode Debug,TrimFull]

Exit codes:
  0  every requested bundle published and has wwwroot/main.mjs
  1  an unknown mode, a failed publish, or an output without wwwroot/main.mjs
#>

param(
    [string[]] $Mode = @('Debug', 'TrimPartial', 'TrimFull', 'NoReflectionJson')
)

$ErrorActionPreference = 'Stop'

# `pwsh -File` passes `-Mode A,B` as one string, so split here rather than rely on [ValidateSet].
$Mode = @($Mode | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$project  = Join-Path $repoRoot 'Tests' 'Tests.Wasm.App' 'Tests.Wasm.App.csproj'

$configurations = @{
    Debug            = 'Debug'
    TrimPartial      = 'Release'
    TrimFull         = 'Release'
    NoReflectionJson = 'Debug'
}

foreach ($m in $Mode) {
    if (-not $configurations.ContainsKey($m)) {
        Write-Error "unknown mode '$m'; expected one of: $($configurations.Keys -join ', ')" -ErrorAction Continue
        exit 1
    }
}

foreach ($m in $Mode) {
    $output = Join-Path $repoRoot '.build' 'wasm' $m

    if (Test-Path $output) {
        Remove-Item -Recurse -Force $output
    }

    Write-Host "Publishing $m ($($configurations[$m])) -> $output"

    dotnet publish $project --configuration $configurations[$m] "-p:WasmBundleMode=$m"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "publish of $m failed with exit code $LASTEXITCODE" -ErrorAction Continue
        exit 1
    }

    if (-not (Test-Path (Join-Path $output 'wwwroot' 'main.mjs'))) {
        Write-Error "publish of $m produced no $(Join-Path $output 'wwwroot' 'main.mjs')" -ErrorAction Continue
        exit 1
    }
}

exit 0
