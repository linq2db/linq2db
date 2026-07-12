param(
	[Parameter(Mandatory = $true)]
	[string] $XmlDocPath,

	[Parameter(Mandatory = $true)]
	[string] $ApiDocPath,

	# Compiled assembly matching $XmlDocPath. Used to read <ai-tags />/<ai-tags-defaults />
	# from AiTagsAttribute/AiTagsDefaultsAttribute (Source/LinqToDB/Internal/Metadata/AiTagsAttribute.cs).
	# Members not yet migrated to the attribute still carry XML-doc <ai-tags /> elements; those are
	# read via the legacy XML path below. Both paths are supported until the migration is complete.
	[Parameter(Mandatory = $true)]
	[string] $AssemblyPath
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

function ExtractAiTagsFromXml($member) {
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

#region Attribute-based AI-Tags (AiTagsAttribute / AiTagsDefaultsAttribute)
#
# Reads <ai-tags /> / <ai-tags-defaults /> for members migrated from the XML-doc custom-tag form to
# the internal AiTagsAttribute/AiTagsDefaultsAttribute (Source/LinqToDB/Internal/Metadata). Members
# not yet migrated fall back to ExtractAiTagsFromXml above. See .agents/ai-tags-attribute-design.md.

# Canonical display order. Multi-value ([Flags]) fields render in enum declaration order, not in the
# order a caller happened to combine them - this is a deliberate, accepted behavior change from the
# free-text XML-doc-attribute era, where value order followed whatever the author typed.
$attributeTagOrder = @('Groups', 'HintType', 'Execution', 'Composability', 'Affects', 'Pipeline', 'Provider')

function DecodeAttributeTypedValue($typedArg) {
	$type  = $typedArg.ArgumentType
	$value = $typedArg.Value

	if (-not $type.IsEnum) {
		return [string] $value
	}

	$isFlags = $type.GetCustomAttributes([FlagsAttribute], $false).Count -gt 0
	if (-not $isFlags) {
		return [System.Enum]::GetName($type, $value)
	}

	$names = New-Object System.Collections.Generic.List[string]
	foreach ($name in [System.Enum]::GetNames($type)) {
		$enumValue = [int] [System.Enum]::Parse($type, $name)
		if ($enumValue -ne 0 -and (([int] $value) -band $enumValue) -eq $enumValue) {
			[void] $names.Add($name)
		}
	}
	return ($names -join ',')
}

function GetNamedArgumentMap($attributeData) {
	$map = @{}
	if ($attributeData -eq $null) {
		return $map
	}

	foreach ($arg in $attributeData.NamedArguments) {
		$map[$arg.MemberName] = DecodeAttributeTypedValue $arg.TypedValue
	}
	return $map
}

function FormatMergedAiTags($ownMap, $defaultsMap) {
	# Merge rule (ai-tags.md "Defaults merge rules"): start from defaults, apply member-level on top,
	# member value wins per key, keys absent from the member are inherited from defaults.
	$merged = @{}
	foreach ($key in $defaultsMap.Keys) { $merged[$key] = $defaultsMap[$key] }
	foreach ($key in $ownMap.Keys)      { $merged[$key] = $ownMap[$key] }

	$parts = New-Object System.Collections.Generic.List[string]
	foreach ($key in $attributeTagOrder) {
		if ($merged.ContainsKey($key)) {
			[void] $parts.Add(('{0}={1}' -f $key, $merged[$key]))
		}
	}

	if ($parts.Count -eq 0) {
		return ''
	}
	return (($parts -join '; ') + ';')
}

function GetCustomAttributeDataByName($provider, [string] $attributeTypeName) {
	$all = [System.Reflection.CustomAttributeData]::GetCustomAttributes($provider)
	return ($all | Where-Object { $_.AttributeType.Name -eq $attributeTypeName } | Select-Object -First 1)
}

# Doc-comment-ID computation (MemberInfo -> "T:"/"M:"/"P:"/"F:"/"E:" id string), per the C#
# documentation-comment ID format (ECMA-334). Covers what linq2db's public surface actually uses:
# plain/nested/generic types, generic methods and method-level generic-parameter back-references,
# arrays, by-ref parameters, and generic type instantiations. Deliberately does not attempt pointers
# or multi-dimensional non-zero-lower-bound arrays - if id computation fails for a member, that
# member is simply skipped for attribute-based lookup (falls back to XML, or has no AI metadata),
# never mismatched to the wrong member.

function GetParamTypeDocName([Type] $type) {
	if ($type.IsByRef) {
		return (GetParamTypeDocName $type.GetElementType()) + '@'
	}

	if ($type.IsArray) {
		if ($type.GetArrayRank() -ne 1) {
			throw "Multi-dimensional arrays are not supported by GetParamTypeDocName."
		}
		return (GetParamTypeDocName $type.GetElementType()) + '[]'
	}

	if ($type.IsGenericParameter) {
		if ($type.DeclaringMethod -ne $null) {
			return ('``{0}' -f $type.GenericParameterPosition)
		}
		return ('`{0}' -f $type.GenericParameterPosition)
	}

	if ($type.IsGenericType -and -not $type.IsGenericTypeDefinition) {
		$def      = $type.GetGenericTypeDefinition()
		$baseName = ($def.FullName -replace '\+', '.') -replace '`\d+$', ''
		$args     = ($type.GetGenericArguments() | ForEach-Object { GetParamTypeDocName $_ }) -join ','
		return "$baseName{$args}"
	}

	$name = $type.FullName
	if ([string]::IsNullOrEmpty($name)) {
		throw ("Type '{0}' has no FullName (open generic parameter context)." -f $type.Name)
	}
	return ($name -replace '\+', '.')
}

function GetMemberDocId($member) {
	$declaringType = $member.DeclaringType
	$typeName      = ($declaringType.FullName -replace '\+', '.')

	if ($member -is [System.Reflection.MethodBase]) {
		$methodName = if ($member -is [System.Reflection.ConstructorInfo]) { '#ctor' } else { $member.Name }

		$arity = ''
		if ($member.IsGenericMethod) {
			$arity = '``{0}' -f $member.GetGenericArguments().Length
		}

		$params = $member.GetParameters()
		$paramList = ''
		if ($params.Length -gt 0) {
			$paramNames = $params | ForEach-Object { GetParamTypeDocName $_.ParameterType }
			$paramList = '(' + ($paramNames -join ',') + ')'
		}

		return "M:$typeName.$methodName$arity$paramList"
	}

	if ($member -is [System.Reflection.PropertyInfo]) {
		$params = $member.GetIndexParameters()
		$paramList = ''
		if ($params.Length -gt 0) {
			$paramNames = $params | ForEach-Object { GetParamTypeDocName $_.ParameterType }
			$paramList = '(' + ($paramNames -join ',') + ')'
		}
		return "P:$typeName.$($member.Name)$paramList"
	}

	if ($member -is [System.Reflection.FieldInfo]) {
		return "F:$typeName.$($member.Name)"
	}

	if ($member -is [System.Reflection.EventInfo]) {
		return "E:$typeName.$($member.Name)"
	}

	throw ("Unsupported member kind for doc-id computation: {0}" -f $member.GetType().Name)
}

function BuildAttributeAiTagsIndex([System.Reflection.Assembly] $assembly) {
	# id -> merged "Key=Value; ...;" string, for every member whose own AiTagsAttribute or whose
	# declaring type's AiTagsDefaultsAttribute contributes at least one field.
	$index      = @{}
	$typeCache  = @{}   # type.FullName -> @{ Own = map; Defaults = map }

	function GetTypeMaps([Type] $type) {
		$key = $type.FullName
		if ($typeCache.ContainsKey($key)) {
			return $typeCache[$key]
		}

		$own      = GetNamedArgumentMap (GetCustomAttributeDataByName $type 'AiTagsAttribute')
		$defaults = GetNamedArgumentMap (GetCustomAttributeDataByName $type 'AiTagsDefaultsAttribute')
		$result   = @{ Own = $own; Defaults = $defaults }
		$typeCache[$key] = $result
		return $result
	}

	try {
		$assemblyTypes = $assembly.GetTypes()
	}
	catch [System.Reflection.ReflectionTypeLoadException] {
		$assemblyTypes = $_.Exception.Types | Where-Object { $_ -ne $null }
	}

	foreach ($type in $assemblyTypes) {
		if (-not $type.IsPublic -and -not $type.IsNestedPublic) {
			continue
		}

		$typeMaps = GetTypeMaps $type

		try {
			$typeId = GetMemberDocId $type
		}
		catch {
			$typeId = $null
		}
		if ($typeId -eq $null -and $typeMaps.Own.Count -gt 0) {
			# Fallback for the type itself (GetMemberDocId only handles member kinds above); types use
			# the simple "T:Namespace.Name`Arity" form directly.
			$typeId = "T:$($type.FullName -replace '\+', '.')"
		}
		if ($typeMaps.Own.Count -gt 0 -and $typeId) {
			$formatted = FormatMergedAiTags $typeMaps.Own @{}
			if ($formatted) { $index[$typeId] = $formatted }
		}

		$members = @()
		$members += $type.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')
		$members += $type.GetConstructors([System.Reflection.BindingFlags]'Public,NonPublic,Instance,DeclaredOnly')
		$members += $type.GetProperties([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')
		$members += $type.GetFields([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')
		$members += $type.GetEvents([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')

		foreach ($member in $members) {
			$ownAttr = GetCustomAttributeDataByName $member 'AiTagsAttribute'
			if ($ownAttr -eq $null) {
				continue
			}

			try {
				$id = GetMemberDocId $member
			}
			catch {
				continue
			}

			$ownMap = GetNamedArgumentMap $ownAttr
			$merged = FormatMergedAiTags $ownMap $typeMaps.Defaults
			if ($merged) {
				$index[$id] = $merged
			}
		}
	}

	return $index
}
#endregion

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

if (-not (Test-Path -LiteralPath $AssemblyPath)) {
	throw "Assembly file not found: $AssemblyPath"
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

$assembly           = [System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$attributeTagsIndex = BuildAttributeAiTagsIndex $assembly

function ExtractAiTags($member, [string] $id) {
	if ($attributeTagsIndex.ContainsKey($id)) {
		return $attributeTagsIndex[$id]
	}
	return ExtractAiTagsFromXml $member
}

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
		Tags    = ExtractAiTags $member $id
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
$lines.Add('AI metadata is generated from `AiTagsAttribute`/`AiTagsDefaultsAttribute` (or, for not-yet-migrated members, legacy XML-doc `<ai-tags />` elements). It is not human-facing `<remarks>` text.')
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
