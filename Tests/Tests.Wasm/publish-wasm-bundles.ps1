<#
publish-wasm-bundles.ps1 — publish the Tests.Wasm.App bundles that the Tests.Wasm host runs under Node.

One `dotnet publish` per runtime and bundle mode, each into .build/wasm/<runtime>/<mode>/ (the PublishDir the app
project derives from WasmRuntime and WasmBundleMode). Each pair also has its own obj/bin
(Tests.Wasm.App/Directory.Build.props), and every output directory is deleted first, so no bundle can run a
previous publish: a pair left out of -Runtime / -Mode is reported missing by the host. Each published bundle is
checked against the tables below.

  Debug             Debug,   untrimmed,        reflection-based JSON on
  TrimPartial       Release, TrimMode=partial, reflection-based JSON on
  TrimFull          Release, TrimMode=full,    reflection-based JSON on
  NoReflectionJson  Debug,   untrimmed,        reflection-based JSON off

  Mono              the runtime a default browser-wasm app uses
  CoreCLR           UseMonoRuntime=false

BrowserWasmAssetBuild is a global property here because the SDK adds LinqToDB to the app as a second, bare project
reference, which metadata on the app's one direct reference cannot reach. It selects the same build that is packed
as runtimes/browser-wasm/lib/net11.0.

Usage:

  pwsh -NoProfile -File Tests/Tests.Wasm/publish-wasm-bundles.ps1 [-Runtime Mono,CoreCLR] [-Mode Debug,TrimFull]

Exit codes:
  0  every requested bundle published and has wwwroot/main.mjs
  1  an unknown runtime or mode, a failed publish, an output without wwwroot/main.mjs, or a bundle whose runtime,
     trimming or reflection-based JSON differs from what was asked for
#>

param(
    [string[]] $Mode    = @('Debug', 'TrimPartial', 'TrimFull', 'NoReflectionJson'),
    [string[]] $Runtime = @('Mono', 'CoreCLR')
)

$ErrorActionPreference = 'Stop'

# `pwsh -File` passes `-Mode A,B` as one string, so split here rather than rely on [ValidateSet].
$Mode    = @($Mode    | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$Runtime = @($Runtime | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$project  = Join-Path $repoRoot 'Tests' 'Tests.Wasm.App' 'Tests.Wasm.App.csproj'

$configurations = @{
    Debug            = @{ Configuration = 'Debug';   Trimmed = $false; ReflectionJson = $true  }
    TrimPartial      = @{ Configuration = 'Release'; Trimmed = $true;  ReflectionJson = $true  }
    TrimFull         = @{ Configuration = 'Release'; Trimmed = $true;  ReflectionJson = $true  }
    NoReflectionJson = @{ Configuration = 'Debug';   Trimmed = $false; ReflectionJson = $false }
}

$runtimePacks = @{
    Mono    = 'Microsoft.NETCore.App.Runtime.Mono.browser-wasm'
    CoreCLR = 'Microsoft.NETCore.App.Runtime.browser-wasm'
}

foreach ($m in $Mode) {
    # Case-sensitive: the host reads .build/wasm/<Runtime>/<Mode> exactly as spelled here.
    if ($configurations.Keys -cnotcontains $m) {
        Write-Error "unknown mode '$m'; expected one of: $($configurations.Keys -join ', ')" -ErrorAction Continue
        exit 1
    }
}

foreach ($r in $Runtime) {
    if ($runtimePacks.Keys -cnotcontains $r) {
        Write-Error "unknown runtime '$r'; expected one of: $($runtimePacks.Keys -join ', ')" -ErrorAction Continue
        exit 1
    }
}

# A bundle left out of -Runtime / -Mode must not run an older publish; the host reports it missing instead.
foreach ($r in $runtimePacks.Keys) {
    foreach ($m in $configurations.Keys) {
        $stale = Join-Path $repoRoot '.build' 'wasm' $r $m

        if (Test-Path $stale) {
            Remove-Item -Recurse -Force $stale
        }
    }
}

foreach ($r in $Runtime) {
    foreach ($m in $Mode) {
        $bundle        = "$r/$m"
        $output        = Join-Path $repoRoot '.build' 'wasm' $r $m
        $configuration = $configurations[$m].Configuration
        $objRoot       = Join-Path $repoRoot '.build' 'obj' "Tests.Wasm.App_${r}_$m"
        # ILLink output survives in obj between publishes, so it only proves trimming once cleared first.
        $linked        = Join-Path $objRoot $configuration 'browser-wasm' 'browser-asset' 'linked'

        if (Test-Path $linked) {
            Remove-Item -Recurse -Force $linked
        }

        Write-Host "Publishing $bundle ($configuration) -> $output"

        dotnet publish $project --configuration $configuration "-p:WasmBundleMode=$m" "-p:WasmRuntime=$r" '-p:BrowserWasmAssetBuild=true'

        if ($LASTEXITCODE -ne 0) {
            Write-Error "publish of $bundle failed with exit code $LASTEXITCODE" -ErrorAction Continue
            exit 1
        }

        if (-not (Test-Path (Join-Path $output 'wwwroot' 'main.mjs'))) {
            Write-Error "publish of $bundle produced no $(Join-Path $output 'wwwroot' 'main.mjs')" -ErrorAction Continue
            exit 1
        }

        # The published files look alike on both runtimes; the restore records which runtime pack was used.
        $assets   = Join-Path $objRoot 'project.assets.json'
        $expected = $runtimePacks[$r]

        if (-not (Select-String -Path $assets -Pattern ('"name"\s*:\s*"' + [regex]::Escape($expected) + '"') -Quiet)) {
            Write-Error "publish of $bundle did not use $expected ($assets)" -ErrorAction Continue
            exit 1
        }

        # Debug and TrimPartial give the same test results, so only the bundle itself shows which mode it is.
        $trimmed = Test-Path $linked

        if ($trimmed -ne $configurations[$m].Trimmed) {
            Write-Error "publish of $bundle is $(if ($trimmed) { 'trimmed' } else { 'untrimmed' }), expected the opposite ($linked)" -ErrorAction Continue
            exit 1
        }

        $runtimeConfig  = Get-Content (Join-Path $output 'linq2db.Tests.Wasm.App.runtimeconfig.json') -Raw | ConvertFrom-Json -AsHashtable
        $reflectionJson = $runtimeConfig.runtimeOptions.configProperties['System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault']

        if ($reflectionJson -ne $configurations[$m].ReflectionJson) {
            Write-Error "publish of $bundle has reflection-based JSON '$reflectionJson', expected '$($configurations[$m].ReflectionJson)'" -ErrorAction Continue
            exit 1
        }
    }
}

exit 0
