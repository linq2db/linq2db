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
	internal sealed class QueryCommand : CliCommand
	{
		private const string DefaultProfileName = "default";

		private static readonly OptionCategory _configurationOptions = new (1, "Configuration", "Configuration options", "configuration");
		private static readonly OptionCategory _connectionOptions    = new (2, "Connection",    "Connection options",    "connection");
		private static readonly OptionCategory _safetyOptions        = new (3, "Safety",        "SQL safety options",    "safety");
		private static readonly OptionCategory _outputOptions        = new (4, "Output",        "Output options",        "output");
		private static readonly OptionCategory _inputOptions         = new (5, "Input",         "SQL input options",     "input");

		private static readonly CliOption Config = new StringCliOption(
			"config",
			null,
			false,
			false,
			"path to query configuration file",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption Profile = new StringCliOption(
			"profile",
			null,
			false,
			false,
			"configuration profile name",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption Provider = new StringCliOption(
			"provider",
			null,
			false,
			false,
			"linq2db provider name",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption ConnectionString = new StringCliOption(
			"connection-string",
			null,
			false,
			false,
			"database connection string; use {0} for user and {1} for password placeholders",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption User = new StringCliOption(
			"user",
			null,
			false,
			false,
			"database user name for connection string formatting",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption Password = new StringCliOption(
			"password",
			null,
			false,
			false,
			"database password for connection string formatting",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption AllowUnsafeSql = new BooleanCliOption(
			"allow-unsafe-sql",
			null,
			false,
			"confirm unsafe SQL execution when configuration unsafeSql policy is confirm; ask the user before using this option",
			null,
			null,
			null,
			false,
			false);

		private static readonly CliOption Output = new StringEnumCliOption(
			"output",
			null,
			false,
			false,
			"output format",
			null,
			null,
			null,
			false,
			new StringEnumOption(true, true, "json", "JSON output"),
			new StringEnumOption(false, false, "csv", "CSV output"));

		private static readonly CliOption OutputFile = new StringCliOption(
			"output-file",
			null,
			false,
			false,
			"path to file for command output",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption Sql = new StringCliOption(
			"sql",
			null,
			false,
			false,
			"SQL query text to execute",
			null,
			null,
			null,
			null,
			null);

		private static readonly CliOption SqlFile = new StringCliOption(
			"sql-file",
			null,
			false,
			false,
			"path to file with SQL query text to execute",
			null,
			null,
			null,
			null,
			null);

		public static CliCommand Instance { get; } = new QueryCommand();

		private QueryCommand()
			: base(
				"query",
				true,
				false,
				"[--config config] [--profile profile] [--provider provider] [--connection-string connection-string] [--user user] [--password password] [--allow-unsafe-sql] [--output output] [--output-file output-file] [--sql sql | --sql-file file]",
				"execute SQL query against specified database",
				new CommandExample[]
				{
					new(
						"dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql \"select * from Person\"",
						"executes specified SQL query and writes JSON result to console"),
					new(
						"dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql-file query.sql",
						"executes SQL query from file and writes JSON result to console"),
					new(
						"dotnet linq2db query --config query.json --profile uat --user readonly --password secret --sql-file query.sql",
						"executes SQL query using connection settings from specified configuration profile"),
					new(
						"dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --output csv --output-file result.csv --sql \"select * from Person\"",
						"executes specified SQL query and writes CSV result to file"),
				})
		{
			AddOption(_configurationOptions, Config);
			AddOption(_configurationOptions, Profile);
			AddOption(_connectionOptions, Provider);
			AddOption(_connectionOptions, ConnectionString);
			AddOption(_connectionOptions, User);
			AddOption(_connectionOptions, Password);
			AddOption(_safetyOptions, AllowUnsafeSql);
			AddOption(_outputOptions, Output);
			AddOption(_outputOptions, OutputFile);
			AddMutuallyExclusiveOptions(_inputOptions, Sql, SqlFile);
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
		private QueryCommandSettings? ProcessOptions(ICliEnvironment environment, Dictionary<CliOption, object?> options)
		{
			options.Remove(Config,   out var config);
			options.Remove(Profile,  out var profile);
			options.Remove(Provider, out var provider);
			options.Remove(ConnectionString, out var connectionString);
			options.Remove(User, out var user);
			options.Remove(Password, out var password);
			options.Remove(AllowUnsafeSql, out var allowUnsafeSql);
			options.Remove(Output,   out var output);
			options.Remove(OutputFile, out var outputFile);
			options.Remove(Sql,     out var sql);
			options.Remove(SqlFile, out var sqlFile);

			var profileName = (string?)profile ?? DefaultProfileName;

			QueryCommandConfiguration? configuration = null;
			if (profile != null && config == null)
			{
				environment.Error.WriteLine($"Option '--{Profile.Name}' requires option '--{Config.Name}'.");
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
			var outputFormat         = (string?)output ?? configuration?.Output ?? "json";
			var outputFileName       = (string?)outputFile ?? configuration?.OutputFile;
			var sqlSafety            = configuration?.SqlSafety ?? QuerySqlSafetyMode.Deny;
			var allowUnsafeSqlValue  = (bool?)allowUnsafeSql ?? false;
			var querySql             = (string?)sql;
			var querySqlFile         = (string?)sqlFile;

			if (providerName == null)
			{
				environment.Error.WriteLine($"Option '--{Provider.Name}' must be specified.");
				return null;
			}

			if (connectionStringText == null)
			{
				environment.Error.WriteLine($"Option '--{ConnectionString.Name}' must be specified.");
				return null;
			}

			connectionStringText = string.Format(CultureInfo.InvariantCulture, connectionStringText, userName, passwordText);

			if (allowUnsafeSqlValue && sqlSafety == QuerySqlSafetyMode.Deny)
			{
				environment.Error.WriteLine($"Option '--{AllowUnsafeSql.Name}' cannot be used because SQL safety policy is 'deny'.");
				return null;
			}

			if (querySql == null && querySqlFile == null)
			{
				environment.Error.WriteLine($"Either '--{Sql.Name}' or '--{SqlFile.Name}' option must be specified.");
				return null;
			}

			return new QueryCommandSettings(profileName, providerName, connectionStringText, outputFormat, outputFileName, sqlSafety, allowUnsafeSqlValue, querySql, querySqlFile);
		}
	}
}
