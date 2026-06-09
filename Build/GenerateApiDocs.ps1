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
	$clean = [System.Text.RegularExpressions.Regex]::Replace($clean, '\s+([.,;:!?])', '$1')
	$clean = $clean.Replace('\', '\\')
	$clean = $clean.Replace('|', '\|')
	$clean = $clean.Replace('[', '\[')
	$clean = $clean.Replace(']', '\]')
	return $clean
}

function FormatXmlReference([string] $value) {
	if ([string]::IsNullOrWhiteSpace($value)) {
		return ''
	}

	$ref = $value.Trim()
	$ref = [System.Text.RegularExpressions.Regex]::Replace($ref, '^[A-Z]:', '')
	$ref = [System.Text.RegularExpressions.Regex]::Replace($ref, '\(.*$', '')
	$ref = $ref.Replace('#ctor', 'Constructor')
	$ref = [System.Text.RegularExpressions.Regex]::Replace($ref, '``\d+', '')
	$ref = [System.Text.RegularExpressions.Regex]::Replace($ref, '`\d+', '')

	return $ref
}

function RenderXmlDocNode($node) {
	if ($node -eq $null) {
		return ''
	}

	$result = New-Object System.Text.StringBuilder

	foreach ($child in $node.ChildNodes) {
		switch ($child.NodeType) {
			'Text' {
				[void] $result.Append($child.Value)
			}
			'CDATA' {
				[void] $result.Append($child.Value)
			}
			'Element' {
				switch ($child.LocalName) {
					'see' {
						$value = ''
						if ($child.cref) {
							$value = FormatXmlReference $child.cref
						}
						elseif ($child.langword) {
							$value = $child.langword
						}
						elseif ($child.href) {
							$value = $child.href
						}
						else {
							$value = RenderXmlDocNode $child
						}

						if ($value) {
							[void] $result.Append((' `{0}` ' -f $value))
						}
					}
					'seealso' {
						$value = ''
						if ($child.cref) {
							$value = FormatXmlReference $child.cref
						}
						elseif ($child.href) {
							$value = $child.href
						}
						else {
							$value = RenderXmlDocNode $child
						}

						if ($value) {
							[void] $result.Append((' `{0}` ' -f $value))
						}
					}
					'paramref' {
						if ($child.name) {
							[void] $result.Append((' `{0}` ' -f $child.name))
						}
					}
					'typeparamref' {
						if ($child.name) {
							[void] $result.Append((' `{0}` ' -f $child.name))
						}
					}
					'c' {
						[void] $result.Append((' `{0}` ' -f $child.InnerText))
					}
					'code' {
						[void] $result.Append((' `{0}` ' -f $child.InnerText))
					}
					'para' {
						[void] $result.Append(' ')
						[void] $result.Append((RenderXmlDocNode $child))
						[void] $result.Append(' ')
					}
					default {
						[void] $result.Append((RenderXmlDocNode $child))
					}
				}
			}
			default {
				if ($child.InnerText) {
					[void] $result.Append($child.InnerText)
				}
			}
		}
	}

	return $result.ToString()
}

function GetXmlNodeText($node, [string] $name) {
	$child = $node.SelectSingleNode($name)
	if ($child -eq $null) {
		return ''
	}

	return RenderXmlDocNode $child
}

function GetSummaryText($member) {
	$summary = CleanText (GetXmlNodeText $member 'summary')
	$remarks = CleanText (GetXmlNodeText $member 'remarks')

	if ($remarks -match '^(Deprecated|Obsolete)\b') {
		return (CleanText ("$remarks $summary"))
	}

	return $summary
}

function GetAiTagDisplayName([string] $name) {
	switch ($name) {
		'group'         { return 'Group' }
		'groups'        { return 'Groups' }
		'execution'     { return 'Execution' }
		'composability' { return 'Composability' }
		'affects'       { return 'Affects' }
		'pipeline'      { return 'Pipeline' }
		'provider'      { return 'Provider' }
		'hint-type'     { return 'HintType' }
		default         { return $name }
	}
}

function GetAllowedAiTagValues([string] $name) {
	switch ($name) {
		'group'         { return @('QueryDirectives', 'NavigationLoading', 'Hints', 'DML', 'Merge', 'Helpers', 'Configuration', 'Connection', 'RawSQL', 'Schema') }
		'groups'        { return @('QueryDirectives', 'NavigationLoading', 'Hints', 'DML', 'Merge', 'Helpers', 'Configuration', 'Connection', 'RawSQL', 'Schema') }
		'execution'     { return @('Deferred', 'Immediate') }
		'composability' { return @('Composable', 'Terminal') }
		'affects'       { return @('DmlStatement', 'DdlStatement', 'QueryRoot', 'QueryStructure', 'QueryCompilation', 'JoinGraph', 'SqlSemantics', 'CommandBuilder', 'Data', 'QueryResult', 'ExecutionContext', 'ConnectionConfiguration', 'Configuration', 'SchemaResult', 'GeneratedSql') }
		'pipeline'      { return @('ExpressionTree', 'SqlAST', 'SqlText', 'Connection', 'Execution', 'BulkInsert') }
		'provider'      { return @('ProviderDefined', 'ProviderAgnostic') }
		'hint-type'     { return @('Table', 'TablesInScope', 'Index', 'Join', 'SubQuery', 'Query', 'Merge', 'TableName') }
		default         { return $null }
	}
}

function ValidateAiTagElement($node, [string] $memberId) {
	if ($node -eq $null) {
		return
	}

	if ($node.Attributes.Count -eq 0) {
		throw ("{0}: <{1}> must declare at least one attribute." -f $memberId, $node.Name)
	}

	foreach ($attribute in $node.Attributes) {
		$name    = $attribute.Name
		$value   = $attribute.Value.Trim()
		$allowed = GetAllowedAiTagValues $name

		if ($allowed -eq $null) {
			throw ("{0}: unknown <{1}> attribute '{2}'." -f $memberId, $node.Name, $name)
		}

		if ([string]::IsNullOrWhiteSpace($value)) {
			throw ("{0}: <{1}> attribute '{2}' must not be empty." -f $memberId, $node.Name, $name)
		}

		$values = if ($name -in @('groups', 'affects', 'pipeline')) { $value.Split(',') } else { @($value) }
		foreach ($raw in $values) {
			$item = $raw.Trim()
			if ([string]::IsNullOrWhiteSpace($item)) {
				throw ("{0}: <{1}> attribute '{2}' contains an empty value." -f $memberId, $node.Name, $name)
			}

			if ($allowed -notcontains $item) {
				throw ("{0}: invalid <{1}> attribute '{2}' value '{3}'." -f $memberId, $node.Name, $name, $item)
			}
		}
	}
}

function FormatAiTagElement($node) {
	if ($node -eq $null -or $node.Attributes.Count -eq 0) {
		return ''
	}

	$order  = @('group', 'groups', 'hint-type', 'execution', 'composability', 'affects', 'pipeline', 'provider')
	$parts  = New-Object System.Collections.Generic.List[string]
	$used   = New-Object System.Collections.Generic.HashSet[string]

	foreach ($name in $order) {
		$attribute = $node.Attributes[$name]
		if ($attribute -and -not [string]::IsNullOrWhiteSpace($attribute.Value)) {
			[void] $parts.Add(('{0}={1}' -f (GetAiTagDisplayName $name), $attribute.Value.Trim()))
			[void] $used.Add($name)
		}
	}

	foreach ($attribute in $node.Attributes) {
		if (-not $used.Contains($attribute.Name) -and -not [string]::IsNullOrWhiteSpace($attribute.Value)) {
			[void] $parts.Add(('{0}={1}' -f (GetAiTagDisplayName $attribute.Name), $attribute.Value.Trim()))
		}
	}

	if ($parts.Count -eq 0) {
		return ''
	}

	return (($parts -join '; ') + ';')
}

function ExtractAiTags($member) {
	$tagNode = $member.SelectSingleNode('ai-tags')
	$tags = FormatAiTagElement $tagNode
	if ($tags) {
		return CleanText $tags
	}

	$text = ''

	$remarks = GetXmlNodeText $member 'remarks'
	if ($remarks) {
		$text = $remarks
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

	if ($summary -match '\b[A-Z_]{3,}\b') {
		foreach ($summaryMatch in [System.Text.RegularExpressions.Regex]::Matches($summary, '\b[A-Z_]{3,}\b')) {
			if (-not $parts.Contains($summaryMatch.Value)) {
				$parts.Add($summaryMatch.Value)
			}
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

	return (($parts | Select-Object -First 16) -join ', ')
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

$xmlMemberCount   = 0
$externalExcluded = 0
$internalExcluded = 0
$itemsList        = New-Object System.Collections.Generic.List[object]

foreach ($member in $xml.doc.members.member) {
	$xmlMemberCount++
	$id = [string] $member.name

	$tagsInsideRemarks = @($member.SelectNodes('remarks//ai-tags | remarks//ai-tags-defaults'))
	if ($tagsInsideRemarks.Count -gt 0) {
		throw ("{0}: <ai-tags /> and <ai-tags-defaults /> must be sibling XML-doc elements, not children of <remarks>." -f $id)
	}

	foreach ($tagNode in @($member.SelectNodes('ai-tags | ai-tags-defaults'))) {
		ValidateAiTagElement $tagNode $id
	}

	if ($id -notmatch '^[A-Z]:LinqToDB\.') {
		$externalExcluded++
		continue
	}

	if ($id -match '^[A-Z]:LinqToDB\.Internal\.') {
		$internalExcluded++
		continue
	}

	$itemsList.Add([pscustomobject] @{
		Id      = $id
		Kind    = GetKind $id
		Family  = GetFamily $id
		Summary = GetSummaryText $member
		Tags    = ExtractAiTags $member
	})
}

$items  = $itemsList.ToArray()
$groups = $items | Group-Object Family | Sort-Object Name
$lines  = New-Object System.Collections.Generic.List[string]
$aiTagsCount = @($items | Where-Object { $_.Tags }).Count
$noSummaryCount = @($items | Where-Object { -not $_.Summary }).Count

$lines.Add($manual)
$lines.Add('')
$lines.Add('---')
$lines.Add('')
$lines.Add($marker)
$lines.Add('')
$lines.Add('This section is generated from the package XML documentation and optimized for agent retrieval.')
$lines.Add('It includes consumer-supported `LinqToDB.*` XML-doc members and groups overload families into compact tables instead of repeating one long section per overload.')
$lines.Add('It intentionally excludes `LinqToDB.Internal.*`; do not use `LinqToDB.Internal.*` APIs in application code even if they are public in the assembly.')
$lines.Add('Do not read this generated section sequentially. Treat it as a search index: search by task terms, provider name, SQL keyword, member name, receiver type, or AI metadata.')
$lines.Add('AI metadata is generated from XML-doc `<ai-tags />` elements. It is not human-facing `<remarks>` text.')
$lines.Add('Use `Search anchors` lines as the primary discovery surface, then use `lib/<TFM>/linq2db.xml` only when you need exact signatures, parameter docs, remarks, and constraints that are not clear from this extract.')
$lines.Add('')
$lines.Add('Missing from this compact section is not proof that an API or overload is absent. Search XML-doc before falling back to generic APIs.')
$lines.Add('')
$lines.Add(('Generated from: `{0}`.' -f (Split-Path -Leaf $XmlDocPath)))
$lines.Add(('XML members scanned: {0}. Included consumer LinqToDB members: {1}. API families: {2}.' -f $xmlMemberCount, $items.Count, $groups.Count))
$lines.Add(('Excluded members: {0} `LinqToDB.Internal.*`; {1} external/non-LinqToDB.' -f $internalExcluded, $externalExcluded))
$lines.Add(('Included members with AI metadata: {0}. Included members without summary: {1}.' -f $aiTagsCount, $noSummaryCount))
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
	$lines.Add('| XML member | Summary |')
	$lines.Add('|---|---|')

	foreach ($entry in $entries) {
		$lines.Add(('| `{0}` | {1} |' -f $entry.Id, $entry.Summary))
	}

	$metadataEntries = @($entries | Where-Object { $_.Tags })
	if ($metadataEntries.Count -gt 0) {
		$lines.Add('')
		$lines.Add('AI metadata:')
		$lines.Add('')
		$lines.Add('| XML member | AI metadata |')
		$lines.Add('|---|---|')

		foreach ($entry in $metadataEntries) {
			$lines.Add(('| `{0}` | {1} |' -f $entry.Id, $entry.Tags))
		}
	}

	$lines.Add('')
}

$text      = ($lines -join "`r`n") + "`r`n"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($ApiDocPath, $text, $utf8NoBom)
