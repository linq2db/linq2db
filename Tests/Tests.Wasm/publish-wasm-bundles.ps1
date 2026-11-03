<#
publish-wasm-bundles.ps1 — publish the Tests.Wasm.App bundles that the Tests.Wasm host runs under Node.

One `dotnet publish` per bundle mode, each into .build/wasm/<mode>/ (the PublishDir the app project derives
from WasmBundleMode). Each mode also has its own obj/bin (Tests.Wasm.App/Directory.Build.props), and every
mode's output directory is deleted first, so no mode can run a previous publish's bundle: a mode left out of
-Mode is reported missing by the host. Each published bundle is checked against the table below.

  Debug             Debug,   untrimmed,        reflection-based JSON on
  TrimPartial       Release, TrimMode=partial, reflection-based JSON on
  TrimFull          Release, TrimMode=full,    reflection-based JSON on
  NoReflectionJson  Debug,   untrimmed,        reflection-based JSON off

Usage:

  pwsh -NoProfile -File Tests/Tests.Wasm/publish-wasm-bundles.ps1 [-Mode Debug,TrimFull]

Exit codes:
  0  every requested bundle published and has wwwroot/main.mjs
  1  an unknown mode, a failed publish, an output without wwwroot/main.mjs, or a bundle whose trimming or
     reflection-based JSON differs from its mode
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
    Debug            = @{ Configuration = 'Debug';   Trimmed = $false; ReflectionJson = $true  }
    TrimPartial      = @{ Configuration = 'Release'; Trimmed = $true;  ReflectionJson = $true  }
    TrimFull         = @{ Configuration = 'Release'; Trimmed = $true;  ReflectionJson = $true  }
    NoReflectionJson = @{ Configuration = 'Debug';   Trimmed = $false; ReflectionJson = $false }
}

foreach ($m in $Mode) {
    # Case-sensitive: the host reads .build/wasm/<Mode> exactly as spelled here.
    if ($configurations.Keys -cnotcontains $m) {
        Write-Error "unknown mode '$m'; expected one of: $($configurations.Keys -join ', ')" -ErrorAction Continue
        exit 1
    }
}

# A mode left out of -Mode must not run an older publish's bundle; the host reports it missing instead.
foreach ($m in $configurations.Keys) {
    $stale = Join-Path $repoRoot '.build' 'wasm' $m

    if (Test-Path $stale) {
        Remove-Item -Recurse -Force $stale
    }
}

foreach ($m in $Mode) {
    $output        = Join-Path $repoRoot '.build' 'wasm' $m
    $configuration = $configurations[$m].Configuration
    # ILLink output survives in obj between publishes, so it only proves trimming once cleared first.
    $linked        = Join-Path $repoRoot '.build' 'obj' "Tests.Wasm.App_$m" $configuration 'browser-wasm' 'linked'

    if (Test-Path $linked) {
        Remove-Item -Recurse -Force $linked
    }

    Write-Host "Publishing $m ($configuration) -> $output"

    dotnet publish $project --configuration $configuration "-p:WasmBundleMode=$m"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "publish of $m failed with exit code $LASTEXITCODE" -ErrorAction Continue
        exit 1
    }

    if (-not (Test-Path (Join-Path $output 'wwwroot' 'main.mjs'))) {
        Write-Error "publish of $m produced no $(Join-Path $output 'wwwroot' 'main.mjs')" -ErrorAction Continue
        exit 1
    }

    # Debug and TrimPartial give the same test results, so only the bundle itself shows which mode it is.
    $trimmed = Test-Path $linked

    if ($trimmed -ne $configurations[$m].Trimmed) {
        Write-Error "publish of $m is $(if ($trimmed) { 'trimmed' } else { 'untrimmed' }), expected the opposite ($linked)" -ErrorAction Continue
        exit 1
    }

    $runtimeConfig  = Get-Content (Join-Path $output 'linq2db.Tests.Wasm.App.runtimeconfig.json') -Raw | ConvertFrom-Json -AsHashtable
    $reflectionJson = $runtimeConfig.runtimeOptions.configProperties['System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault']

    if ($reflectionJson -ne $configurations[$m].ReflectionJson) {
        Write-Error "publish of $m has reflection-based JSON '$reflectionJson', expected '$($configurations[$m].ReflectionJson)'" -ErrorAction Continue
        exit 1
    }
}

exit 0
