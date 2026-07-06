#!/usr/bin/env pwsh
<#
Extract the text payload from a persisted tool-result JSON into a plain file.

When a KB indexer agent's output is too large to return inline, the harness
persists it to .../tool-results/<id>.json as an array of
  { "type": "text", "text": "..." }  (plus non-text blocks)
objects. This script joins the `text` elements back into the raw envelope so it
can be fed straight to apply-fences:

  pwsh -NoProfile -File .agents/scripts/kb-state.ps1   { "op":"apply-fences", "agentOutputFile":"<OutFile>" }

One permission rule:
  Bash(pwsh -NoProfile -File .agents/scripts/extract-agent-output.ps1 *)

Inputs (named params, or -ManifestFile <json> with { sourceJson, outFile }):
  -SourceJson <path>   persisted tool-result JSON (required)
  -OutFile    <path>   destination raw-text file (required; should be under .build/.agents/)

Output (stdout, JSON):
  { "ok": true, "outFile": "...", "length": N, "textBlocks": N, "hasEnvelope": true|false }

`hasEnvelope` is a convenience check: true when the joined text contains both
`=== KB-INDEXER OUTPUT v1 ===` and `=== END KB-INDEXER OUTPUT ===`, so the caller
can confirm a usable envelope before calling apply-fences.
#>
param(
    [string]$SourceJson,
    [string]$OutFile,
    [string]$ManifestFile
)

$global:ScriptBaseName = 'extract-agent-output'
. "$PSScriptRoot/_shared.ps1"

if ($ManifestFile) {
    $m = Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile
    if (-not $SourceJson -and $m.sourceJson) { $SourceJson = [string]$m.sourceJson }
    if (-not $OutFile    -and $m.outFile)    { $OutFile    = [string]$m.outFile }
}

if (-not $SourceJson)             { Exit-WithError 'SourceJson required' }
if (-not $OutFile)                { Exit-WithError 'OutFile required' }
if (-not (Test-Path $SourceJson)) { Exit-WithError "source not found: $SourceJson" }

$parsed = $null
try { $parsed = Get-Content -Raw -LiteralPath $SourceJson | ConvertFrom-Json -Depth 100 }
catch { Exit-WithError "parse failed: $($_.Exception.Message)" }

$blocks = @($parsed | Where-Object { $_.type -eq 'text' } | ForEach-Object { [string]$_.text })
$text   = $blocks -join "`n"
[System.IO.File]::WriteAllText($OutFile, $text, [System.Text.UTF8Encoding]::new($false))

$hasEnv = ($text.IndexOf('=== KB-INDEXER OUTPUT v1 ===') -ge 0) -and ($text.IndexOf('=== END KB-INDEXER OUTPUT ===') -ge 0)

Write-JsonOutput -InputObject ([pscustomobject]@{
    ok          = $true
    outFile     = $OutFile
    length      = $text.Length
    textBlocks  = $blocks.Count
    hasEnvelope = $hasEnv
})
