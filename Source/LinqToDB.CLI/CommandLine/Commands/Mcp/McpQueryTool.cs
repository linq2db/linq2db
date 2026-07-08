using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.QueryExecution;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// MCP tool adapter for shared query execution.
	/// </summary>
	[McpServerToolType]
	sealed class McpQueryTool(McpQueryStartupOptions startupOptions)
	{
		readonly McpQueryStartupOptions _startupOptions = startupOptions;

		[McpServerTool(
			Name        = "linq2db_query",
			Title       = "Execute linq2db SQL query",
			ReadOnly    = true,
			Idempotent  = true,
			OpenWorld   = true,
			Destructive = false)]
		[Description("Executes a single read-oriented SQL query using linq2db CLI startup configuration.")]
		public async Task<CallToolResult> Query(
			[Description("Single SQL query text to execute. Multiple SQL statements are rejected.")]                       string  sql,
			[Description("Optional configuration profile override. Requires startup --config.")]                           string? profile = null,
			[Description("Optional maximum number of result rows to read. Use 0 to disable the limit.")]                   int?    maxRows = null,
			[Description("Optional output format override: json, json-table, or csv.")]                                    string? output  = null,
			[Description("Confirms unsafe SQL execution when the selected configuration profile uses unsafeSql=confirm.")] bool    allowUnsafeSql = false,
			CancellationToken cancellationToken = default)
		{
			var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
			var environment = new McpQueryEnvironment(errorWriter);
			var values      = CreateOptionValues(sql, profile, maxRows, output, allowUnsafeSql);
			var resolver    = new QueryExecutionSettingsResolver(environment);
			var settings    = resolver.Resolve(values);

			if (settings == null)
				return CreateErrorResult(errorWriter.ToString());

			using var resultWriter = new StringWriter(CultureInfo.InvariantCulture);
			var result = await new QueryExecutionExecutor(settings).Execute(resultWriter, cancellationToken).ConfigureAwait(false);

			if (result.Error != null)
				return CreateErrorResult(result.Error);

			return new CallToolResult
			{
				Content = [new TextContentBlock { Text = resultWriter.ToString() }],
			};
		}

		QueryExecutionOptionValues CreateOptionValues(string sql, string? profile, int? maxRows, string? output, bool allowUnsafeSql)
		{
			return new QueryExecutionOptionValues(
				_startupOptions.Config,
				profile ?? _startupOptions.Profile,
				_startupOptions.Provider,
				_startupOptions.ProviderLocation,
				_startupOptions.ConnectionString,
				_startupOptions.ConnectionStringEnv,
				_startupOptions.User,
				_startupOptions.UserEnv,
				_startupOptions.Password,
				_startupOptions.PasswordEnv,
				_startupOptions.Impersonate,
				_startupOptions.ImpersonateMode,
				_startupOptions.CommandTimeout,
				_startupOptions.LockTimeout,
				maxRows?.ToString(CultureInfo.InvariantCulture) ?? _startupOptions.MaxRows,
				output ?? _startupOptions.Output,
				null,
				false,
				false,
				allowUnsafeSql,
				sql,
				null,
				"json-table");
		}

		static CallToolResult CreateErrorResult(string message)
		{
			return new CallToolResult
			{
				IsError = true,
				Content = [new TextContentBlock { Text = message.Trim() }],
			};
		}
	}
}
