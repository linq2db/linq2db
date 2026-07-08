using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Query command configuration loaded from JSON profile file.
	/// The root object contains a required profile named "default" and optional named profiles.
	/// Named profiles inherit values from the "default" profile because it is applied first.
	/// </summary>
	internal sealed class QueryExecutionConfiguration
	{
		private const string DefaultProfileName = "default";

		/// <summary>
		/// linq2db provider name.
		/// </summary>
		public string? Provider         { get; private set; }

		/// <summary>
		/// Optional path to external provider assembly. Provider dependencies must be available next to it or through normal application probing.
		/// </summary>
		public string? ProviderLocation { get; private set; }

		/// <summary>
		/// Database connection string. It is always formatted with <see cref="string.Format(System.IFormatProvider,string,object?[])"/>;
		/// <c>{0}</c> is replaced with <see cref="User"/> and <c>{1}</c> is replaced with <see cref="Password"/>.
		/// </summary>
		public string? ConnectionString { get; private set; }

		/// <summary>
		/// Environment variable name that contains database connection string.
		/// </summary>
		public string? ConnectionStringEnv { get; private set; }

		/// <summary>
		/// Optional database user name used as <c>{0}</c> argument for connection string formatting.
		/// </summary>
		public string? User             { get; private set; }

		/// <summary>
		/// Environment variable name that contains database user name.
		/// </summary>
		public string? UserEnv          { get; private set; }

		/// <summary>
		/// Optional database password used as <c>{1}</c> argument for connection string formatting.
		/// </summary>
		public string? Password         { get; private set; }

		/// <summary>
		/// Environment variable name that contains database password.
		/// </summary>
		public string? PasswordEnv      { get; private set; }

		/// <summary>
		/// Run database access operations under resolved Windows <see cref="User"/>/<see cref="Password"/> credentials.
		/// </summary>
		public bool?   Impersonate      { get; private set; }

		/// <summary>
		/// Windows impersonation logon mode.
		/// </summary>
		public string? ImpersonateMode  { get; private set; }

		/// <summary>
		/// Optional query command timeout in seconds. Value <c>0</c> disables the option.
		/// </summary>
		public int?    CommandTimeout   { get; private set; }

		/// <summary>
		/// Optional provider-specific lock wait timeout in seconds. Value <c>0</c> disables the option.
		/// </summary>
		public int?    LockTimeout      { get; private set; }

		/// <summary>
		/// Optional maximum number of result rows to read. Value <c>0</c> disables the limit.
		/// </summary>
		public int?    MaxRows          { get; private set; }

		/// <summary>
		/// Unsafe SQL execution policy. This value is intentionally available only from configuration profiles.
		/// </summary>
		public UnsafeSqlPolicy? UnsafeSqlPolicy { get; private set; }

		/// <summary>
		/// Query output format.
		/// </summary>
		public string? Output           { get; private set; }

		/// <summary>
		/// Optional output file path. When it is not specified, query result is written to stdout.
		/// </summary>
		public string? OutputFile       { get; private set; }

		public static bool TryLoad(ICliEnvironment environment, string fileName, string profileName, out QueryExecutionConfiguration? configuration, out string? error)
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

				var result = new QueryExecutionConfiguration();

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
					case "providerLocation":
					case "provider-location":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						ProviderLocation = value;
						break;
					case "connectionString":
					case "connection-string":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						ConnectionString = value;
						break;
					case "connectionStringEnv":
					case "connection-string-env":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						ConnectionStringEnv = value;
						break;
					case "user":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						User = value;
						break;
					case "userEnv":
					case "user-env":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						UserEnv = value;
						break;
					case "password":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						Password = value;
						break;
					case "passwordEnv":
					case "password-env":
						if (!TryGetString(fileName, profileName, property, out value, out error))
							return false;

						PasswordEnv = value;
						break;
					case "impersonate":
						if (!TryParseBoolean(fileName, profileName, property, out var booleanValue, out error))
							return false;

						Impersonate = booleanValue;
						break;
					case "impersonateMode":
					case "impersonate-mode":
						if (property.Value.ValueKind == JsonValueKind.Number)
						{
							if (!property.Value.TryGetInt32(out var numericValue))
							{
								error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' has unknown value '{property.Value}'.";
								return false;
							}

							value = numericValue.ToString(CultureInfo.InvariantCulture);
						}
						else if (!TryGetString(fileName, profileName, property, out value, out error))
						{
							return false;
						}

						if (!IsValidImpersonateMode(value))
						{
							error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' has unknown value '{value}'.";
							return false;
						}

						ImpersonateMode = value!.ToLowerInvariant();
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

						if (!TryParseUnsafeSqlPolicy(value, out var unsafeSqlPolicy))
						{
							error = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' has unknown value '{value}'.";
							return false;
						}

						UnsafeSqlPolicy = unsafeSqlPolicy;
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

		static bool TryParseBoolean(string fileName, string profileName, JsonProperty property, out bool? result, out string? error)
		{
			if (property.Value.ValueKind == JsonValueKind.True)
			{
				result = true;
				error  = null;
				return true;
			}

			if (property.Value.ValueKind == JsonValueKind.False)
			{
				result = false;
				error  = null;
				return true;
			}

			if (property.Value.ValueKind == JsonValueKind.String
				&& bool.TryParse(property.Value.GetString(), out var parsedValue))
			{
				result = parsedValue;
				error  = null;
				return true;
			}

			result = null;
			error  = $"Configuration file '{fileName}' profile '{profileName}' property '{property.Name}' must be boolean.";
			return false;
		}

		static bool IsValidImpersonateMode(string? value)
		{
			return string.Equals(value, "network-cleartext", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "interactive", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "network", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "new-credentials", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "2", StringComparison.Ordinal)
				|| string.Equals(value, "3", StringComparison.Ordinal)
				|| string.Equals(value, "8", StringComparison.Ordinal)
				|| string.Equals(value, "9", StringComparison.Ordinal);
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

		static bool TryParseUnsafeSqlPolicy(string? value, out UnsafeSqlPolicy unsafeSqlPolicy)
		{
			if (string.Equals(value, "deny", StringComparison.OrdinalIgnoreCase))
			{
				unsafeSqlPolicy = Commands.QueryExecution.UnsafeSqlPolicy.Deny;
				return true;
			}

			if (string.Equals(value, "confirm", StringComparison.OrdinalIgnoreCase))
			{
				unsafeSqlPolicy = Commands.QueryExecution.UnsafeSqlPolicy.Confirm;
				return true;
			}

			if (string.Equals(value, "allow", StringComparison.OrdinalIgnoreCase))
			{
				unsafeSqlPolicy = Commands.QueryExecution.UnsafeSqlPolicy.Allow;
				return true;
			}

			unsafeSqlPolicy = default;
			return false;
		}
	}
}
