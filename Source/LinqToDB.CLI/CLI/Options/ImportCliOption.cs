using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LinqToDB.CLI
{
	/// <summary>
	/// Path to JSON file with CLI options.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	internal sealed record ImportCliOption(
		string    Name,
		char?     ShortName,
		string    Help,
		string?   DetailedHelp,
		string[]? Examples)
		: CliOption(
			Name,
			ShortName,
			OptionType.JSONImport,
			false,
			false,
			false,
			true,
			Help,
			DetailedHelp,
			Examples,
			null)
	{
		public override object? ParseCLI(CliCommand command, string rawValue)
		{
			var results = new Dictionary<CliOption, object?>();

			// as json parsing errors could be tricky for user to identify, we should log detailed errors here
			// currently we parse json using fail-fast approach
			if (!File.Exists(rawValue))
			{
				Console.Error.WriteLine("JSON ({0}): object expected as root element", rawValue);
				return null;
			}

			var json = JsonDocument.Parse(File.ReadAllText(rawValue));

			if (json.RootElement.ValueKind != JsonValueKind.Object)
			{
				Console.Error.WriteLine("JSON ({0}): object expected as root element", rawValue);
				return null;
			}

			foreach (var categoryProperty in json.RootElement.EnumerateObject())
			{
				var category = command.Categories.SingleOrDefault(c => c.Name == categoryProperty.Name);

				if (category == null)
				{
					Console.Error.WriteLine("JSON ({0}): unknown property: {1}", rawValue, categoryProperty.Name);
					return null;
				}

				var categoryOptions = new HashSet<CliOption>(command.GetCategoryOptions(category));

				// do not think it makes sense to support
				//if (categoryProperty.Value.ValueKind == JsonValueKind.Null || categoryProperty.Value.ValueKind == JsonValueKind.Undefined)
					//continue;

				if (categoryProperty.Value.ValueKind != JsonValueKind.Object)
				{
					Console.Error.WriteLine("JSON ({0}): property '{1}' must be object", rawValue, categoryProperty.Name);
					return null;
				}

				foreach (var optionProperty in categoryProperty.Value.EnumerateObject())
				{
					var option = command.GetOptionByName(optionProperty.Name);
					if (option == null)
					{
						Console.Error.WriteLine("JSON ({0}): unknown property '{1}.{2}'", rawValue, categoryProperty.Name, optionProperty.Name);
						return null;
					}
					if (!categoryOptions.Contains(option))
					{
						Console.Error.WriteLine("JSON ({0}): property '{2}' doesn't belong to '{1}'", rawValue, categoryProperty.Name, optionProperty.Name);
						return null;
					}

					if (!option.AllowInJson)
					{
						Console.Error.WriteLine("JSON ({0}): option '{1}.{2}' not supported in JSON", rawValue, categoryProperty.Name, optionProperty.Name);
						return null;
					}

					if (results.ContainsKey(option))
					{
						// TODO: is it possible or json parser will throw earlier?
						Console.Error.WriteLine("JSON ({0}): option '{1}.{2}' specified multiple times", rawValue, categoryProperty.Name, optionProperty.Name);
						return null;
					}

					var value = option.ParseJSON(optionProperty.Value);
					if (value == null)
					{
						Console.Error.WriteLine("JSON ({0}): cannot parse option '{1}.{2}' value: {3}", rawValue, categoryProperty.Name, optionProperty.Name, optionProperty.Value);
						return null;
					}

					results.Add(option, value);
				}
			}

			return results;
		}

		public override object? ParseJSON(JsonElement rawValue)
		{
			// we don't support JSON import from JSON
			return null;
		}
	}
}
