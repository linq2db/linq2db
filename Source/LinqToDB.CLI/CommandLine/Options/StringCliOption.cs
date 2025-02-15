using System.Collections.Generic;
using System.Text.Json;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Arbitrary string (more or less) CLI option.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <c>true</c>, used requred to specify this option.</param>
	/// <param name="AllowMultiple">When <c>true</c>, user can specify multiple values (separated by comma).</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	/// <param name="Default">Optional default value (or values for <paramref name="AllowMultiple"/> set to <c>true</c>), used when user didn't specified option explicitly in default mode.</param>
	/// <param name="T4Default">Optional default value (or values for <paramref name="AllowMultiple"/> set to <c>true</c>), used when user didn't specified option explicitly in T4-compat mode.</param>
	internal sealed record StringCliOption(
		string    Name,
		char?     ShortName,
		bool      Required,
		bool      AllowMultiple,
		string    Help,
		string?   DetailedHelp,
		string[]? Examples,
		string[]? JsonExamples,
		string[]? Default,
		string[]? T4Default)
		: CliOption(
			Name,
			ShortName,
			OptionType.String,
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
			errorDetails = null;

			if (AllowMultiple)
				return rawValue.Split(',');

			return rawValue;
		}

		public override object? ParseJSON(JsonElement rawValue, out string? errorDetails)
		{
			if (AllowMultiple)
			{
				var values = new List<string>();

				if (rawValue.ValueKind == JsonValueKind.Array)
				{
					foreach (var value in rawValue.EnumerateArray())
					{
						if (value.ValueKind != JsonValueKind.String)
						{
							errorDetails = $"array should contain strings but '{value.ValueKind}' value found";
							return null;
						}

						values.Add(value.GetString()!);
					}
				}

				errorDetails = null;
				return values.ToArray();

			}
			else
			{
				if (rawValue.ValueKind == JsonValueKind.String)
				{
					errorDetails = null;
					return rawValue.GetString()!;
				}
			}

			errorDetails = $"string expected but got '{rawValue.ValueKind}'";
			return null;
		}
	}
}
