using System;
using System.Globalization;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.Connection;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Resolves raw command/tool query options into executor settings.
	/// </summary>
	internal sealed class QueryExecutionSettingsResolver(ICliEnvironment environment)
	{
		const int    DefaultMaxRows             = 1000;
		const string MissingEnvironmentVariable = "\u0000";

		readonly ICliEnvironment _environment = environment;

		public int ErrorStatusCode { get; private set; } = StatusCodes.INVALID_ARGUMENTS;

		public QueryExecutionSettings? Resolve(QueryExecutionOptionValues values)
		{
			ErrorStatusCode = StatusCodes.INVALID_ARGUMENTS;

			var connectionResolver = new ConnectionSettingsResolver(_environment);
			var connectionSettings = connectionResolver.Resolve(new ConnectionOptionValues(
				values.Config,
				values.Profile,
				values.Provider,
				values.ProviderLocation,
				values.ConnectionString,
				values.ConnectionStringEnv,
				values.User,
				values.UserEnv,
				values.Password,
				values.PasswordEnv,
				values.Impersonate,
				values.ImpersonateMode,
				values.CommandTimeout,
				values.LockTimeout));

			ErrorStatusCode = connectionResolver.ErrorStatusCode;

			if (connectionSettings == null)
				return null;

			var configuration = connectionSettings.Configuration;
			var maxRowsValue  = values.MaxRows != null ? ParseRowCount(QueryExecutionCliOptions.MaxRows, values.MaxRows) : configuration?.MaxRows ?? DefaultMaxRows;
			var outputFormat  = values.Output ?? configuration?.Output ?? values.DefaultOutput;
			var outputFileName       = values.OutputFile != null
				? connectionResolver.ResolvePath(QueryExecutionCliOptions.OutputFile, values.OutputFile)
				: values.UseConfiguredOutputFile
					? connectionResolver.ResolvePath(QueryExecutionCliOptions.OutputFile, configuration?.OutputFile, connectionSettings.ConfigDirectory)
					: null;
			var enableExecute        = configuration?.EnableExecute ?? false;
			var querySql             = values.Sql;
			var querySqlFile         = connectionResolver.ResolvePath(QueryExecutionCliOptions.SqlFile, values.SqlFile);

			if (maxRowsValue < 0)
				return null;

			if (!IsKnownOutputFormat(outputFormat))
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Output.Name}' has unknown value '{outputFormat}'.");
				return null;
			}

			if (string.Equals(outputFileName,       MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(querySqlFile,         MissingEnvironmentVariable, StringComparison.Ordinal))
				return null;

			if (values.Mode == QueryExecutionMode.Execute && !enableExecute)
			{
				_environment.Error.WriteLine($"Profile '{connectionSettings.Profile}' doesn't enable execute mode. Set enableExecute to true in the trusted configuration profile before using write-capable SQL execution.");
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
				connectionSettings.Profile,
				connectionSettings.Provider,
				connectionSettings.ProviderLocation,
				connectionSettings.User,
				connectionSettings.Password,
				connectionSettings.ConnectionString,
				connectionSettings.CommandTimeout,
				connectionSettings.LockTimeout,
				maxRowsValue,
				outputFormat,
				outputFileName,
				values.Overwrite,
				values.Mode,
				enableExecute,
				_environment.Error,
				connectionSettings.Impersonate,
				connectionSettings.ImpersonateMode,
				querySql!);
		}

		static bool IsKnownOutputFormat(string value)
		{
			return string.Equals(value, "json",       StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "json-table", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "csv",        StringComparison.OrdinalIgnoreCase);
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
