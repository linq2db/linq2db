using System.Collections.Generic;
using System.Text.Json;

namespace LinqToDB.CLI
{
	/// <summary>
	/// Name-value string dictionary CLI option.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <c>true</c>, used requred to specify this option.</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	internal sealed record StringDictionaryCliOption(
		string    Name,
		char?     ShortName,
		bool      Required,
		string    Help,
		string?   DetailedHelp,
		string[]? Examples,
		string[]? JsonExamples)
		: CliOption(
			Name,
			ShortName,
			OptionType.StringDictionary,
			Required,
			true,
			true,
			true,
			Help,
			DetailedHelp,
			Examples,
			JsonExamples)
	{
		public override object? ParseCLI(CliCommand command, string rawValue)
		{
			var result = new Dictionary<string, string>();
			foreach (var entry in rawValue.Split(','))
			{
				var parts = entry.Split('=');

				if (parts.Length != 2)
					return null;
				if (result.ContainsKey(parts[0]))
					return null;

				result.Add(parts[0], parts[1]);
			}

			return result;
		}

		public override object? ParseJSON(JsonElement rawValue)
		{
			if (rawValue.ValueKind == JsonValueKind.Object)
			{
				var result = new Dictionary<string, string>();

				foreach (var property in rawValue.EnumerateObject())
				{
					if (result.ContainsKey(property.Name))
						return null;

					if (property.Value.ValueKind != JsonValueKind.String)
						return null;

					result.Add(property.Name, property.Value.GetString()!);
				}

				return result;
			}

			return null;
		}
	}
}
