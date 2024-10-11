using System.Text.Json;

namespace LinqToDB.CommandLine
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
		public override object? ParseCommandLine(CliCommand command, string rawValue, out string? errorDetails)
		{
			var filter = new NameFilter();

			var names = rawValue.Split(',');

			foreach (var name in names)
			{
				if (name.Length == 0 || !filter.AddName(null, name))
				{
					errorDetails = $"comma-separated list contains empty or duplicate elements: {rawValue}";
					return null;
				}
			}

			errorDetails = null;
			return filter;
		}

		public override object? ParseJSON(JsonElement rawValue, out string? errorDetails)
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
						var hasSchema  = false;
						string? name   = null;
						string? regex  = null;
						string? schema = null;

						foreach (var property in element.EnumerateObject())
						{
							switch (property.Name)
							{
								case "name":
									if (name != null)
									{
										errorDetails = $"duplicate 'name' property";
										return null;
									}

									if (regex != null)
									{
										errorDetails = $"both 'name' and 'regex' properties specified";
										return null;
									}

									if (property.Value.ValueKind != JsonValueKind.String)
									{
										errorDetails = $"'name' should be string but was '{property.Value.ValueKind}'";
										return null;
									}

									name = property.Value.GetString()!;
									break;
								case "regex":
									if (name != null)
									{
										errorDetails = $"duplicate 'regex' property";
										return null;
									}

									if (regex != null)
									{
										errorDetails = $"both 'name' and 'regex' properties specified";
										return null;
									}

									if (property.Value.ValueKind != JsonValueKind.String)
									{
										errorDetails = $"'regex' should be string but was '{property.Value.ValueKind}'";
										return null;
									}

									regex = property.Value.GetString()!;
									break;
								case "schema":
									if (hasSchema)
									{
										errorDetails = $"duplicate 'schema' property";
										return null;
									}

									hasSchema = true;

									if (property.Value.ValueKind == JsonValueKind.Null || property.Value.ValueKind == JsonValueKind.Undefined)
										break;

									if (property.Value.ValueKind != JsonValueKind.String)
									{
										errorDetails = $"'schema' should be string but was '{property.Value.ValueKind}'";
										return null;
									}

									schema = property.Value.GetString()!;
									break;
								default:
									errorDetails = $"unknown property '{property.Name}'";
									return null;
							}
						}

						if (name != null)
							filter.AddName(schema, name);
						else if (regex != null)
							filter.AddRegularExpression(schema, regex);
						else
						{
							errorDetails = $"'name' or 'regex' property required";
							return null;
						}
					}
					else
					{
						errorDetails = $"string or object expected but '{element.ValueKind}' provided";
						return null;
					}
				}

				errorDetails = null;
				return filter;
			}

			errorDetails = $"array expected but '{rawValue.ValueKind}' provided";
			return null;
		}
	}
}
