using System.Text.Json;

namespace LinqToDB.CLI
{
	/// <summary>
	/// Database object filtering by name CLI option.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <c>true</c>, used requred to specify this option.</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	internal sealed record ObjectNameFilterCliOption(
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
			OptionType.DatabaseObjectFilter,
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
			var filter = new NameFilter();

			var names = rawValue.Split(',');

			foreach (var name in names)
			{
				if (name == string.Empty || !filter.AddName(null, name))
					return null;
			}

			return filter;
		}

		public override object? ParseJSON(JsonElement rawValue)
		{
			if (rawValue.ValueKind == JsonValueKind.Array)
			{
				var filter = new NameFilter();

				foreach (var element in rawValue.EnumerateArray())
				{
					if (element.ValueKind == JsonValueKind.String)
						filter.AddName(null, element.GetString()!);
					else if (element.ValueKind == JsonValueKind.Object)
					{
						foreach (var property in element.EnumerateObject())
						{
							var hasSchema  = false;
							string? name   = null;
							string? regex  = null;
							string? schema = null;
							switch (property.Name)
							{
								case "name"  :
									if (name != null || regex != null || property.Value.ValueKind != JsonValueKind.String)
										return null;

									name = property.Value.GetString()!;
									break;
								case "regex" :
									if (name != null || regex != null || property.Value.ValueKind != JsonValueKind.String)
										return null;

									regex = property.Value.GetString()!;
									break;
								case "schema":
									if (hasSchema)
										return null;

									hasSchema = true;

									if (property.Value.ValueKind == JsonValueKind.Null || property.Value.ValueKind == JsonValueKind.Undefined)
										break;

									if (property.Value.ValueKind != JsonValueKind.String)
										return null;

									schema = property.Value.GetString()!;
									break;
								default      :
									return null;
							}

							if (name != null)
								filter.AddName(schema, name);
							else if (regex != null)
								filter.AddRegularExpression(schema, regex);
							else
								return null;
						}
					}
					else
						return null;
				}

				return filter;
			}

			return null;
		}
	}
}
