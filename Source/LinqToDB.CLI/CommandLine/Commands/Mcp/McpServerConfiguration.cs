using System;
using System.Text.Json;

using LinqToDB.CommandLine;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// Instance-specific MCP server metadata loaded from the query configuration file.
	/// </summary>
	internal sealed record McpServerConfiguration(
		string Title,
		string Description,
		string Instructions)
	{
		const string SectionName         = "mcp";
		const string DefaultTitle        = "linq2db Database Tools";
		const string DefaultDescription  = "Provider-aware database schema inspection and SQL access through linq2db CLI.";
		const string DefaultInstructions = "Call linq2db_info first to discover available database profiles and provider dialects. Call linq2db_schema before generating SQL when database objects are unknown. Use linq2db_query for read-only SQL. Call linq2db_skill for the full linq2db CLI/MCP usage guide, supported providers, configuration rules, and safety guidance. Use linq2db_execute only when it is available and the user explicitly approved the exact write-capable operation.";

		public static McpServerConfiguration Default { get; } = new(DefaultTitle, DefaultDescription, DefaultInstructions);

		public static bool TryLoad(ICliEnvironment environment, string? fileName, out McpServerConfiguration configuration, out string? error)
		{
			configuration = Default;

			if (fileName == null || !environment.FileExists(fileName))
			{
				error = null;
				return true;
			}

			JsonDocument json;

			try
			{
				json = JsonDocument.Parse(
					environment.ReadAllText(fileName),
					new JsonDocumentOptions
					{
						CommentHandling     = JsonCommentHandling.Skip,
						AllowTrailingCommas = true,
					});
			}
			catch (JsonException ex)
			{
				error = $"Configuration file '{fileName}' is not valid JSON: {ex.Message}";
				return false;
			}

			using (json)
			{
				if (json.RootElement.ValueKind != JsonValueKind.Object)
				{
					error = $"Configuration file '{fileName}' must contain JSON object as root element.";
					return false;
				}

				if (!json.RootElement.TryGetProperty(SectionName, out var section))
				{
					error = null;
					return true;
				}

				if (section.ValueKind != JsonValueKind.Object)
				{
					error = $"Configuration file '{fileName}' section '{SectionName}' must be object.";
					return false;
				}

				var     title            = DefaultTitle;
				var     description      = DefaultDescription;
				string? userInstructions = null;

				foreach (var property in section.EnumerateObject())
				{
					if (property.Name is not ("title" or "description" or "instructions"))
					{
						error = $"Configuration file '{fileName}' section '{SectionName}' contains unknown property '{property.Name}'.";
						return false;
					}

					if (property.Value.ValueKind != JsonValueKind.String)
					{
						error = $"Configuration file '{fileName}' section '{SectionName}' property '{property.Name}' must be string.";
						return false;
					}

					switch (property.Name)
					{
						case "title"       : title            = property.Value.GetString()!; break;
						case "description" : description      = property.Value.GetString()!; break;
						case "instructions": userInstructions = property.Value.GetString();  break;
					}
				}

				var instructions = userInstructions == null
					? DefaultInstructions
					: string.Concat(DefaultInstructions, "\n\n", userInstructions);

				configuration = new McpServerConfiguration(title, description, instructions);
				error         = null;

				return true;
			}
		}
	}
}
