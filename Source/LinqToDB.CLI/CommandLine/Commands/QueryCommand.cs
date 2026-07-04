using System;
using System.Collections.Generic;
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
		private static readonly OptionCategory _outputOptions        = new (3, "Output",        "Output options",        "output");
		private static readonly OptionCategory _inputOptions         = new (4, "Input",         "SQL input options",     "input");

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
			"database connection string",
			null,
			null,
			null,
			null,
			null);

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
				"[--config config] [--profile profile] [--provider provider] [--connection-string connection-string] [--output output] [--output-file output-file] [--sql sql | --sql-file file]",
				"execute SQL query against specified database",
				[])
		{
			AddOption(_configurationOptions, Config);
			AddOption(_configurationOptions, Profile);
			AddOption(_connectionOptions, Provider);
			AddOption(_connectionOptions, ConnectionString);
			AddOption(_outputOptions, Output);
			AddOption(_outputOptions, OutputFile);
			AddMutuallyExclusiveOptions(_inputOptions, Sql, SqlFile);
		}

		public override ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			var settings = ProcessOptions(environment, options);

			if (settings == null)
				return new(StatusCodes.INVALID_ARGUMENTS);

			if (options.Count > 0)
			{
				foreach (var kvp in options)
					environment.Error.WriteLine($"{Name} command miss '{kvp.Key.Name}' option handler");

				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			var executor = new QueryCommandExecutor(environment, settings);
			return executor.Execute(cancellationToken);
		}

		private QueryCommandSettings? ProcessOptions(ICliEnvironment environment, Dictionary<CliOption, object?> options)
		{
			options.Remove(Config,   out var config);
			options.Remove(Profile,  out var profile);
			options.Remove(Provider, out var provider);
			options.Remove(ConnectionString, out var connectionString);
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
			var outputFormat         = (string?)output ?? configuration?.Output ?? "json";
			var outputFileName       = (string?)outputFile ?? configuration?.OutputFile;
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

			if (querySql == null && querySqlFile == null)
			{
				environment.Error.WriteLine($"Either '--{Sql.Name}' or '--{SqlFile.Name}' option must be specified.");
				return null;
			}

			return new QueryCommandSettings(profileName, providerName, connectionStringText, outputFormat, outputFileName, querySql, querySqlFile);
		}
	}
}
