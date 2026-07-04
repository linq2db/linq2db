using System;
using System.IO;
using System.Text.Json;

namespace LinqToDB.CommandLine
{
	internal sealed class QueryCommandConfiguration
	{
		private const string DefaultProfileName = "default";

		public string? Provider         { get; private set; }
		public string? ConnectionString { get; private set; }
		public string? Output           { get; private set; }
		public string? OutputFile       { get; private set; }

		public static bool TryLoad(ICliEnvironment environment, string fileName, string profileName, out QueryCommandConfiguration? configuration, out string? error)
		{
			configuration = null;

			if (!environment.FileExists(fileName))
			{
				error = $"Configuration file '{fileName}' not found.";
				return false;
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

				if (!json.RootElement.TryGetProperty(DefaultProfileName, out var defaultProfile))
				{
					error = $"Configuration file '{fileName}' doesn't contain '{DefaultProfileName}' profile.";
					return false;
				}

				var result = new QueryCommandConfiguration();

				if (!result.ApplyProfile(fileName, DefaultProfileName, defaultProfile, out error))
					return false;

				if (!string.Equals(profileName, DefaultProfileName, StringComparison.Ordinal))
				{
					if (!json.RootElement.TryGetProperty(profileName, out var profile))
					{
						error = $"Configuration file '{fileName}' doesn't contain '{profileName}' profile.";
						return false;
					}

					if (!result.ApplyProfile(fileName, profileName, profile, out error))
						return false;
				}

				configuration = result;
				error         = null;

				return true;
			}
		}

		private bool ApplyProfile(string fileName, string profileName, JsonElement profile, out string? error)
		{
			if (profile.ValueKind != JsonValueKind.Object)
			{
				error = $"Configuration file '{fileName}' profile '{profileName}' must be object.";
				return false;
			}

			foreach (var property in profile.EnumerateObject())
			{
				if (property.Value.ValueKind != JsonValueKind.String)
				{
					error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' must be string.";
					return false;
				}

				var value = property.Value.GetString();

				switch (property.Name)
				{
					case "provider":
						Provider = value;
						break;
					case "connectionString":
					case "connection-string":
						ConnectionString = value;
						break;
					case "output":
						if (!string.Equals(value, "json", StringComparison.OrdinalIgnoreCase) && !string.Equals(value, "csv", StringComparison.OrdinalIgnoreCase))
						{
							error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' has unknown value '{value}'.";
							return false;
						}

						Output = value!.ToLowerInvariant();
						break;
					case "outputFile":
					case "output-file":
						OutputFile = value;
						break;
					default:
						error = $"Configuration file '{fileName}' profile '{profileName}' contains unknown property '{property.Name}'.";
						return false;
				}
			}

			error = null;
			return true;
		}
	}
}
