param(
	[string] $RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path,
	[string] $OutputRoot = 'Source\Knowledge\linq2db-expert',
	[string] $Configuration = 'Release',
	[string] $XmlDocPath = '',
	[switch] $NoBuild,
	[switch] $NoRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$sourceRoot = Join-Path $RepoRoot 'Source\Skills\linq2db'
$packageReadmePath = Join-Path $RepoRoot 'Source\LinqToDB\README.md'
$maintenancePath = Join-Path $RepoRoot '.agents\knowledge-pack-maintenance.md'
if (-not [System.IO.Path]::IsPathRooted($OutputRoot)) {
	$OutputRoot = Join-Path $RepoRoot $OutputRoot
}

function Write-Utf8CrLfFile([string] $Path, [string] $Text) {
	$normalized = $Text -replace "`r?`n", "`r`n"
	if (-not $normalized.EndsWith("`r`n")) {
		$normalized += "`r`n"
	}

	$parent = Split-Path -Parent $Path
	if ($parent -and -not (Test-Path -LiteralPath $parent)) {
		New-Item -ItemType Directory -Path $parent | Out-Null
	}

	[System.IO.File]::WriteAllText($Path, $normalized, $script:utf8NoBom)
}

function Read-Utf8File([string] $Path) {
	return [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false, $true))
}

function Clean-Text([string] $Text) {
	if ([string]::IsNullOrWhiteSpace($Text)) {
		return ''
	}

	return ([System.Text.RegularExpressions.Regex]::Replace($Text, '\s+', ' ')).Trim()
}

function Get-Kind([string] $Id) {
	switch ($Id.Substring(0, 1)) {
		'T' { return 'Type' }
		'M' { return 'Method' }
		'P' { return 'Property' }
		'F' { return 'Field' }
		'E' { return 'Event' }
		default { return 'Member' }
	}
}

function Get-GenericSuffix([int] $Count) {
	if ($Count -le 0) {
		return ''
	}
	if ($Count -eq 1) {
		return '<T>'
	}

	$names = for ($i = 1; $i -le $Count; $i++) { "T$i" }
	return '<' + ($names -join ',') + '>'
}

function Get-DisplayName([string] $Id) {
	$body = $Id.Substring(2)
	$body = [System.Text.RegularExpressions.Regex]::Replace($body, '\(.*$', '')
	$body = $body.Replace('#ctor', 'Constructor')

	$body = [System.Text.RegularExpressions.Regex]::Replace(
		$body,
		'``(\d+)',
		{
			param($m)
			return Get-GenericSuffix ([int]$m.Groups[1].Value)
		})

	$body = [System.Text.RegularExpressions.Regex]::Replace(
		$body,
		'`(\d+)',
		{
			param($m)
			return Get-GenericSuffix ([int]$m.Groups[1].Value)
		})

	return $body
}

function Extract-AiTags($Member) {
	$text = ''

	$remarksNode = $Member.SelectSingleNode('remarks')
	if ($remarksNode) {
		$text = $remarksNode.InnerText
	}
	if ([string]::IsNullOrWhiteSpace($text)) {
		$text = $Member.InnerText
	}

	$match = [System.Text.RegularExpressions.Regex]::Match($text, 'AI-Tags:\s*([^\r\n]+)')
	if (-not $match.Success) {
		return ''
	}

	return Clean-Text $match.Groups[1].Value
}

function Get-XmlChildText($Node, [string] $Name) {
	$child = $Node.SelectSingleNode($Name)
	if ($child) {
		return $child.InnerText
	}

	return ''
}

function Normalize-RelativePath([string] $Path) {
	$value = $Path.Replace('\', '/').Trim()
	while ($value.StartsWith('./')) {
		$value = $value.Substring(2)
	}

	$parts = New-Object System.Collections.Generic.List[string]
	foreach ($part in $value.Split('/')) {
		if ([string]::IsNullOrWhiteSpace($part) -or $part -eq '.') {
			continue
		}
		if ($part -eq '..') {
			if ($parts.Count -gt 0) {
				$parts.RemoveAt($parts.Count - 1)
			}
			continue
		}
		$parts.Add($part)
	}

	return ($parts -join '/')
}

function Join-RelativePath([string] $BaseFile, [string] $Target) {
	$baseDir = Split-Path -Parent ($BaseFile.Replace('\', '/'))
	if ([string]::IsNullOrWhiteSpace($baseDir)) {
		return Normalize-RelativePath $Target
	}

	return Normalize-RelativePath ($baseDir + '/' + $Target)
}

function Get-RepoRelativePath([string] $Path) {
	$root = (Resolve-Path -LiteralPath $RepoRoot).Path.TrimEnd('\')
	$resolved = (Resolve-Path -LiteralPath $Path).Path
	if ($resolved.StartsWith($root + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
		return $resolved.Substring($root.Length + 1).Replace('\', '/')
	}

	return $resolved.Replace('\', '/')
}

$sourceToOutput = @{
	'SKILL.md'                           = '01-skill.md'
	'LinqToDB/README.md'                 = '03-overview-readme.md'
	'../../LinqToDB/README.md'           = '03-overview-readme.md'
	'docs/coverage.md'                 = '02-coverage.md'
	'docs/api.md'                        = '04-api-discovery-and-extract.md'
	'docs/architecture.md'               = '05-architecture.md'
	'docs/agent-antipatterns.md'         = '06-agent-antipatterns-and-ai-tags.md'
	'docs/ai-tags.md'                    = '06-agent-antipatterns-and-ai-tags.md'
	'docs/provider-capabilities.md'      = '07-provider-configuration.md'
	'docs/provider-setup.md'             = '07-provider-configuration.md'
	'docs/configuration.md'              = '07-provider-configuration.md'
	'docs/mapping.md'                    = '08-mapping.md'
	'docs/crud/crud.md'                  = '09-crud-and-merge.md'
	'docs/crud/crud-select.md'           = '09-crud-and-merge.md'
	'docs/crud/crud-insert.md'           = '09-crud-and-merge.md'
	'docs/crud/crud-insert-values.md'    = '09-crud-and-merge.md'
	'docs/crud/crud-insert-select.md'    = '09-crud-and-merge.md'
	'docs/crud/crud-upsert.md'           = '09-crud-and-merge.md'
	'docs/crud/crud-update.md'           = '09-crud-and-merge.md'
	'docs/crud/crud-delete.md'           = '09-crud-and-merge.md'
	'docs/crud/crud-bulkcopy.md'         = '09-crud-and-merge.md'
	'docs/crud/crud-merge.md'            = '09-crud-and-merge.md'
	'docs/query-cte.md'                  = '10-query-composition.md'
	'docs/query-temp-tables.md'          = '10-query-composition.md'
	'docs/hints.md'                      = '11-hints.md'
	'docs/hints-api-map.md'              = '12-hints-api-map.md'
	'docs/custom-sql.md'                 = '13-custom-sql.md'
	'docs/translatable-methods.md'       = '14-translatable-methods.md'
	'docs/interceptors.md'               = '15-interceptors.md'
}

function Resolve-LinkTarget([string] $CurrentSourceRel, [string] $Href) {
	$target = $Href.Trim()
	$title = ''

	$titleMatch = [System.Text.RegularExpressions.Regex]::Match($target, '^(?<href><[^>]+>|[^\s]+)(?<title>\s+.+)$')
	if ($titleMatch.Success) {
		$target = $titleMatch.Groups['href'].Value
		$title = $titleMatch.Groups['title'].Value
	}

	$wrapped = $target.StartsWith('<') -and $target.EndsWith('>')
	if ($wrapped) {
		$target = $target.Substring(1, $target.Length - 2)
	}

	if ($target.StartsWith('#')) {
		return $Href
	}

	if ($target -match '^(mailto:|https?://)') {
		if ($target -match '^https://linq2db\.github\.io/api/') {
			return '16-xml-doc.md' + $title
		}
		if ($target -match '^https://github\.com/linq2db/linq2db/blob/master/Source/LinqToDB/(.+)$') {
			$target = $matches[1]
		}
		elseif ($target -match '^https://github\.com/linq2db/linq2db/blob/master/docs/(.+)$') {
			$target = 'docs/' + $matches[1]
		}
		else {
			return $Href
		}
	}

	$anchor = ''
	$hashIndex = $target.IndexOf('#')
	if ($hashIndex -ge 0) {
		$anchor = $target.Substring($hashIndex)
		$target = $target.Substring(0, $hashIndex)
	}

	if ($target -match 'linq2db\.xml$' -or $target -match '^lib/.+/linq2db\.xml$') {
		return '16-xml-doc.md' + $anchor + $title
	}

	$normalized = Join-RelativePath $CurrentSourceRel $target
	$normalized = [System.Text.RegularExpressions.Regex]::Replace($normalized, '^Source/LinqToDB/', '')
	$normalized = [System.Text.RegularExpressions.Regex]::Replace($normalized, '^LinqToDB/docs/', 'docs/')
	$normalized = [System.Text.RegularExpressions.Regex]::Replace($normalized, '^LinqToDB/README\.md$', '../../LinqToDB/README.md')

	if ($script:sourceToOutput.ContainsKey($normalized)) {
		return $script:sourceToOutput[$normalized] + $anchor + $title
	}

	return $Href
}

function Convert-MarkdownLinks([string] $Text, [string] $CurrentSourceRel) {
	$lines = $Text -split "`r?`n", -1
	$inFence = $false
	$out = New-Object System.Collections.Generic.List[string]

	foreach ($line in $lines) {
		if ($line -match '^\s*(```|~~~)') {
			$out.Add($line)
			$inFence = -not $inFence
			continue
		}

		if ($inFence) {
			$out.Add($line)
			continue
		}

		$out.Add([System.Text.RegularExpressions.Regex]::Replace(
			$line,
			'(?<!!)\[((?:[^\]]|`[^`]*`)*)\]\(([^)]+)\)',
			{
				param($m)
				$label = $m.Groups[1].Value
				$href = $m.Groups[2].Value
				$newHref = Resolve-LinkTarget $CurrentSourceRel $href
				return '[' + $label + '](' + $newHref + ')'
			}))
	}

	return ($out -join "`r`n")
}

function Convert-SourceMarkdown([string] $SourceRel) {
	$path = Join-Path $sourceRoot ($SourceRel.Replace('/', '\'))
	if (-not (Test-Path -LiteralPath $path)) {
		throw "Source file not found: $path"
	}

	$text = Read-Utf8File $path
	$text = Convert-MarkdownLinks $text $SourceRel
	$text = [System.Text.RegularExpressions.Regex]::Replace($text.Trim(), "(?m)^(---\r?\n\s*){3,}", "---`r`n")
	return "<!-- Generated from: $(Get-RepoRelativePath $path) -->`r`n`r`n$text"
}

function New-MarkdownBundle([string[]] $Sources) {
	$parts = foreach ($source in $Sources) {
		Convert-SourceMarkdown $source
	}

	return ($parts -join "`r`n`r`n")
}

function New-XmlDocMarkdown([string] $Path) {
	if (-not (Test-Path -LiteralPath $Path)) {
		throw "XML documentation file not found: $Path"
	}

	[xml] $xml = Read-Utf8File $Path
	$relativeXml = Get-RepoRelativePath $Path
	$lines = New-Object System.Collections.Generic.List[string]
	$lines.Add("<!-- Generated from: $relativeXml -->")
	$lines.Add('')
	$lines.Add('# linq2db XML Documentation Extract')
	$lines.Add('')
	$lines.Add('Generated directly from the current package XML documentation. Use XML member ids for exact API lookup.')
	$lines.Add('')

	$members = @($xml.doc.members.member | Where-Object { ([string]$_.name) -match '^[A-Z]:LinqToDB\.' } | Sort-Object { [string]$_.name })

	foreach ($member in $members) {
		$id = [string]$member.name
		$lines.Add(('## {0}' -f (Get-DisplayName $id)))
		$lines.Add('')
		$lines.Add(('- XML member: `{0}`' -f $id))
		$lines.Add(('- Kind: {0}' -f (Get-Kind $id)))

		$summary = Clean-Text (Get-XmlChildText $member 'summary')
		if ($summary) {
			$lines.Add(('- Summary: {0}' -f $summary))
		}

		$remarks = Clean-Text (Get-XmlChildText $member 'remarks')
		if ($remarks) {
			$lines.Add(('- Remarks: {0}' -f $remarks))
		}

		$typeParams = @($member.SelectNodes('typeparam'))
		if ($typeParams.Count -gt 0) {
			$lines.Add('- Type parameters:')
			foreach ($param in $typeParams) {
				$lines.Add(('  - `{0}`: {1}' -f $param.name, (Clean-Text $param.InnerText)))
			}
		}

		$params = @($member.SelectNodes('param'))
		if ($params.Count -gt 0) {
			$lines.Add('- Parameters:')
			foreach ($param in $params) {
				$lines.Add(('  - `{0}`: {1}' -f $param.name, (Clean-Text $param.InnerText)))
			}
		}

		$returns = Clean-Text (Get-XmlChildText $member 'returns')
		if ($returns) {
			$lines.Add(('- Returns: {0}' -f $returns))
		}

		$tags = Extract-AiTags $member
		if ($tags) {
			$lines.Add(('- AI-Tags: {0}' -f $tags))
		}

		$lines.Add('')
	}

	return [pscustomobject]@{
		Text        = ($lines -join "`r`n")
		MemberCount = $members.Count
	}
}

function Test-GeneratedPack([string] $Root, [string[]] $UploadFiles) {
	$actual = @(Get-ChildItem -LiteralPath $Root -File | Where-Object { $_.Name -match '^\d\d-.*\.md$' })
	if ($actual.Count -ne $UploadFiles.Count) {
		throw "Expected $($UploadFiles.Count) numbered upload files, found $($actual.Count)."
	}

	foreach ($fileName in $UploadFiles) {
		$path = Join-Path $Root $fileName
		if (-not (Test-Path -LiteralPath $path)) {
			throw "Upload file missing: $path"
		}

		$bytes = [System.IO.File]::ReadAllBytes($path)
		if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
			throw "File has UTF-8 BOM: $path"
		}

		$text = [System.Text.UTF8Encoding]::new($false, $true).GetString($bytes)
		if ([System.Text.RegularExpressions.Regex]::IsMatch($text, '(?<!\r)\n')) {
			throw "File has LF-only line endings: $path"
		}
	}

	$api = Read-Utf8File (Join-Path $Root '04-api-discovery-and-extract.md')
	if (-not $api.Contains('Search anchors:')) {
		throw 'API extract does not contain Search anchors lines.'
	}

	$hints = Read-Utf8File (Join-Path $Root '11-hints.md')
	$map = Read-Utf8File (Join-Path $Root '12-hints-api-map.md')
	foreach ($needle in @('provider marker', 'AsSqlServer()', 'AsOracle()', 'AsClickHouse()')) {
		if (-not ($hints.Contains($needle) -or $map.Contains($needle))) {
			throw "Provider marker canary missing: $needle"
		}
	}

	$guide = Read-Utf8File (Join-Path $Root '01-skill.md')
	$instructionsPath = Join-Path $Root 'custom-gpt-instructions.md'
	$instructions = if (Test-Path -LiteralPath $instructionsPath) { Read-Utf8File $instructionsPath } else { '' }
	foreach ($needle in @('outside knowledge', 'not specific to LinqToDB', 'package-grounded', 'map it to LinqToDB')) {
		if (-not ($guide.Contains($needle) -or $hints.Contains($needle) -or $instructions.Contains($needle))) {
			throw "Knowledge-boundary canary missing: $needle"
		}
	}

	$stalePattern = '\]\((\.\./|\.\\|docs/|crud/|AGENT_GUIDE\.md|SKILL\.md|hints\.md|hints-api-map\.md|[^)]*linq2db\.xml|https://github\.com/linq2db/linq2db/blob/master/docs/|https://linq2db\.github\.io/api/)'
	$staleLinks = New-Object System.Collections.Generic.List[string]
	foreach ($fileName in $UploadFiles) {
		$path = Join-Path $Root $fileName
		$text = Read-Utf8File $path
		foreach ($match in [System.Text.RegularExpressions.Regex]::Matches($text, $stalePattern)) {
			$staleLinks.Add(('{0}: {1}' -f $fileName, $match.Value))
			if ($staleLinks.Count -ge 20) {
				break
			}
		}
		if ($staleLinks.Count -ge 20) {
			break
		}
	}

	if ($staleLinks.Count -gt 0) {
		throw "Stale package-local links remain after rewrite:`n$($staleLinks -join "`n")"
	}
}

if (-not (Test-Path -LiteralPath $sourceRoot)) {
	throw "Source root not found: $sourceRoot"
}

if (-not $NoBuild) {
	$project = Join-Path $RepoRoot 'Source\LinqToDB\LinqToDB.csproj'
	$args = @('build', $project, '-c', $Configuration)
	if ($NoRestore) {
		$args += '--no-restore'
	}
	& dotnet @args
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet build failed with exit code $LASTEXITCODE."
	}
}

if ([string]::IsNullOrWhiteSpace($XmlDocPath)) {
	$candidates = @(
		(Join-Path $RepoRoot ".build\bin\LinqToDB\$Configuration\net10.0\linq2db.xml"),
		(Join-Path $RepoRoot '.build\bin\LinqToDB\net10.0\linq2db.xml'),
		(Join-Path $RepoRoot "Source\LinqToDB\bin\$Configuration\net10.0\linq2db.xml")
	)
	$XmlDocPath = ($candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)
}

if ([string]::IsNullOrWhiteSpace($XmlDocPath) -or -not (Test-Path -LiteralPath $XmlDocPath)) {
	throw 'Could not find linq2db.xml. Pass -XmlDocPath or build the project first.'
}

$bundles = [ordered]@{
	'01-skill.md'                          = @('SKILL.md')
	'02-coverage.md'                       = @('docs/coverage.md')
	'03-overview-readme.md'                = @('../../LinqToDB/README.md')
	'04-api-discovery-and-extract.md'      = @('docs/api.md')
	'05-architecture.md'                   = @('docs/architecture.md')
	'06-agent-antipatterns-and-ai-tags.md' = @('docs/agent-antipatterns.md', 'docs/ai-tags.md')
	'07-provider-configuration.md'         = @('docs/provider-capabilities.md', 'docs/provider-setup.md', 'docs/configuration.md')
	'08-mapping.md'                        = @('docs/mapping.md')
	'09-crud-and-merge.md'                 = @(
		'docs/crud/crud.md',
		'docs/crud/crud-select.md',
		'docs/crud/crud-insert.md',
		'docs/crud/crud-insert-values.md',
		'docs/crud/crud-insert-select.md',
		'docs/crud/crud-upsert.md',
		'docs/crud/crud-update.md',
		'docs/crud/crud-delete.md',
		'docs/crud/crud-bulkcopy.md',
		'docs/crud/crud-merge.md'
	)
	'10-query-composition.md'              = @('docs/query-cte.md', 'docs/query-temp-tables.md')
	'11-hints.md'                          = @('docs/hints.md')
	'12-hints-api-map.md'                  = @('docs/hints-api-map.md')
	'13-custom-sql.md'                     = @('docs/custom-sql.md')
	'14-translatable-methods.md'           = @('docs/translatable-methods.md')
	'15-interceptors.md'                   = @('docs/interceptors.md')
}

$uploadFiles = @($bundles.Keys) + @('16-xml-doc.md')

if (-not (Test-Path -LiteralPath $OutputRoot)) {
	New-Item -ItemType Directory -Path $OutputRoot | Out-Null
}

Get-ChildItem -LiteralPath $OutputRoot -File -Filter '[0-9][0-9]-*.md' |
	Where-Object { $uploadFiles -notcontains $_.Name } |
	Remove-Item -Force

foreach ($entry in $bundles.GetEnumerator()) {
	Write-Utf8CrLfFile (Join-Path $OutputRoot $entry.Key) (New-MarkdownBundle $entry.Value)
}

$xmlDoc = New-XmlDocMarkdown $XmlDocPath
Write-Utf8CrLfFile (Join-Path $OutputRoot '16-xml-doc.md') $xmlDoc.Text

$includedDocs = @(
	$bundles.Values | ForEach-Object { $_ }
) | ForEach-Object { $_ } | Sort-Object

$manifest = [ordered]@{
	generated_at      = (Get-Date).ToString('s')
	prompt_source     = $maintenancePath
	source_root       = $sourceRoot
	xml_source        = (Resolve-Path -LiteralPath $XmlDocPath).Path
	upload_file_count = $uploadFiles.Count
	upload_files      = $uploadFiles
	xml_member_count  = $xmlDoc.MemberCount
	included_docs     = $includedDocs
}

$manifestJson = $manifest | ConvertTo-Json -Depth 8
Write-Utf8CrLfFile (Join-Path $OutputRoot 'bundle-manifest.json') $manifestJson
Write-Utf8CrLfFile (Join-Path $OutputRoot 'manifest.json') $manifestJson

$readme = @"
# linq2db Expert Knowledge Pack

Generated from ``$maintenancePath``.

Upload only the numbered markdown files (``01-*.md`` through ``16-*.md``) to Custom GPT Knowledge.

Do not upload supporting files such as ``README.md``, ``MAINTENANCE.md``, ``manifest.json``, ``bundle-manifest.json``, or ``custom-gpt-instructions.md``.

Paste the curated ``custom-gpt-instructions.md`` file from this directory into the GPT Instructions field separately.
"@
Write-Utf8CrLfFile (Join-Path $OutputRoot 'README.md') $readme

$maintenance = @"
# Maintenance

Source of truth: ``$maintenancePath``.

Regenerate this directory from package-local docs and XML-doc. Do not edit generated numbered files by hand.
"@
Write-Utf8CrLfFile (Join-Path $OutputRoot 'MAINTENANCE.md') $maintenance

Test-GeneratedPack $OutputRoot $uploadFiles

Write-Host "Generated linq2db Expert knowledge pack:"
Write-Host "  Output: $OutputRoot"
Write-Host "  Upload files: $($uploadFiles.Count)"
Write-Host "  XML members: $($xmlDoc.MemberCount)"
