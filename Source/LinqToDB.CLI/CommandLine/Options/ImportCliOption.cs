using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LinqToDB.CommandLine
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
		public override object? ParseCommandLine(CliCommand command, string rawValue, out string? errorDetails)
		{
			var results  = new Dictionary<CliOption, object?>();

			// as json parsing errors could be tricky for user to identify, we should log detailed errors here
			// currently we parse json using fail-fast approach
			if (!File.Exists(rawValue))
			{
				errorDetails = $"JSON ({rawValue}): object expected as root element";
				return null;
			}

			var options = new JsonDocumentOptions()
			{
				CommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true
			};
			var json = JsonDocument.Parse(File.ReadAllText(rawValue), options);

			if (json.RootElement.ValueKind != JsonValueKind.Object)
			{
				errorDetails = $"JSON ({rawValue}): object expected as root element";
				return null;
			}

			var conflictingOptions = new HashSet<CliOption>();

			foreach (var categoryProperty in json.RootElement.EnumerateObject())
			{
				var category = command.Categories.SingleOrDefault(c => c.JsonProperty == categoryProperty.Name);

				if (category == null)
				{
					errorDetails = $"JSON ({rawValue}): unknown property: {categoryProperty.Name}";
					return null;
				}

				var categoryOptions = new HashSet<CliOption>(command.GetCategoryOptions(category));

				// do not think it makes sense to support
				//if (categoryProperty.Value.ValueKind == JsonValueKind.Null || categoryProperty.Value.ValueKind == JsonValueKind.Undefined)
					//continue;

				if (categoryProperty.Value.ValueKind != JsonValueKind.Object)
				{
					errorDetails = $"JSON ({rawValue}): property '{categoryProperty.Name}' must be object";
					return null;
				}

				foreach (var optionProperty in categoryProperty.Value.EnumerateObject())
				{
					var option = command.GetOptionByName(optionProperty.Name);
					if (option == null)
					{
						errorDetails = $"JSON ({rawValue}): unknown property '{categoryProperty.Name}.{optionProperty.Name}'";
						return null;
					}

					if (!categoryOptions.Contains(option))
					{
						errorDetails = $"JSON ({rawValue}): property '{categoryProperty.Name}' doesn't belong to '{optionProperty.Name}'";
						return null;
					}

					if (!option.AllowInJson)
					{
						errorDetails = $"JSON ({rawValue}): option '{categoryProperty.Name}.{optionProperty.Name}' not supported in JSON";
						return null;
					}

					if (results.ContainsKey(option))
					{
						// TODO: is it possible or json parser will throw earlier?
						errorDetails = $"JSON ({rawValue}): option '{categoryProperty.Name}.{optionProperty.Name}' specified multiple times";
						return null;
					}
					else if (conflictingOptions.Contains(option))
					{
						errorDetails = $"Option '{categoryProperty.Name}.{optionProperty.Name}' conflicts with other option(s): {string.Join(", ", command.GetIncompatibleOptions(option)!.Select(o => $"{o.Name}"))}";
						return null;
					}

					var incompatibleOptions = command.GetIncompatibleOptions(option);
					if (incompatibleOptions != null)
					{
						foreach (var opt in incompatibleOptions)
							conflictingOptions.Add(opt);
					}

					var value = option.ParseJSON(optionProperty.Value, out errorDetails);
					if (value == null)
					{
						errorDetails = $"JSON ({rawValue}): cannot parse option '{categoryProperty.Name}.{optionProperty.Name}' value '{optionProperty.Value}' : {errorDetails}";
						return null;
					}

					results.Add(option, value);
				}
			}

			errorDetails = null;
			return results;
		}

		public override object? ParseJSON(JsonElement rawValue, out string? errorDetails)
		{
			// we don't support JSON import from JSON
			errorDetails = null;
			return null;
		}
	}
}
