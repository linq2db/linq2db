using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// String-typed CLI option with fixed list of allowed values.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <see langword="true"/>, used requred to specify this option.</param>
	/// <param name="AllowMultiple">When <see langword="true"/>, user can specify multiple values (separated by comma).</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	/// <param name="CaseSensitive">Define option value parsing mode - case-sensitive or case-insensitive.</param>
	/// <param name="Values">List of allowed values (with defaults).</param>
	internal sealed record StringEnumCliOption(
		       string             Name,
		       char?              ShortName,
		       bool               Required,
		       bool               AllowMultiple,
		       string             Help,
		       string?            DetailedHelp,
		       string[]?          Examples,
		       string[]?          JsonExamples,
		       bool               CaseSensitive,
		params StringEnumOption[] Values)
		: CliOption(
			Name,
			ShortName,
			OptionType.StringEnum,
			Required,
			AllowMultiple,
			true,
			true,
			Help,
			DetailedHelp,
			Examples,
			JsonExamples)
	{
		public override object? ParseCommandLine(CliCommand command, string rawValue, out string? errorDetails)
		{
			// we don't operate with large lists to bother with Values lookup optimization with dictionary
			if (AllowMultiple)
			{
				var values = new List<string>();
				foreach (var val in rawValue.Split(','))
				{
					var found = false;
					foreach (var value in Values)
					{
						if (string.Equals(val, value.Value, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
						{
							values.Add(value.Value);
							found = true;
							break;
						}
					}

					if (!found)
					{
						errorDetails = $"unknown value '{val}'";
						return null;
					}
				}

				errorDetails = null;
				return values.ToArray();
			}
			else
			{
				foreach (var value in Values)
				{
					if (string.Equals(rawValue, value.Value, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
					{
						errorDetails = null;
						return value.Value;
					}
				}

				errorDetails = $"unknown value '{rawValue}'";
				return null;
			}
		}

		public override object? ParseJSON(JsonElement rawValue, out string? errorDetails)
		{
			if (AllowMultiple)
			{
				if (rawValue.ValueKind == JsonValueKind.Array)
				{
					var values = new List<string>();

					foreach (var element in rawValue.EnumerateArray())
					{
						if (element.ValueKind != JsonValueKind.String)
						{
							errorDetails = $"array should contain strings but got '{rawValue.ValueKind}' value";
							return null;
						}

						var stringValue = element.GetString()!;

						var found = false;
						foreach (var value in Values)
						{
							if (string.Equals(stringValue, value.Value, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
							{
								found = true;
								values.Add(value.Value);
								break;
							}
						}

						if (!found)
						{
							errorDetails = $"unknown value '{stringValue}'";
							return null;
						}
					}

					errorDetails = null;
					return values.ToArray();
				}

				errorDetails = $"array expected but got '{rawValue.ValueKind}'";
				return null;
			}
			else
			{
				if (rawValue.ValueKind == JsonValueKind.String)
				{
					var stringValue = rawValue.GetString()!;

					foreach (var value in Values)
					{
						if (string.Equals(stringValue, value.Value, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
						{
							errorDetails = null;
							return value.Value;
						}
					}

					errorDetails = $"unknown value '{stringValue}'";
					return null;
				}

				errorDetails = $"string expected but got '{rawValue.ValueKind}'";
				return null;
			}
		}
	}
}
