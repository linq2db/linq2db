using System;
using System.Text.Json;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Boolean CLI option descriptor.
	/// Option value format: <c>true | false</c>
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <see langword="true"/>, used requred to specify this option.</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	/// <param name="Default">Default option value when used didn't specified it explicitly in default mode.</param>
	/// <param name="T4Default">Default option value when used didn't specified it explicitly in T4-compat mode.</param>
	internal sealed record BooleanCliOption(
		string    Name,
		char?     ShortName,
		bool      Required,
		string    Help,
		string?   DetailedHelp,
		string[]? Examples,
		string[]? JsonExamples,
		bool      Default,
		bool      T4Default)
		: CliOption(
			Name,
			ShortName,
			OptionType.Boolean,
			Required,
			false,
			true,
			true,
			Help,
			DetailedHelp,
			Examples,
			JsonExamples)
	{
		public override object? ParseCommandLine(CliCommand command, string rawValue, out string? errorDetails)
		{
			if (string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase))
			{
				errorDetails = null;
				return true;
			}

			if (string.Equals(rawValue, "false", StringComparison.OrdinalIgnoreCase))
			{
				errorDetails = null;
				return false;
			}

			errorDetails = $"expected true or false";
			return null;
		}

		public override object? ParseJSON(JsonElement rawValue, out string? errorDetails)
		{
			if (rawValue.ValueKind == JsonValueKind.True)
			{
				errorDetails = null;
				return true;
			}

			if (rawValue.ValueKind == JsonValueKind.False)
			{
				errorDetails = null;
				return false;
			}

			errorDetails = $"expected boolean value, but got {rawValue.ValueKind}";
			return null;
		}
	}
}
