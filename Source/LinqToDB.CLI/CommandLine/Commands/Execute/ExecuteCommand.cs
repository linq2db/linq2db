using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Execute
{
	/// <summary>
	/// Execute command descriptor and CLI option processing.
	/// </summary>
	sealed class ExecuteCommand : CliCommand
	{
		static readonly OptionCategory _inputOptions = new(5, "Input", "SQL input options", "input");

		public static CliCommand Instance { get; } = new ExecuteCommand();

		ExecuteCommand()
			: base(
				"execute",
				true,
				false,
				"<options>",
				"execute write-capable SQL statement using a trusted profile with enableExecute set",
				[
					new("dotnet linq2db execute --config query.json --profile dev --sql \"update Person set Name = 'x' where Id = 1\"",
						"executes a write-capable SQL statement only when the selected profile has enableExecute set to true"),
					new("dotnet linq2db execute --config query.json --profile dev --sql-file migration.sql --output json-table",
						"executes a single SQL statement from file and writes JSON table output"),
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
			AddOption(QueryExecutionCliOptions.ConnectionOptions,    QueryExecutionCliOptions.WindowsCredentials);
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
					await environment.Error.WriteLineAsync($"{Name} command missing '{kvp.Key.Name}' option handler").ConfigureAwait(false);

				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			if (settings is { OutputFile: not null, Overwrite: false } && environment.FileExists(settings.OutputFile))
			{
				await environment.Error.WriteLineAsync($"Output file '{settings.OutputFile}' already exists. Use '--overwrite' to replace it.").ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
			}

			var output = CommandOutput.Create(environment, settings.OutputFile);
			await using var _ = output.ConfigureAwait(false);

			var result = await new QueryExecutionExecutor(settings).Execute(output.Writer, cancellationToken).ConfigureAwait(false);

			if (result.Error != null)
			{
				await environment.Error.WriteLineAsync(result.Error).ConfigureAwait(false);
				return result.StatusCode;
			}

			if (!await output.Commit(settings.Overwrite).ConfigureAwait(false))
			{
				await environment.Error.WriteLineAsync($"Output file '{settings.OutputFile}' already exists. Use '--overwrite' to replace it.").ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
			}

			if (result.Truncated && !string.Equals(settings.Output, "json-table", StringComparison.OrdinalIgnoreCase))
			{
				await environment.Error.WriteLineAsync($"Execution result truncated to {settings.MaxRows.ToString(CultureInfo.InvariantCulture)} row(s). Use '--max-rows' to change the limit.").ConfigureAwait(false);
			}

			return result.StatusCode;
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
			options.Remove(QueryExecutionCliOptions.WindowsCredentials,  out var windowsCredentials);
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
				(string?)windowsCredentials,
				(bool?)impersonate,
				(string?)impersonateMode,
				(string?)commandTimeout,
				(string?)lockTimeout,
				(string?)maxRows,
				(string?)output,
				(string?)outputFile,
				true,
				(bool?)overwrite ?? false,
				QueryExecutionMode.Execute,
				(string?)sql,
				(string?)sqlFile,
				"json-table");

			var resolver = new QueryExecutionSettingsResolver(environment);
			var settings = resolver.Resolve(values);

			errorStatusCode = resolver.ErrorStatusCode;
			return settings;
		}
	}
}
