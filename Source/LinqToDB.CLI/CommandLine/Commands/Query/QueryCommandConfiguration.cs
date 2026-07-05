using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Query command configuration loaded from JSON profile file.
	/// The root object contains a required profile named "default" and optional named profiles.
	/// Named profiles inherit values from the "default" profile because it is applied first.
	/// </summary>
	internal sealed class QueryCommandConfiguration
	{
		private const string DefaultProfileName = "default";

		/// <summary>
		/// linq2db provider name.
		/// </summary>
		public string? Provider         { get; private set; }

		/// <summary>
		/// Database connection string. It is always formatted with <see cref="string.Format(System.IFormatProvider,string,object?[])"/>;
		/// <c>{0}</c> is replaced with <see cref="User"/> and <c>{1}</c> is replaced with <see cref="Password"/>.
		/// </summary>
		public string? ConnectionString { get; private set; }

		/// <summary>
		/// Optional database user name used as <c>{0}</c> argument for connection string formatting.
		/// </summary>
		public string? User             { get; private set; }

		/// <summary>
		/// Optional database password used as <c>{1}</c> argument for connection string formatting.
		/// </summary>
		public string? Password         { get; private set; }

		/// <summary>
		/// Optional query command timeout in seconds.
		/// </summary>
		public int?    CommandTimeout   { get; private set; }

		/// <summary>
		/// Optional provider-specific lock wait timeout in seconds.
		/// </summary>
		public int?    LockTimeout      { get; private set; }

		/// <summary>
		/// Optional maximum number of result rows to read.
		/// </summary>
		public int?    MaxRows          { get; private set; }

		/// <summary>
		/// Unsafe SQL execution policy. This value is intentionally available only from configuration profiles.
		/// </summary>
		public QuerySqlSafetyMode? SqlSafety { get; private set; }

		/// <summary>
		/// Query output format.
		/// </summary>
		public string? Output           { get; private set; }

		/// <summary>
		/// Optional output file path. When it is not specified, query result is written to stdout.
		/// </summary>
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
				switch (property.Name)
				{
					case "provider":
						if (!TryGetString(fileName, profileName, property, out var value, out error))
							return false;

						Provider = value;
						break;
					case "connectionString":
					case "connection-string":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						ConnectionString = value;
						break;
					case "user":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						User = value;
						break;
					case "password":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						Password = value;
						break;
					case "commandTimeout":
					case "command-timeout":
						if (!TryParseTimeout(fileName, profileName, property, out var timeout, out error))
							return false;

						CommandTimeout = timeout;
						break;
					case "lockTimeout":
					case "lock-timeout":
						if (!TryParseTimeout(fileName, profileName, property, out timeout, out error))
							return false;

						LockTimeout = timeout;
						break;
					case "maxRows":
					case "max-rows":
						if (!TryParseRowCount(fileName, profileName, property, out var maxRows, out error))
							return false;

						MaxRows = maxRows;
						break;
					case "unsafeSql":
					case "unsafe-sql":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						if (!TryParseSqlSafety(value, out var sqlSafety))
						{
							error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' has unknown value '{value}'.";
							return false;
						}

						SqlSafety = sqlSafety;
						break;
					case "output":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						if (!string.Equals(value, "json", StringComparison.OrdinalIgnoreCase)
							&& !string.Equals(value, "json-table", StringComparison.OrdinalIgnoreCase)
							&& !string.Equals(value, "csv", StringComparison.OrdinalIgnoreCase))
						{
							error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' has unknown value '{value}'.";
							return false;
						}

						Output = value!.ToLowerInvariant();
						break;
					case "outputFile":
					case "output-file":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

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

		static bool TryGetString(string fileName, string profileName, JsonProperty property, out string? value, out string? error)
		{
			if (property.Value.ValueKind != JsonValueKind.String)
			{
				value = null;
				error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' must be string.";
				return false;
			}

			value = property.Value.GetString();
			error = null;
			return true;
		}

		static bool TryParseTimeout(string fileName, string profileName, JsonProperty property, out int? timeout, out string? error)
		{
			if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var numericValue) && numericValue >= 0)
			{
				timeout = numericValue;
				error   = null;
				return true;
			}

			if (property.Value.ValueKind == JsonValueKind.String
				&& int.TryParse(property.Value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out numericValue)
				&& numericValue >= 0)
			{
				timeout = numericValue;
				error   = null;
				return true;
			}

			timeout = null;
			error   = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' must be a non-negative integer number of seconds.";
			return false;
		}

		static bool TryParseRowCount(string fileName, string profileName, JsonProperty property, out int? rowCount, out string? error)
		{
			if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var numericValue) && numericValue >= 0)
			{
				rowCount = numericValue;
				error    = null;
				return true;
			}

			if (property.Value.ValueKind == JsonValueKind.String
				&& int.TryParse(property.Value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out numericValue)
				&& numericValue >= 0)
			{
				rowCount = numericValue;
				error    = null;
				return true;
			}

			rowCount = null;
			error    = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' must be a non-negative integer row count.";
			return false;
		}

		static bool TryParseSqlSafety(string? value, out QuerySqlSafetyMode sqlSafety)
		{
			if (string.Equals(value, "deny", StringComparison.OrdinalIgnoreCase))
			{
				sqlSafety = QuerySqlSafetyMode.Deny;
				return true;
			}

			if (string.Equals(value, "confirm", StringComparison.OrdinalIgnoreCase))
			{
				sqlSafety = QuerySqlSafetyMode.Confirm;
				return true;
			}

			if (string.Equals(value, "allow", StringComparison.OrdinalIgnoreCase))
			{
				sqlSafety = QuerySqlSafetyMode.Allow;
				return true;
			}

			sqlSafety = default;
			return false;
		}
	}
}
