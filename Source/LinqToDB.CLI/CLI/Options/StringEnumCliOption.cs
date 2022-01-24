using System.Collections.Generic;
using System.Text.Json;

namespace LinqToDB.CLI
{
	/// <summary>
	/// String-typed CLI option with fixed list of allowed values.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <c>true</c>, used requred to specify this option.</param>
	/// <param name="AllowMultiple">When <c>true</c>, user can specify multiple values (separated by comma).</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
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
		StringEnumOption[] Values)
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
		public override object? ParseCLI(CliCommand command, string rawValue)
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
						if (rawValue == value.Value)
						{
							values.Add(rawValue);
							found = true;
							break;
						}
					}

					if (!found)
						return null;
				}

				return values.ToArray();
			}
			else
			{
				foreach (var value in Values)
				{
					if (rawValue == value.Value)
						return rawValue;
				}

				return null;
			}
		}

		public override object? ParseJSON(JsonElement rawValue)
		{
			if (AllowMultiple)
			{
				if (rawValue.ValueKind == JsonValueKind.Array)
				{
					var values = new List<string>();

					foreach (var element in rawValue.EnumerateArray())
					{
						if (element.ValueKind != JsonValueKind.String)
							return null;

						var stringValue = element.GetString()!;

						var found = false;
						foreach (var value in Values)
						{
							if (stringValue == value.Value)
							{
								found = true;
								values.Add(stringValue);
								break;
							}
						}

							if (!found)
							return null;
					}

					return values.ToArray();
				}
			}
			else
			{
				if (rawValue.ValueKind == JsonValueKind.String)
				{
					var stringValue = rawValue.GetString()!;

					foreach (var value in Values)
					{
						if (stringValue == value.Value)
							return stringValue;
					}
				}
			}

			return null;
		}
	}
}
