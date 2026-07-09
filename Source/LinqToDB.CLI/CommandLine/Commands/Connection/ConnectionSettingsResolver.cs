using System;
using System.Globalization;
using System.IO;
using System.Text;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Resolves trusted connection options from command/MCP inputs and optional configuration profiles.
	/// </summary>
	internal sealed class ConnectionSettingsResolver(ICliEnvironment environment)
	{
		const string DefaultProfileName         = "default";
		const string MissingEnvironmentVariable = "\u0000";

		readonly ICliEnvironment _environment = environment;

		public int ErrorStatusCode { get; private set; } = StatusCodes.INVALID_ARGUMENTS;

		public ConnectionSettings? Resolve(ConnectionOptionValues values)
		{
			ErrorStatusCode = StatusCodes.INVALID_ARGUMENTS;

			var profileName = values.Profile ?? DefaultProfileName;

			QueryExecutionConfiguration? configuration = null;
			if (values.Profile != null && values.Config == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Profile.Name}' requires option '--{QueryExecutionCliOptions.Config.Name}'.");
				return null;
			}

			var configFileName = ResolvePath(QueryExecutionCliOptions.Config, values.Config);

			if (string.Equals(configFileName, MissingEnvironmentVariable, StringComparison.Ordinal))
				return null;

			if (configFileName != null && !QueryExecutionConfiguration.TryLoad(_environment, configFileName, profileName, out configuration, out var error))
			{
				_environment.Error.WriteLine(error);
				return null;
			}

			var configDirectory     = configFileName != null ? Path.GetDirectoryName(configFileName) : null;
			var providerName        = values.Provider ?? configuration?.Provider;
			var providerLocation    = values.ProviderLocation != null
				? ResolvePath(QueryExecutionCliOptions.ProviderLocation, values.ProviderLocation)
				: ResolvePath(QueryExecutionCliOptions.ProviderLocation, configuration?.ProviderLocation, configDirectory);
			var connectionString    = GetConfiguredValue(QueryExecutionCliOptions.ConnectionString, values.ConnectionString, values.ConnectionStringEnv, configuration?.ConnectionString, configuration?.ConnectionStringEnv);
			var user                = GetConfiguredExpandableValue(QueryExecutionCliOptions.User, values.User, values.UserEnv, configuration?.User, configuration?.UserEnv);
			var password            = GetConfiguredValue(QueryExecutionCliOptions.Password, values.Password, values.PasswordEnv, configuration?.Password, configuration?.PasswordEnv);
			var commandTimeout      = values.CommandTimeout != null ? ParseTimeout(QueryExecutionCliOptions.CommandTimeout, values.CommandTimeout) : configuration?.CommandTimeout;
			var lockTimeout         = values.LockTimeout    != null ? ParseTimeout(QueryExecutionCliOptions.LockTimeout,    values.LockTimeout)    : configuration?.LockTimeout;
			var impersonate         = values.Impersonate ?? configuration?.Impersonate ?? false;
			var impersonateMode     = ParseImpersonateMode(values.ImpersonateMode ?? configuration?.ImpersonateMode);

			if (commandTimeout < 0 || lockTimeout < 0)
				return null;

			if (impersonateMode == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.ImpersonateMode.Name}' has unknown value '{values.ImpersonateMode ?? configuration?.ImpersonateMode}'.");
				return null;
			}

			if (string.Equals(connectionString, MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(user,             MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(password,         MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(providerLocation, MissingEnvironmentVariable, StringComparison.Ordinal))
				return null;

			if (providerName == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Provider.Name}' must be specified.");
				return null;
			}

			if (connectionString == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.ConnectionString.Name}' must be specified.");
				return null;
			}

			try
			{
				connectionString = string.Format(CultureInfo.InvariantCulture, connectionString, user, password);
			}
			catch (FormatException ex)
			{
				_environment.Error.WriteLine($"Invalid connection string format: {ex.Message} Use '{{0}}' for user, '{{1}}' for password, and escape literal braces as '{{{{' and '}}}}'.");
				return null;
			}

			if (impersonate && (user == null || password == null))
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Impersonate.Name}' requires resolved '--{QueryExecutionCliOptions.User.Name}' and '--{QueryExecutionCliOptions.Password.Name}' values.");
				return null;
			}

			return new ConnectionSettings(
				profileName,
				providerName,
				providerLocation,
				user,
				password,
				connectionString,
				commandTimeout,
				lockTimeout,
				impersonate,
				impersonateMode.Value,
				configDirectory,
				configuration);
		}

		public string? ResolvePath(CliOption option, string? path, string? baseDirectory = null)
		{
			if (path == null)
				return null;

			var resolvedPath = ResolveEnvironmentVariables(option, path);

			if (resolvedPath == null || string.Equals(resolvedPath, MissingEnvironmentVariable, StringComparison.Ordinal))
				return resolvedPath;

			if (!string.IsNullOrEmpty(baseDirectory) && !Path.IsPathRooted(resolvedPath))
				resolvedPath = Path.Combine(baseDirectory, resolvedPath);

			return resolvedPath;
		}

		static WindowsImpersonationMode? ParseImpersonateMode(string? value)
		{
			return value?.ToLower(CultureInfo.InvariantCulture) switch
			{
				null                => WindowsImpersonationMode.NetworkCleartext,
				"8"                 => WindowsImpersonationMode.NetworkCleartext,
				"network-cleartext" => WindowsImpersonationMode.NetworkCleartext,
				"2"                 => WindowsImpersonationMode.Interactive,
				"interactive"       => WindowsImpersonationMode.Interactive,
				"3"                 => WindowsImpersonationMode.Network,
				"network"           => WindowsImpersonationMode.Network,
				"9"                 => WindowsImpersonationMode.NewCredentials,
				"new-credentials"   => WindowsImpersonationMode.NewCredentials,
				_                   => null,
			};
		}

		int? ParseTimeout(CliOption option, string value)
		{
			if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var timeout) && timeout >= 0)
				return timeout;

			_environment.Error.WriteLine($"Option '--{option.Name}' must be a non-negative integer number of seconds.");
			return -1;
		}

		string? GetConfiguredValue(CliOption option, string? commandValue, string? commandEnvironmentVariableName, string? configurationValue, string? configurationEnvironmentVariableName)
		{
			if (commandValue != null)
				return commandValue;

			if (commandEnvironmentVariableName != null)
				return GetEnvironmentValue(option, commandEnvironmentVariableName);

			if (configurationValue != null)
				return configurationValue;

			if (configurationEnvironmentVariableName != null)
				return GetEnvironmentValue(option, configurationEnvironmentVariableName);

			return null;
		}

		string? GetConfiguredExpandableValue(CliOption option, string? commandValue, string? commandEnvironmentVariableName, string? configurationValue, string? configurationEnvironmentVariableName)
		{
			if (commandValue != null)
				return ResolveEnvironmentVariables(option, commandValue);

			if (commandEnvironmentVariableName != null)
				return GetEnvironmentValue(option, commandEnvironmentVariableName);

			if (configurationValue != null)
				return ResolveEnvironmentVariables(option, configurationValue);

			if (configurationEnvironmentVariableName != null)
				return GetEnvironmentValue(option, configurationEnvironmentVariableName);

			return null;
		}

		string? GetEnvironmentValue(CliOption option, string environmentVariableName)
		{
			var value = _environment.GetEnvironmentVariable(environmentVariableName);

			if (value != null)
				return value;

			_environment.Error.WriteLine($"Environment variable '{environmentVariableName}' specified for option '--{option.Name}' is not set.");
			return MissingEnvironmentVariable;
		}

		string? ResolveEnvironmentVariables(CliOption option, string? value)
		{
			if (value == null)
				return null;

			var result = new StringBuilder(value.Length);

			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] == '%')
				{
					var end = value.IndexOf('%', i + 1);

					if (end > i + 1)
					{
						if (!TryAppendEnvironmentValue(option, value.Substring(i + 1, end - i - 1), result))
							return MissingEnvironmentVariable;

						i = end;
						continue;
					}
				}

				if (value[i] == '$' && i + 1 < value.Length && value[i + 1] == '{')
				{
					var end = value.IndexOf('}', i + 2);

					if (end > i + 2)
					{
						if (!TryAppendEnvironmentValue(option, value.Substring(i + 2, end - i - 2), result))
							return MissingEnvironmentVariable;

						i = end;
						continue;
					}
				}

				result.Append(value[i]);
			}

			return result.ToString();
		}

		bool TryAppendEnvironmentValue(CliOption option, string name, StringBuilder result)
		{
			var value = _environment.GetEnvironmentVariable(name);

			if (value == null)
			{
				_environment.Error.WriteLine($"Environment variable '{name}' referenced by option '--{option.Name}' is not set.");
				return false;
			}

			result.Append(value);
			return true;
		}
	}
}
