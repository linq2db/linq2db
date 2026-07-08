using System;
using System.Globalization;
using System.IO;
using System.Text;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Resolves raw command/tool query options into executor settings.
	/// </summary>
	internal sealed class QueryExecutionSettingsResolver(ICliEnvironment environment)
	{
		const string DefaultProfileName        = "default";
		const int    DefaultMaxRows            = 1000;
		const string MissingEnvironmentVariable = "\u0000";

		readonly ICliEnvironment _environment = environment;

		public int ErrorStatusCode { get; private set; } = StatusCodes.INVALID_ARGUMENTS;

		public QueryExecutionSettings? Resolve(QueryExecutionOptionValues values)
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

			var configDirectory = configFileName != null ? Path.GetDirectoryName(configFileName) : null;
			var providerName         = values.Provider ?? configuration?.Provider;
			var providerLocationPath = values.ProviderLocation != null
				? ResolvePath(QueryExecutionCliOptions.ProviderLocation, values.ProviderLocation)
				: ResolvePath(QueryExecutionCliOptions.ProviderLocation, configuration?.ProviderLocation, configDirectory);
			var connectionStringText = GetConfiguredValue(QueryExecutionCliOptions.ConnectionString, values.ConnectionString, values.ConnectionStringEnv, configuration?.ConnectionString, configuration?.ConnectionStringEnv);
			var userName             = GetConfiguredValue(QueryExecutionCliOptions.User,             values.User,             values.UserEnv,             configuration?.User,             configuration?.UserEnv);
			var passwordText         = GetConfiguredValue(QueryExecutionCliOptions.Password,         values.Password,         values.PasswordEnv,         configuration?.Password,         configuration?.PasswordEnv);
			var commandTimeoutValue  = values.CommandTimeout != null ? ParseTimeout(QueryExecutionCliOptions.CommandTimeout, values.CommandTimeout) : configuration?.CommandTimeout;
			var lockTimeoutValue     = values.LockTimeout    != null ? ParseTimeout(QueryExecutionCliOptions.LockTimeout,    values.LockTimeout)    : configuration?.LockTimeout;
			var maxRowsValue         = values.MaxRows        != null ? ParseRowCount(QueryExecutionCliOptions.MaxRows,       values.MaxRows)        : configuration?.MaxRows ?? DefaultMaxRows;
			var outputFormat         = values.Output ?? configuration?.Output ?? values.DefaultOutput;
			var outputFileName       = values.OutputFile != null
				? ResolvePath(QueryExecutionCliOptions.OutputFile, values.OutputFile)
				: values.UseConfiguredOutputFile
					? ResolvePath(QueryExecutionCliOptions.OutputFile, configuration?.OutputFile, configDirectory)
					: null;
			var impersonateValue     = values.Impersonate ?? configuration?.Impersonate ?? false;
			var impersonateModeValue = ParseImpersonateMode(values.ImpersonateMode ?? configuration?.ImpersonateMode);
			var unsafeSqlPolicy      = configuration?.UnsafeSqlPolicy ?? UnsafeSqlPolicy.Deny;
			var querySql             = values.Sql;
			var querySqlFile         = ResolvePath(QueryExecutionCliOptions.SqlFile, values.SqlFile);

			if (commandTimeoutValue < 0 || lockTimeoutValue < 0 || maxRowsValue < 0)
				return null;

			if (impersonateModeValue == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.ImpersonateMode.Name}' has unknown value '{values.ImpersonateMode ?? configuration?.ImpersonateMode}'.");
				return null;
			}

			if (!IsKnownOutputFormat(outputFormat))
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Output.Name}' has unknown value '{outputFormat}'.");
				return null;
			}

			if (string.Equals(connectionStringText, MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(userName,             MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(passwordText,         MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(providerLocationPath, MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(outputFileName,       MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(querySqlFile,         MissingEnvironmentVariable, StringComparison.Ordinal))
				return null;

			if (providerName == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Provider.Name}' must be specified.");
				return null;
			}

			if (connectionStringText == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.ConnectionString.Name}' must be specified.");
				return null;
			}

			try
			{
				connectionStringText = string.Format(CultureInfo.InvariantCulture, connectionStringText, userName, passwordText);
			}
			catch (FormatException ex)
			{
				_environment.Error.WriteLine($"Invalid connection string format: {ex.Message} Use '{{0}}' for user, '{{1}}' for password, and escape literal braces as '{{{{' and '}}}}'.");
				return null;
			}

			if (values.AllowUnsafeSql && unsafeSqlPolicy == UnsafeSqlPolicy.Deny)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.AllowUnsafeSql.Name}' cannot be used because unsafe SQL policy is 'deny'.");
				return null;
			}

			if (impersonateValue && (userName == null || passwordText == null))
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Impersonate.Name}' requires resolved '--{QueryExecutionCliOptions.User.Name}' and '--{QueryExecutionCliOptions.Password.Name}' values.");
				return null;
			}

			if (querySql == null && querySqlFile == null)
			{
				_environment.Error.WriteLine($"Either '--{QueryExecutionCliOptions.Sql.Name}' or '--{QueryExecutionCliOptions.SqlFile.Name}' option must be specified.");
				return null;
			}

			if (querySqlFile != null)
			{
				if (!_environment.FileExists(querySqlFile))
				{
					_environment.Error.WriteLine($"SQL file '{querySqlFile}' not found.");
					ErrorStatusCode = StatusCodes.EXPECTED_ERROR;
					return null;
				}

				querySql = _environment.ReadAllText(querySqlFile);
			}

			return new QueryExecutionSettings(
				profileName,
				providerName,
				providerLocationPath,
				userName,
				passwordText,
				connectionStringText,
				commandTimeoutValue,
				lockTimeoutValue,
				maxRowsValue,
				outputFormat,
				outputFileName,
				values.Overwrite,
				unsafeSqlPolicy,
				values.AllowUnsafeSql,
				impersonateValue,
				impersonateModeValue.Value,
				querySql!);
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

		static bool IsKnownOutputFormat(string value)
		{
			return string.Equals(value, "json",       StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "json-table", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "csv",        StringComparison.OrdinalIgnoreCase);
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

		string? GetEnvironmentValue(CliOption option, string environmentVariableName)
		{
			var value = _environment.GetEnvironmentVariable(environmentVariableName);

			if (value != null)
				return value;

			_environment.Error.WriteLine($"Environment variable '{environmentVariableName}' specified for option '--{option.Name}' is not set.");
			return MissingEnvironmentVariable;
		}

		string? ResolvePath(CliOption option, string? path, string? baseDirectory = null)
		{
			if (path == null)
				return null;

			var result = new StringBuilder(path.Length);

			for (var i = 0; i < path.Length; i++)
			{
				if (path[i] == '%')
				{
					var end = path.IndexOf('%', i + 1);

					if (end > i + 1)
					{
						if (!TryAppendEnvironmentValue(option, path.Substring(i + 1, end - i - 1), result))
							return MissingEnvironmentVariable;

						i = end;
						continue;
					}
				}

				if (path[i] == '$' && i + 1 < path.Length && path[i + 1] == '{')
				{
					var end = path.IndexOf('}', i + 2);

					if (end > i + 2)
					{
						if (!TryAppendEnvironmentValue(option, path.Substring(i + 2, end - i - 2), result))
							return MissingEnvironmentVariable;

						i = end;
						continue;
					}
				}

				result.Append(path[i]);
			}

			var resolvedPath = result.ToString();

			if (!string.IsNullOrEmpty(baseDirectory) && !Path.IsPathRooted(resolvedPath))
				resolvedPath = Path.Combine(baseDirectory, resolvedPath);

			return resolvedPath;
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

		int ParseRowCount(CliOption option, string value)
		{
			if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var rowCount) && rowCount >= 0)
				return rowCount;

			_environment.Error.WriteLine($"Option '--{option.Name}' must be a non-negative integer row count.");
			return -1;
		}
	}
}
