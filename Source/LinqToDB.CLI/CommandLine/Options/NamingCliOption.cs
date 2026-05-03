using System.Collections.Generic;
using System.Text.Json;

using LinqToDB.Naming;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Code identifier normalization/generation options. Not supported in CLI (JSON only).
	/// </summary>
	/// <param name="Name">Option name (used as JSON property name).</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	/// <param name="Default">Optional default value, used when user didn't specified option explicitly.</param>
	internal sealed record NamingCliOption(
		string                Name,
		string                Help,
		string?               DetailedHelp,
		string[]?             JsonExamples,
		NormalizationOptions? Default,
		NormalizationOptions? T4Default)
		: CliOption(
			Name,
			null,
			OptionType.Naming,
			false,
			false,
			true,
			false,
			Help,
			DetailedHelp,
			null,
			JsonExamples)
	{
		public override object? ParseCommandLine(CliCommand command, string rawValue, out string? errorDetails)
		{
			// not supported from CLI
			errorDetails = null;
			return null;
		}

		public override object? ParseJSON(JsonElement rawValue, out string? errorDetails)
		{
			if (rawValue.ValueKind != JsonValueKind.Object)
			{
				errorDetails = $"expected object, but got {rawValue.ValueKind}";
				return null;
			}

			var properties = new HashSet<string>(System.StringComparer.Ordinal);

			var options = new NormalizationOptions();

			foreach (var property in rawValue.EnumerateObject())
			{
				if (!properties.Add(property.Name))
				{
					errorDetails = $"duplicate property '{property.Name}'";
					return null;
				}

				switch (property.Name)
				{
					case "case"                            :
						if (property.Value.ValueKind != JsonValueKind.String)
						{
							errorDetails = $"case : expected string value, but got {property.Value.ValueKind}";
							return null;
						}

						var caseValue = property.Value.GetString()!;
						switch (caseValue.ToLowerInvariant())
						{
							case "none"         : options.Casing = NameCasing.None                 ; break;
							case "camel_case"   : options.Casing = NameCasing.CamelCase            ; break;
							case "lower_case"   : options.Casing = NameCasing.LowerCase            ; break;
							case "pascal_case"  : options.Casing = NameCasing.Pascal               ; break;
							case "snake_case"   : options.Casing = NameCasing.SnakeCase            ; break;
							case "t4"           : options.Casing = NameCasing.T4CompatNonPluralized; break;
							case "t4_pluralized": options.Casing = NameCasing.T4CompatPluralized   ; break;
							case "upper_case"   : options.Casing = NameCasing.UpperCase            ; break;
							default             :
								errorDetails = $"case : unknown value: '{caseValue}'";
								return null;
						}

						break;
					case "pluralization"                   :
						if (property.Value.ValueKind != JsonValueKind.String)
						{
							errorDetails = $"pluralization : expected string value, but got {property.Value.ValueKind}";
							return null;
						}

						var pluralizationValue = property.Value.GetString()!;
						switch (pluralizationValue.ToLowerInvariant())
						{
							case "none"                      : options.Pluralization = Pluralization.None                 ; break;
							case "plural"                    : options.Pluralization = Pluralization.Plural               ; break;
							case "plural_multiple_characters": options.Pluralization = Pluralization.PluralIfLongerThanOne; break;
							case "singular"                  : options.Pluralization = Pluralization.Singular             ; break;
							default                          :
								errorDetails = $"pluralization : unknown value: '{pluralizationValue}'";
								return null;
						}
						
						break;
					case "prefix"                          :
						if (property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
							options.Prefix = null;
						else if (property.Value.ValueKind == JsonValueKind.String)
							options.Prefix = property.Value.GetString()!;
						else
						{
							errorDetails = $"prefix : expected string value, but got {property.Value.ValueKind}";
							return null;
						}

						break;
					case "suffix"                          :
						if (property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
							options.Suffix = null;
						else if (property.Value.ValueKind == JsonValueKind.String)
							options.Suffix = property.Value.GetString()!;
						else
						{
							errorDetails = $"suffix : expected string value, but got {property.Value.ValueKind}";
							return null;
						}

						break;
					case "transformation"                  :
						if (property.Value.ValueKind != JsonValueKind.String)
						{
							errorDetails = $"transformation : expected string value, but got {property.Value.ValueKind}";
							return null;
						}

						var transformationValue = property.Value.GetString()!;
						switch (transformationValue.ToLowerInvariant())
						{
							case "none"               : options.Transformation = NameTransformation.None             ; break;
							case "split_by_underscore": options.Transformation = NameTransformation.SplitByUnderscore; break;
							case "association"        : options.Transformation = NameTransformation.Association      ; break;
							default                   :
								errorDetails = $"transformation : unknown value: '{transformationValue}'";
								return null;
						}
						
						break;
					case "pluralize_if_ends_with_word_only":
						if (property.Value.ValueKind == JsonValueKind.True)
							options.PluralizeOnlyIfLastWordIsText = true;
						else if (property.Value.ValueKind == JsonValueKind.False)
							options.PluralizeOnlyIfLastWordIsText = false;
						else
						{
							errorDetails = $"pluralize_if_ends_with_word_only : expected boolean value, but got {property.Value.ValueKind}";
							return null;
						}

						break;
					case "ignore_all_caps"                 :
						if (property.Value.ValueKind == JsonValueKind.True)
							options.DontCaseAllCaps = true;
						else if (property.Value.ValueKind == JsonValueKind.False)
							options.DontCaseAllCaps = false;
						else
						{
							errorDetails = $"ignore_all_caps : expected boolean value, but got {property.Value.ValueKind}";
							return null;
						}

						break;
					case "max_uppercase_word_length":
						if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var maxLength) && maxLength > 1)
							options.MaxUpperCaseWordLength = maxLength;
						else
						{
							errorDetails = $"max_uppercase_word_length : expected number >= 2, but got: {property.Value}";
							return null;
						}

						break;
					default:
						errorDetails = $"unexpected property '{property.Name}'";
						return null;
				}
			}

			errorDetails = null;
			return options;
		}
	}
}
