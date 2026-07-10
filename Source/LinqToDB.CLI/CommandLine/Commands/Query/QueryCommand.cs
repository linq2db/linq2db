using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Query
{
	/// <summary>
	/// Query command descriptor and CLI option processing.
	/// </summary>
	sealed class QueryCommand : CliCommand
	{
		static readonly OptionCategory _inputOptions = new(5, "Input", "SQL input options", "input");

		public static CliCommand Instance { get; } = new QueryCommand();

		QueryCommand()
			: base(
				"query",
				true,
				false,
				"<options>",
				"execute read-oriented SQL query so agents can analyze code together with live database data",
				[
					new("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql \"select * from Person\"",
						"executes specified read-oriented SQL query and writes JSON result to console"),
					new("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql-file query.sql",
						"executes read-oriented SQL query from file and writes JSON result to console"),
					new("dotnet linq2db query --config query.json --profile uat --command-timeout 30 --sql-file query.sql",
						"executes read-oriented SQL query with a command timeout override"),
					new("dotnet linq2db query --config query.json --profile uat --user readonly --password secret --sql-file query.sql",
						"executes read-oriented SQL query using connection settings from specified configuration profile"),
					new("dotnet linq2db query --config query.json --profile uat --output json-table --sql \"select p.Id, o.Id from Person p join Orders o on o.PersonId = p.Id\"",
						"executes query and writes duplicate-safe JSON table output with column metadata"),
					new("dotnet linq2db query --provider DB2 --provider-location \"C:\\path\\to\\IBM.Data.Db2.dll\" --connection-string \"Server=localhost:50000;Database=testdb;UID=db2inst1;PWD=Password12!\" --sql \"select * from SYSIBM.SYSDUMMY1\"",
						"executes query using an external DB2 provider assembly"),
					new("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --output csv --output-file result.csv --sql \"select * from Person\"",
						"executes specified read-oriented SQL query and writes CSV result to file"),
				])
		{
			AddOption(QueryExecutionCliOptions.ConfigurationOptions, QueryExecutionCliOptions.Config);
			AddOption(QueryExecutionCliOptions.ConfigurationOptions, QueryExecutionCliOptions.Profile);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.Provider);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.ProviderLocation);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.ConnectionString);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.ConnectionStringEnv);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.User);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.UserEnv);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.Password);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.PasswordEnv);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.Impersonate);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.ImpersonateMode);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.CommandTimeout);
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.LockTimeout);
			AddOption(QueryExecutionCliOptions.OutputOptions,        QueryExecutionCliOptions.Output);
			AddOption(QueryExecutionCliOptions.OutputOptions,        QueryExecutionCliOptions.OutputFile);
			AddOption(QueryExecutionCliOptions.OutputOptions,        QueryExecutionCliOptions.Overwrite);
			AddOption(QueryExecutionCliOptions.OutputOptions,        QueryExecutionCliOptions.MaxRows);

			AddMutuallyExclusiveOptions(_inputOptions, QueryExecutionCliOptions.Sql, QueryExecutionCliOptions.SqlFile);
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			var settings = ProcessOptions(environment, options, out var errorStatusCode);

			if (settings == null)
				return errorStatusCode;

			if (options.Count > 0)
			{
				foreach (var kvp in options)
					await environment.Error.WriteLineAsync($"{Name} command miss '{kvp.Key.Name}' option handler").ConfigureAwait(false);

				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			if (settings is { OutputFile: not null, Overwrite: false } && environment.FileExists(settings.OutputFile))
			{
				await environment.Error.WriteLineAsync($"Output file '{settings.OutputFile}' already exists. Use '--overwrite' to replace it.").ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
			}

			// Open the output writer before optional impersonation so file access stays under the original process account.
			//
			var outputWriter        = settings.OutputFile != null ? environment.CreateTextWriter(settings.OutputFile) : environment.Out;
			var disposeOutputWriter = settings.OutputFile != null;

			try
			{
				var result = await new QueryExecutionExecutor(settings).Execute(outputWriter, cancellationToken).ConfigureAwait(false);

				if (result.Error != null)
				{
					await environment.Error.WriteLineAsync(result.Error).ConfigureAwait(false);
					return result.StatusCode;
				}

				if (result.Truncated && !string.Equals(settings.Output, "json-table", StringComparison.OrdinalIgnoreCase))
				{
					// JSON table carries truncation in-band; other formats report it through stderr.
					//
					await environment.Error.WriteLineAsync($"Query result truncated to {settings.MaxRows.ToString(CultureInfo.InvariantCulture)} row(s). Use '--max-rows' to change the limit.").ConfigureAwait(false);
				}

				return result.StatusCode;
			}
			finally
			{
				if (disposeOutputWriter)
					await outputWriter.DisposeAsync().ConfigureAwait(false);
			}
		}

		static QueryExecutionSettings? ProcessOptions(ICliEnvironment environment, Dictionary<CliOption, object?> options, out int errorStatusCode)
		{
			options.Remove(QueryExecutionCliOptions.Config,              out var config);
			options.Remove(QueryExecutionCliOptions.Profile,             out var profile);
			options.Remove(QueryExecutionCliOptions.Provider,            out var provider);
			options.Remove(QueryExecutionCliOptions.ProviderLocation,    out var providerLocation);
			options.Remove(QueryExecutionCliOptions.ConnectionString,    out var connectionString);
			options.Remove(QueryExecutionCliOptions.ConnectionStringEnv, out var connectionStringEnv);
			options.Remove(QueryExecutionCliOptions.User,                out var user);
			options.Remove(QueryExecutionCliOptions.UserEnv,             out var userEnv);
			options.Remove(QueryExecutionCliOptions.Password,            out var password);
			options.Remove(QueryExecutionCliOptions.PasswordEnv,         out var passwordEnv);
			options.Remove(QueryExecutionCliOptions.Impersonate,         out var impersonate);
			options.Remove(QueryExecutionCliOptions.ImpersonateMode,     out var impersonateMode);
			options.Remove(QueryExecutionCliOptions.CommandTimeout,      out var commandTimeout);
			options.Remove(QueryExecutionCliOptions.LockTimeout,         out var lockTimeout);
			options.Remove(QueryExecutionCliOptions.Output,              out var output);
			options.Remove(QueryExecutionCliOptions.OutputFile,          out var outputFile);
			options.Remove(QueryExecutionCliOptions.Overwrite,           out var overwrite);
			options.Remove(QueryExecutionCliOptions.MaxRows,             out var maxRows);
			options.Remove(QueryExecutionCliOptions.Sql,                 out var sql);
			options.Remove(QueryExecutionCliOptions.SqlFile,             out var sqlFile);

			var values = new QueryExecutionOptionValues(
				(string?)config,
				(string?)profile,
				(string?)provider,
				(string?)providerLocation,
				(string?)connectionString,
				(string?)connectionStringEnv,
				(string?)user,
				(string?)userEnv,
				(string?)password,
				(string?)passwordEnv,
				(bool?)  impersonate,
				(string?)impersonateMode,
				(string?)commandTimeout,
				(string?)lockTimeout,
				(string?)maxRows,
				(string?)output,
				(string?)outputFile,
				true,
				(bool?)  overwrite ?? false,
				QueryExecutionMode.Query,
				(string?)sql,
				(string?)sqlFile,
				"json");

			var resolver = new QueryExecutionSettingsResolver(environment);
			var settings = resolver.Resolve(values);

			errorStatusCode = resolver.ErrorStatusCode;
			return settings;
		}
	}
}
