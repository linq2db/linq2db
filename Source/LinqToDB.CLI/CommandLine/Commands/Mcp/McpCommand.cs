using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// MCP STDIO server command descriptor and startup option processing.
	/// </summary>
	sealed class McpCommand : CliCommand
	{
		public static CliCommand Instance { get; } = new McpCommand();

		McpCommand()
			: base(
				"mcp",
				true,
				false,
				"<options>",
				"run STDIO MCP server exposing read-oriented SQL query execution as a model-controlled tool",
				[
					new("dotnet linq2db mcp --config query.json --profile dev",
						"starts MCP server using connection settings from specified configuration profile"),
					new("dotnet linq2db mcp --provider SQLite --connection-string \"Data Source=data.db\"",
						"starts MCP server with direct SQLite connection settings"),
					new("dotnet linq2db mcp --config query.json --profile dev --max-rows 100 --output json-table",
						"starts MCP server with default result limit and output format for tool calls"),
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
			AddOption(QueryExecutionCliOptions.OutputOptions,        QueryExecutionCliOptions.McpOutput);
			AddOption(QueryExecutionCliOptions.OutputOptions,        QueryExecutionCliOptions.MaxRows);
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			var startupOptions = ProcessOptions(options);

			if (options.Count > 0)
			{
				foreach (var kvp in options)
					await environment.Error.WriteLineAsync($"{Name} command miss '{kvp.Key.Name}' option handler").ConfigureAwait(false);

				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			var builder = Host.CreateApplicationBuilder([]);

			builder.Logging.AddConsole(consoleOptions =>
			{
				consoleOptions.LogToStandardErrorThreshold = LogLevel.Trace;
			});

			builder.Services
				.AddMcpServer()
				.WithStdioServerTransport()
				.WithTools(new McpQueryTool(startupOptions));

			await builder.Build().RunAsync(cancellationToken).ConfigureAwait(false);
			return StatusCodes.SUCCESS;
		}

		static McpQueryStartupOptions ProcessOptions(Dictionary<CliOption, object?> options)
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
			options.Remove(QueryExecutionCliOptions.McpOutput,           out var output);
			options.Remove(QueryExecutionCliOptions.MaxRows,             out var maxRows);

			return new McpQueryStartupOptions(
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
				(bool?)impersonate,
				(string?)impersonateMode,
				(string?)commandTimeout,
				(string?)lockTimeout,
				(string?)maxRows,
				(string?)output);
		}
	}
}
