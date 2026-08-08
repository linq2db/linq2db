using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// MCP tool adapter for write-capable SQL execution.
	/// </summary>
	[McpServerToolType]
	sealed class McpExecuteTool(McpQueryStartupOptions startupOptions)
	{
		readonly McpQueryStartupOptions _startupOptions = startupOptions;

		[McpServerTool(
			Name        = "linq2db_execute",
			Title       = "Execute write-capable SQL statement",
			ReadOnly    = false,
			Idempotent  = false,
			OpenWorld   = true,
			Destructive = true)]
		[Description("""
			Executes one write-capable SQL statement using a configured linq2db profile.

			Requires server startup with --enable-execute-tool and enableExecute=true in the selected profile. Use only after explicit user approval for the exact operation. Multiple statements are rejected.
			""")]
		public async Task<CallToolResult> Execute(
			[Description("""
				One provider-appropriate write-capable SQL statement. Multiple statements are rejected.
				""")] string sql,
			[Description("""
				Optional profile returned by linq2db_info or explicitly provided by the user. If omitted, the server startup/default profile is used. Requires server startup with --config.
				""")] string? profile = null,
			[Description("""
				Optional row limit for returned data. Prefer a small value; use 0 only when the full result set is explicitly needed.
				""")] int? maxRows = null,
			[Description("""
				Optional output: json or json-table. Prefer json-table because it includes recordsAffected when the provider returns it.
				""")] string? output = null,
			CancellationToken cancellationToken = default)
		{
			var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
			var environment = new McpQueryEnvironment(errorWriter);
			var values      = CreateOptionValues(sql, profile, maxRows, output);
			var resolver    = new QueryExecutionSettingsResolver(environment);
			var settings    = resolver.Resolve(values);

			if (settings == null)
				return CreateErrorResult(errorWriter.ToString());

			settings = settings with
			{
				DiagnosticWriter = Console.Error,
				MaxOutputBytes   = _startupOptions.MaxResponseBytes,
			};

			if (!IsMcpOutputFormat(settings.Output))
				return CreateErrorResult($"MCP execute supports only 'json' and 'json-table' output. The selected profile resolves output='{settings.Output}'. Pass output='json-table' or output='json' in the tool call, or update the profile for MCP usage.");

			using var resultWriter = new StringWriter(CultureInfo.InvariantCulture);

			var result = await new QueryExecutionExecutor(settings).Execute(resultWriter, cancellationToken);

			if (result.Error != null)
				return CreateErrorResult(result.Error);

			return McpQueryExecutionResult.Create(resultWriter.ToString(), result, _startupOptions.MaxResponseBytes);
		}

		QueryExecutionOptionValues CreateOptionValues(string sql, string? profile, int? maxRows, string? output)
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
				_startupOptions.Credentials,
				_startupOptions.Impersonate,
				_startupOptions.ImpersonateMode,
				_startupOptions.CommandTimeout,
				_startupOptions.LockTimeout,
				maxRows?.ToString(CultureInfo.InvariantCulture) ?? _startupOptions.MaxRows,
				output ?? _startupOptions.Output,
				null,
				false,
				false,
				QueryExecutionMode.Execute,
				sql,
				null,
				"json-table");
		}

		static bool IsMcpOutputFormat(string output)
		{
			return string.Equals(output, "json",       StringComparison.OrdinalIgnoreCase)
				|| string.Equals(output, "json-table", StringComparison.OrdinalIgnoreCase);
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
