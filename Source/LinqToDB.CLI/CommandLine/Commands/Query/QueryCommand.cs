using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Query command descriptor and CLI option processing.
	/// </summary>
	sealed class QueryCommand : CliCommand
	{
		const string DefaultProfileName = "default";
		const int    DefaultMaxRows     = 1000;

		static readonly OptionCategory _configurationOptions = new (1, "Configuration", "Configuration options", "configuration");
		static readonly OptionCategory _connectionOptions    = new (2, "Connection",    "Connection options",    "connection");
		static readonly OptionCategory _safetyOptions        = new (3, "Safety",        "SQL safety options",    "safety");
		static readonly OptionCategory _outputOptions        = new (4, "Output",        "Output options",        "output");
		static readonly OptionCategory _inputOptions         = new (5, "Input",         "SQL input options",     "input");

		static readonly CliOption _config           = new StringCliOption("config",            null, false, false, "path to query configuration file");
		static readonly CliOption _profile          = new StringCliOption("profile",           null, false, false, "configuration profile name");
		static readonly CliOption _provider         = new StringCliOption("provider",          null, false, false, "linq2db provider name");
		static readonly CliOption _connectionString = new StringCliOption("connection-string", null, false, false, "database connection string; use {0} for user and {1} for password placeholders");
		static readonly CliOption _user             = new StringCliOption("user",              null, false, false, "database user name for connection string formatting");
		static readonly CliOption _password         = new StringCliOption("password",          null, false, false, "database password for connection string formatting");
		static readonly CliOption _commandTimeout   = new StringCliOption("command-timeout",   null, false, false, "SQL command timeout in seconds");
		static readonly CliOption _lockTimeout      = new StringCliOption("lock-timeout",      null, false, false, "provider-specific lock wait timeout in seconds");
		static readonly CliOption _maxRows          = new StringCliOption("max-rows",          null, false, false, "maximum number of result rows to read");
		static readonly CliOption _outputFile       = new StringCliOption("output-file",       null, false, false, "path to file for command output");
		static readonly CliOption _sql              = new StringCliOption("sql",               null, false, false, "SQL query text to execute");
		static readonly CliOption _sqlFile          = new StringCliOption("sql-file",          null, false, false, "path to file with SQL query text to execute");

		static readonly CliOption _allowUnsafeSql = new BooleanCliOption(
			"allow-unsafe-sql",
			null,
			false,
			"confirm unsafe SQL execution when configuration unsafeSql policy is confirm; ask the user before using this option",
			null,
			null,
			null,
			false,
			false);

		static readonly CliOption _output = new StringEnumCliOption(
			"output",
			null,
			false,
			false,
			"output format",
			null,
			null,
			null,
			false,
			new StringEnumOption(true,  true,  "json",       "JSON output"),
			new StringEnumOption(false, false, "json-table", "JSON table output"),
			new StringEnumOption(false, false, "csv",        "CSV output"));

		public static CliCommand Instance { get; } = new QueryCommand();

		QueryCommand()
			: base(
				"query",
				true,
				false,
				"<options>",
				"execute SQL query so agents can analyze code together with live database data",
				[
					new("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql \"select * from Person\"",
						"executes specified SQL query and writes JSON result to console"),
					new("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql-file query.sql",
						"executes SQL query from file and writes JSON result to console"),
					new("dotnet linq2db query --config query.json --profile uat --command-timeout 30 --sql-file query.sql",
						"executes SQL query with a command timeout override"),
					new("dotnet linq2db query --config query.json --profile uat --user readonly --password secret --sql-file query.sql",
						"executes SQL query using connection settings from specified configuration profile"),
					new("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --output csv --output-file result.csv --sql \"select * from Person\"",
						"executes specified SQL query and writes CSV result to file"),
				])
		{
			AddOption(_configurationOptions, _config);
			AddOption(_configurationOptions, _profile);
			AddOption(_connectionOptions,    _provider);
			AddOption(_connectionOptions,    _connectionString);
			AddOption(_connectionOptions,    _user);
			AddOption(_connectionOptions,    _password);
			AddOption(_connectionOptions,    _commandTimeout);
			AddOption(_connectionOptions,    _lockTimeout);
			AddOption(_safetyOptions,        _allowUnsafeSql);
			AddOption(_outputOptions,        _output);
			AddOption(_outputOptions,        _outputFile);
			AddOption(_outputOptions,        _maxRows);

			AddMutuallyExclusiveOptions(_inputOptions, _sql, _sqlFile);
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			var settings = ProcessOptions(environment, options);

			if (settings == null)
				return StatusCodes.INVALID_ARGUMENTS;

			if (options.Count > 0)
			{
				foreach (var kvp in options)
					await environment.Error.WriteLineAsync($"{Name} command miss '{kvp.Key.Name}' option handler").ConfigureAwait(false);

				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			var executor = new QueryCommandExecutor(environment, settings);

			return await executor.Execute(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Applies query option precedence: command line values override selected profile values, selected profile
		/// values override the default profile values, and built-in defaults are applied last.
		/// SQL text and SQL file are command-line only options and are not loaded from configuration profiles.
		/// </summary>
		QueryCommandSettings? ProcessOptions(ICliEnvironment environment, Dictionary<CliOption, object?> options)
		{
			options.Remove(_config,           out var config);
			options.Remove(_profile,          out var profile);
			options.Remove(_provider,         out var provider);
			options.Remove(_connectionString, out var connectionString);
			options.Remove(_user,             out var user);
			options.Remove(_password,         out var password);
			options.Remove(_commandTimeout,   out var commandTimeout);
			options.Remove(_lockTimeout,      out var lockTimeout);
			options.Remove(_allowUnsafeSql,   out var allowUnsafeSql);
			options.Remove(_output,           out var output);
			options.Remove(_outputFile,       out var outputFile);
			options.Remove(_maxRows,          out var maxRows);
			options.Remove(_sql,              out var sql);
			options.Remove(_sqlFile,          out var sqlFile);

			var profileName = (string?)profile ?? DefaultProfileName;

			QueryCommandConfiguration? configuration = null;
			if (profile != null && config == null)
			{
				environment.Error.WriteLine($"Option '--{_profile.Name}' requires option '--{_config.Name}'.");
				return null;
			}

			if (config != null && !QueryCommandConfiguration.TryLoad(environment, (string)config, profileName, out configuration, out var error))
			{
				environment.Error.WriteLine(error);
				return null;
			}

			var providerName         = (string?)provider ?? configuration?.Provider;
			var connectionStringText = (string?)connectionString ?? configuration?.ConnectionString;
			var userName             = (string?)user ?? configuration?.User;
			var passwordText         = (string?)password ?? configuration?.Password;
			var commandTimeoutValue  = (string?)commandTimeout != null ? ParseTimeout(environment, _commandTimeout,    (string)commandTimeout)    : configuration?.CommandTimeout;
			var lockTimeoutValue     = (string?)lockTimeout    != null ? ParseTimeout(environment, _lockTimeout,       (string)lockTimeout)       : configuration?.LockTimeout;
			var maxRowsValue         = (string?)maxRows        != null ? ParseRowCount(environment, _maxRows,          (string)maxRows)           : configuration?.MaxRows ?? DefaultMaxRows;
			var outputFormat         = (string?)output ?? configuration?.Output ?? "json";
			var outputFileName       = (string?)outputFile ?? configuration?.OutputFile;
			var sqlSafety            = configuration?.SqlSafety ?? QuerySqlSafetyMode.Deny;
			var allowUnsafeSqlValue  = (bool?)allowUnsafeSql ?? false;
			var querySql             = (string?)sql;
			var querySqlFile         = (string?)sqlFile;

			if (commandTimeoutValue < 0 || lockTimeoutValue < 0 || maxRowsValue < 0)
				return null;

			if (providerName == null)
			{
				environment.Error.WriteLine($"Option '--{_provider.Name}' must be specified.");
				return null;
			}

			if (connectionStringText == null)
			{
				environment.Error.WriteLine($"Option '--{_connectionString.Name}' must be specified.");
				return null;
			}

			connectionStringText = string.Format(CultureInfo.InvariantCulture, connectionStringText, userName, passwordText);

			if (allowUnsafeSqlValue && sqlSafety == QuerySqlSafetyMode.Deny)
			{
				environment.Error.WriteLine($"Option '--{_allowUnsafeSql.Name}' cannot be used because SQL safety policy is 'deny'.");
				return null;
			}

			if (querySql == null && querySqlFile == null)
			{
				environment.Error.WriteLine($"Either '--{_sql.Name}' or '--{_sqlFile.Name}' option must be specified.");
				return null;
			}

			return new QueryCommandSettings(
				profileName,
				providerName,
				connectionStringText,
				commandTimeoutValue,
				lockTimeoutValue,
				maxRowsValue,
				outputFormat,
				outputFileName,
				sqlSafety,
				allowUnsafeSqlValue,
				querySql,
				querySqlFile);
		}

		static int? ParseTimeout(ICliEnvironment environment, CliOption option, string value)
		{
			if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var timeout) && timeout >= 0)
				return timeout;

			environment.Error.WriteLine($"Option '--{option.Name}' must be a non-negative integer number of seconds.");
			return -1;
		}

		static int ParseRowCount(ICliEnvironment environment, CliOption option, string value)
		{
			if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var rowCount) && rowCount >= 0)
				return rowCount;

			environment.Error.WriteLine($"Option '--{option.Name}' must be a non-negative integer row count.");
			return -1;
		}
	}
}
