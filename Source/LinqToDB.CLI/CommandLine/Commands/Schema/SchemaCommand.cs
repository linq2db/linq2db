using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.Connection;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Commands.SchemaInspection;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Schema
{
	/// <summary>
	/// Schema command descriptor and CLI option processing.
	/// </summary>
	sealed class SchemaCommand : CliCommand
	{
		public static CliCommand Instance { get; } = new SchemaCommand();

		SchemaCommand()
			: base(
				"schema",
				true,
				false,
				"<options>",
				"inspect database object metadata using linq2db schema providers",
				[
					new("dotnet linq2db schema --provider SQLite --connection-string \"Data Source=data.db\"",
						"writes database schema metadata as JSON"),
					new("dotnet linq2db schema --config query.json --profile dev --filter-schema dbo",
						"writes schema metadata for a configured profile and selected schema"),
					new("dotnet linq2db schema --config query.json --profile dev --get-foreign-keys false --output-file schema.json",
						"writes schema metadata without foreign keys to a JSON file"),
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
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.PreferProviderSpecificTypes);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.GetTables);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.GetForeignKeys);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.GenerateChar1AsString);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.IgnoreSystemHistoryTables);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.DefaultSchema);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.FilterSchema);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.FilterCatalog);
			AddOption(SchemaInspectionCliOptions.SchemaOptions,      SchemaInspectionCliOptions.FilterTable);
			AddOption(SchemaInspectionCliOptions.OutputOptions,      SchemaInspectionCliOptions.Output);
			AddOption(SchemaInspectionCliOptions.OutputOptions,      SchemaInspectionCliOptions.OutputFile);
			AddOption(SchemaInspectionCliOptions.OutputOptions,      SchemaInspectionCliOptions.Overwrite);
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

			var outputWriter        = settings.OutputFile != null ? environment.CreateTextWriter(settings.OutputFile) : environment.Out;
			var disposeOutputWriter = settings.OutputFile != null;

			try
			{
				var result = await new SchemaInspectionExecutor(settings).Execute(outputWriter, cancellationToken).ConfigureAwait(false);

				if (result.Error != null)
				{
					await environment.Error.WriteLineAsync(result.Error).ConfigureAwait(false);
				}

				return result.StatusCode;
			}
			finally
			{
				if (disposeOutputWriter)
					await outputWriter.DisposeAsync().ConfigureAwait(false);
			}
		}

		static SchemaInspectionSettings? ProcessOptions(ICliEnvironment environment, Dictionary<CliOption, object?> options, out int errorStatusCode)
		{
			options.Remove(QueryExecutionCliOptions.  Config,                      out var config);
			options.Remove(QueryExecutionCliOptions.  Profile,                     out var profile);
			options.Remove(QueryExecutionCliOptions.  Provider,                    out var provider);
			options.Remove(QueryExecutionCliOptions.  ProviderLocation,            out var providerLocation);
			options.Remove(QueryExecutionCliOptions.  ConnectionString,            out var connectionString);
			options.Remove(QueryExecutionCliOptions.  ConnectionStringEnv,         out var connectionStringEnv);
			options.Remove(QueryExecutionCliOptions.  User,                        out var user);
			options.Remove(QueryExecutionCliOptions.  UserEnv,                     out var userEnv);
			options.Remove(QueryExecutionCliOptions.  Password,                    out var password);
			options.Remove(QueryExecutionCliOptions.  PasswordEnv,                 out var passwordEnv);
			options.Remove(QueryExecutionCliOptions.  Impersonate,                 out var impersonate);
			options.Remove(QueryExecutionCliOptions.  ImpersonateMode,             out var impersonateMode);
			options.Remove(QueryExecutionCliOptions.  CommandTimeout,              out var commandTimeout);
			options.Remove(SchemaInspectionCliOptions.PreferProviderSpecificTypes, out var preferProviderSpecificTypes);
			options.Remove(SchemaInspectionCliOptions.GetTables,                   out var getTables);
			options.Remove(SchemaInspectionCliOptions.GetForeignKeys,              out var getForeignKeys);
			options.Remove(SchemaInspectionCliOptions.GenerateChar1AsString,       out var generateChar1AsString);
			options.Remove(SchemaInspectionCliOptions.IgnoreSystemHistoryTables,   out var ignoreSystemHistoryTables);
			options.Remove(SchemaInspectionCliOptions.DefaultSchema,               out var defaultSchema);
			options.Remove(SchemaInspectionCliOptions.FilterSchema,                out var filterSchemas);
			options.Remove(SchemaInspectionCliOptions.FilterCatalog,               out var filterCatalogs);
			options.Remove(SchemaInspectionCliOptions.FilterTable,                 out var filterTables);
			options.Remove(SchemaInspectionCliOptions.Output,                      out var output);
			options.Remove(SchemaInspectionCliOptions.OutputFile,                  out var outputFile);
			options.Remove(SchemaInspectionCliOptions.Overwrite,                   out var overwrite);

			var connectionResolver = new ConnectionSettingsResolver(environment);
			var connection = connectionResolver.Resolve(new ConnectionOptionValues(
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
				null));

			if (connection == null)
			{
				errorStatusCode = connectionResolver.ErrorStatusCode;
				return null;
			}

			var schemaResolver = new SchemaInspectionSettingsResolver(environment);
			var settings = schemaResolver.Resolve(
				connection,
				new SchemaInspectionOptionValues(
					(string?)  profile,
					(bool?)    preferProviderSpecificTypes,
					(bool?)    getTables,
					(bool?)    getForeignKeys,
					(bool?)    generateChar1AsString,
					(bool?)    ignoreSystemHistoryTables,
					(string?)  defaultSchema,
					(string[]?)filterSchemas,
					(string[]?)filterCatalogs,
					(string[]?)filterTables,
					(string?)  output,
					(string?)  outputFile,
					(bool?)    overwrite ?? false));

			errorStatusCode = schemaResolver.ErrorStatusCode;
			return settings;
		}
	}
}
