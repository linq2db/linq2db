param(
	[Parameter(Mandatory = $true)]
	[string] $XmlDocPath,

	[Parameter(Mandatory = $true)]
	[string] $ApiDocPath
)

$ErrorActionPreference = 'Stop'

$marker = '## Generated API Extract'

function CleanText([string] $text) {
	if ([string]::IsNullOrWhiteSpace($text)) {
		return ''
	}

	$clean = [System.Text.RegularExpressions.Regex]::Replace($text, '\s+', ' ').Trim()
	return $clean.Replace('|', '\|')
}

function ExtractAiTags($member) {
	$text = ''

	if ($member.remarks) {
		$text = $member.remarks.InnerText
	}

	if ([string]::IsNullOrWhiteSpace($text)) {
		$text = $member.InnerText
	}

	$match = [System.Text.RegularExpressions.Regex]::Match($text, 'AI-Tags:\s*([^\r\n]+)')
	if (-not $match.Success) {
		return ''
	}

	return CleanText $match.Groups[1].Value
}

function GetKind([string] $id) {
	switch ($id.Substring(0, 1)) {
		'T' { return 'Type' }
		'M' { return 'Method' }
		'P' { return 'Property' }
		'F' { return 'Field' }
		'E' { return 'Event' }
		default { return 'Member' }
	}
}

function GetFamily([string] $id) {
	$body = $id.Substring(2)
	$body = [System.Text.RegularExpressions.Regex]::Replace($body, '\(.*$', '')
	$body = $body.Replace('#ctor', 'Constructor')
	$body = [System.Text.RegularExpressions.Regex]::Replace($body, '``\d+', '')
	$body = [System.Text.RegularExpressions.Regex]::Replace($body, '`\d+', '')
	return $body
}

function GetAnchors([string] $family, [string] $summary, [string] $tags) {
	$parts = New-Object System.Collections.Generic.List[string]
	$last  = $family.Split('.')[-1]
	$parts.Add($last)

	$words = [System.Text.RegularExpressions.Regex]::Matches($last, '[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+')
	foreach ($wordMatch in $words) {
		$word = $wordMatch.Value
		if ($word.Length -ge 3 -and -not $parts.Contains($word)) {
			$parts.Add($word)
		}
	}

	if ($tags) {
		foreach ($piece in $tags.Split(';')) {
			$value = $piece.Trim()
			if ($value -and -not $parts.Contains($value)) {
				$parts.Add($value)
			}
		}
	}

	if ($summary -match '\b[A-Z_]{3,}\b') {
		foreach ($summaryMatch in [System.Text.RegularExpressions.Regex]::Matches($summary, '\b[A-Z_]{3,}\b')) {
			if (-not $parts.Contains($summaryMatch.Value)) {
				$parts.Add($summaryMatch.Value)
			}
		}
	}

	return (($parts | Select-Object -First 12) -join ', ')
}

if (-not (Test-Path -LiteralPath $XmlDocPath)) {
	throw "XML documentation file not found: $XmlDocPath"
}

if (-not (Test-Path -LiteralPath $ApiDocPath)) {
	throw "API documentation file not found: $ApiDocPath"
}

$existing    = [System.IO.File]::ReadAllText($ApiDocPath, [System.Text.Encoding]::UTF8)
$markerIndex = $existing.IndexOf($marker)

if ($markerIndex -ge 0) {
	$manual = $existing.Substring(0, $markerIndex).TrimEnd()
}
else {
	$manual = $existing.TrimEnd()
}

$manual = [System.Text.RegularExpressions.Regex]::Replace($manual, '(\r?\n\s*---\s*)+$', '').TrimEnd()

[xml] $xml = [System.IO.File]::ReadAllText($XmlDocPath, [System.Text.Encoding]::UTF8)

$items = foreach ($member in $xml.doc.members.member) {
	$id = [string] $member.name

	if ($id -notmatch '^[A-Z]:LinqToDB\.') {
		continue
	}

	[pscustomobject] @{
		Id      = $id
		Kind    = GetKind $id
		Family  = GetFamily $id
		Summary = CleanText $member.summary.InnerText
		Tags    = ExtractAiTags $member
	}
}

$groups = $items | Group-Object Family | Sort-Object Name
$lines  = New-Object System.Collections.Generic.List[string]

$lines.Add($manual)
$lines.Add('')
$lines.Add('---')
$lines.Add('')
$lines.Add($marker)
$lines.Add('')
$lines.Add('This section is generated from the package XML documentation and optimized for agent retrieval.')
$lines.Add('It includes `LinqToDB.*` XML-doc members and groups overload families into compact tables instead of repeating one long section per overload.')
$lines.Add('Use `Search anchors` lines as the primary discovery surface, then verify exact signatures, parameter docs, remarks, and constraints in `lib/<TFM>/linq2db.xml`.')
$lines.Add('')
$lines.Add('Missing from this compact section is not proof that an API or overload is absent. Search XML-doc before falling back to generic APIs.')
$lines.Add('')
$lines.Add(('Generated LinqToDB member entries: {0}. API families: {1}.' -f $items.Count, $groups.Count))
$lines.Add('')

foreach ($group in $groups) {
	$entries           = @($group.Group | Sort-Object Id)
	$summaryForAnchors = ($entries | Where-Object { $_.Summary } | Select-Object -First 1).Summary
	$tagsForAnchors    = (($entries | Where-Object { $_.Tags } | Select-Object -First 3 | ForEach-Object { $_.Tags }) -join '; ')
	$anchors           = GetAnchors $group.Name $summaryForAnchors $tagsForAnchors

	$lines.Add(('### {0}' -f $group.Name))
	$lines.Add('')
	$lines.Add(('Kind: {0}.' -f (($entries.Kind | Select-Object -Unique) -join ', ')))

	if ($anchors) {
		$lines.Add(('Search anchors: {0}.' -f $anchors))
	}

	$lines.Add('')
	$lines.Add('| XML member | Summary | AI-Tags |')
	$lines.Add('|---|---|---|')

	foreach ($entry in $entries) {
		$lines.Add(('| `{0}` | {1} | {2} |' -f $entry.Id, $entry.Summary, $entry.Tags))
	}

	$lines.Add('')
}

$text      = ($lines -join "`r`n") + "`r`n"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($ApiDocPath, $text, $utf8NoBom)
