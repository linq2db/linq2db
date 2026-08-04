<#
.SYNOPSIS
Converts XML-doc <ai-tags /> / <ai-tags-defaults /> custom tags into the internal
AiTagsAttribute/AiTagsDefaultsAttribute (Source/LinqToDB/Internal/Metadata/*.cs).

.DESCRIPTION
Mechanical, regex-based conversion. See .agents/ai-tags-attribute-design.md for the design
rationale. Processes *.cs and *.tt files under -Root that contain "<ai-tags". For each matched
line:
  - parses the XML attributes (group/groups, execution, composability, affects, pipeline,
    provider, hint-type)
  - emits the equivalent [AiTags(...)] / [AiTagsDefaults(...)] C# attribute application,
    replacing the XML-doc line in place (same indentation)
  - multi-value fields (group/groups, affects, pipeline) become bitwise-OR'd [Flags] values in
    canonical enum-declaration order, not the order the XML attribute happened to list them
  - ensures "using LinqToDB.Internal.Metadata;" is present, inserted alphabetically into the
    trailing contiguous "using LinqToDB.*;" block

*.generated.cs files are converted directly (not by re-running T4) - the corresponding *.tt
source template is converted with the same transform, so source and generated output stay
textually consistent without a T4 tooling dependency.

Run with -WhatIf first and inspect the reported diffs before applying.

.PARAMETER Root
Directory to scan. Defaults to Source/LinqToDB.

.PARAMETER WhatIf
Report what would change without writing any file.
#>
param(
	[string] $Root = "Source/LinqToDB",
	[switch] $WhatIf
)

$ErrorActionPreference = 'Stop'

$propMap = @{
	'group'         = 'Groups'
	'groups'        = 'Groups'
	'hint-type'     = 'HintType'
	'execution'     = 'Execution'
	'composability' = 'Composability'
	'affects'       = 'Affects'
	'pipeline'      = 'Pipeline'
	'provider'      = 'Provider'
}
$enumMap = @{
	'Groups'        = 'AiGroup'
	'HintType'      = 'AiHintType'
	'Execution'     = 'AiExecution'
	'Composability' = 'AiComposability'
	'Affects'       = 'AiAffects'
	'Pipeline'      = 'AiPipeline'
	'Provider'      = 'AiProvider'
}
# Emission order - matches the order used by the 7 hand-converted members in the initial spike.
$propOrder = @('Groups', 'HintType', 'Execution', 'Composability', 'Affects', 'Pipeline', 'Provider')

function ConvertAiTagLine([string] $tagName, [string] $attrsText) {
	$byKey = @{}
	foreach ($m in [regex]::Matches($attrsText, '(?<key>[\w-]+)="(?<val>[^"]*)"')) {
		$byKey[$m.Groups['key'].Value] = $m.Groups['val'].Value
	}

	if ($byKey.Count -eq 0) {
		throw "No attributes found in <$tagName $attrsText />"
	}

	$parts = New-Object System.Collections.Generic.List[string]
	foreach ($xmlKey in $byKey.Keys) {
		if (-not $propMap.ContainsKey($xmlKey)) {
			throw "Unknown <$tagName> attribute '$xmlKey'."
		}
	}

	foreach ($prop in $propOrder) {
		$xmlKeys = $propMap.GetEnumerator() | Where-Object { $_.Value -eq $prop } | ForEach-Object { $_.Key }
		$xmlKey  = $xmlKeys | Where-Object { $byKey.ContainsKey($_) } | Select-Object -First 1
		if (-not $xmlKey) { continue }

		$enum  = $enumMap[$prop]
		$vals  = $byKey[$xmlKey] -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
		if ($vals.Count -eq 0) {
			throw "Empty value for <$tagName> attribute '$xmlKey'."
		}
		$expr = ($vals | ForEach-Object { "$enum.$_" }) -join ' | '
		[void] $parts.Add("$prop = $expr")
	}

	$attrName = if ($tagName -eq 'ai-tags-defaults') { 'AiTagsDefaults' } else { 'AiTags' }
	return "[$attrName(" + ($parts -join ', ') + ")]"
}

function EnsureUsing([string] $text) {
	if ($text -match 'using LinqToDB\.Internal\.Metadata;') {
		return $text
	}

	# Find the last contiguous run of plain "using X;" lines (excluding "using static ...;")
	# anywhere before 'namespace', and re-emit that run sorted with the new line added. Scanning
	# the whole leading region (rather than walking up strictly from 'namespace') tolerates
	# "using static ...;" lines and blank/comment lines interleaved after the plain-using group,
	# a pattern some files use.
	$lines = [System.Collections.Generic.List[string]]($text -split "(?:\r\n|\n)")

	$namespaceIndex = -1
	for ($i = 0; $i -lt $lines.Count; $i++) {
		if ($lines[$i] -match '^\s*namespace\s') {
			$namespaceIndex = $i
			break
		}
	}
	if ($namespaceIndex -lt 0) {
		throw "Could not find 'namespace' declaration to anchor using-insertion."
	}

	$blockEnd = -1
	for ($i = $namespaceIndex - 1; $i -ge 0; $i--) {
		if ($lines[$i] -match '^using\s+[\w.]+;\s*$') {
			$blockEnd = $i
			break
		}
	}
	if ($blockEnd -lt 0) {
		throw "Could not locate a plain 'using X;' line above 'namespace'."
	}

	$blockStart = $blockEnd
	while ($blockStart - 1 -ge 0 -and $lines[$blockStart - 1] -match '^using\s+[\w.]+;\s*$') {
		$blockStart--
	}

	$blockLines = New-Object System.Collections.Generic.List[string]
	for ($i = $blockStart; $i -le $blockEnd; $i++) { [void] $blockLines.Add($lines[$i]) }
	[void] $blockLines.Add('using LinqToDB.Internal.Metadata;')
	$sorted = $blockLines | Sort-Object -Culture 'en-US'

	$result = New-Object System.Collections.Generic.List[string]
	for ($i = 0; $i -lt $blockStart; $i++) { [void] $result.Add($lines[$i]) }
	foreach ($l in $sorted) { [void] $result.Add($l) }
	for ($i = $blockEnd + 1; $i -lt $lines.Count; $i++) { [void] $result.Add($lines[$i]) }

	return ($result -join "`r`n")
}

function FloatAttributesPastTrailingDocs([string] $text) {
	# <ai-tags /> is not always the last XML-doc tag in its comment block (e.g. <remarks> then
	# <ai-tags /> then <returns>). A converted [AiTags(...)]/[AiTagsDefaults(...)] attribute line
	# left in that spot would split the /// block in two, which is invalid (CS1587). Move any such
	# attribute line down past the run of /// lines immediately following it, so it ends up right
	# before the next non-doc-comment line (the real declaration, or another attribute).
	$lines = $text -split "(?:\r\n|\n)"
	$out   = New-Object System.Collections.Generic.List[string]
	$i     = 0
	while ($i -lt $lines.Count) {
		$line = $lines[$i]
		if ($line -match '^[ \t]*\[Ai(?:Tags|TagsDefaults)\(.*\)\][ \t]*$') {
			$j = $i + 1
			$docRun = New-Object System.Collections.Generic.List[string]
			while ($j -lt $lines.Count -and $lines[$j].TrimStart().StartsWith('///')) {
				[void] $docRun.Add($lines[$j])
				$j++
			}
			if ($docRun.Count -gt 0) {
				foreach ($d in $docRun) { [void] $out.Add($d) }
				[void] $out.Add($line)
				$i = $j
				continue
			}
		}
		[void] $out.Add($line)
		$i++
	}
	return ($out -join "`r`n")
}

$pattern = '(?m)^([ \t]*)/// <(ai-tags(?:-defaults)?)((?:\s+[\w-]+="[^"]*")+)\s*/>[ \t]*(\r?)$'

$files = @()
$files += Get-ChildItem -Path $Root -Recurse -Filter '*.cs' | Where-Object { (Get-Content -Raw $_.FullName) -match '<ai-tags' }
$files += Get-ChildItem -Path $Root -Recurse -Filter '*.tt'  | Where-Object { (Get-Content -Raw $_.FullName) -match '<ai-tags' }

$totalLines = 0
$totalFiles = 0
$report     = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
	$text = [System.IO.File]::ReadAllText($file.FullName)
	$lineCountThisFile = 0

	$newText = [regex]::Replace($text, $pattern, {
		param($m)
		$indent   = $m.Groups[1].Value
		$tagName  = $m.Groups[2].Value
		$attrs    = $m.Groups[3].Value
		$cr       = $m.Groups[4].Value
		$script:lineCountThisFile++
		return "$indent" + (ConvertAiTagLine $tagName $attrs) + $cr
	})

	if ($lineCountThisFile -eq 0) { continue }

	$newText = FloatAttributesPastTrailingDocs $newText

	try {
		$newText = EnsureUsing $newText
	}
	catch {
		[void] $report.Add(("ERROR {0}: {1}" -f $file.FullName, $_.Exception.Message))
		continue
	}

	$totalFiles++
	$totalLines += $lineCountThisFile
	[void] $report.Add(("{0}: {1} tag(s)" -f $file.FullName.Substring((Resolve-Path .).Path.Length + 1), $lineCountThisFile))

	if (-not $WhatIf) {
		$isTt      = $file.Extension -eq '.tt'
		$hasBom    = -not $isTt
		$encoding  = New-Object System.Text.UTF8Encoding($hasBom)
		[System.IO.File]::WriteAllText($file.FullName, $newText, $encoding)
	}
}

$report | Sort-Object | ForEach-Object { Write-Output $_ }
Write-Output ''
Write-Output ("Files: {0}. Tags converted: {1}. Mode: {2}" -f $totalFiles, $totalLines, $(if ($WhatIf) { 'WhatIf (no changes written)' } else { 'Applied' }))
