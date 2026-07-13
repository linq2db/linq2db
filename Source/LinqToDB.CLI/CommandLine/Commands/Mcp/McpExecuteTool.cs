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
			Executes one write-capable SQL statement against a database configured by linq2db CLI MCP startup options or query configuration profiles.

			This tool is available only when the MCP server was started with --enable-execute-tool. The selected profile must also set enableExecute to true.

			Use this tool only after explicit user approval for the exact SQL operation. Multiple SQL statements are always rejected.

			The tool cannot accept provider names, connection strings, passwords, provider assembly paths, or impersonation credentials. Those are configured only at MCP server startup or in trusted configuration profiles.
			""")]
		public async Task<CallToolResult> Execute(
			[Description("""
				Single SQL statement to execute.

				Use provider-appropriate SQL syntax based on the selected profile's dialect. Multiple statements are rejected.
				""")] string sql,
			[Description("""
				Optional configuration profile override.

				Use only a profile name returned by linq2db_info or explicitly provided by the user. Do not invent profile names.

				If omitted, the MCP server startup profile or default profile is used.
				Requires MCP server startup with --config.
				""")] string? profile = null,
			[Description("""
				Optional maximum number of result rows to read when the statement returns rows.

				Use a small value for write statements that return data. Use 0 only when the user explicitly needs the full result set.
				""")] int? maxRows = null,
			[Description("""
				Optional output format override.

				Allowed values: json, json-table.

				recordsAffected is reported only in json-table output, and only when the provider returns it; json output never includes it. Use json-table by default for this reason.
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

			settings = settings with { DiagnosticWriter = Console.Error };

			if (!IsMcpOutputFormat(settings.Output))
				return CreateErrorResult($"MCP execute supports only 'json' and 'json-table' output. The selected profile resolves output='{settings.Output}'. Pass output='json-table' or output='json' in the tool call, or update the profile for MCP usage.");

			using var resultWriter = new StringWriter(CultureInfo.InvariantCulture);

			var result = await new QueryExecutionExecutor(settings).Execute(resultWriter, cancellationToken).ConfigureAwait(false);

			if (result.Error != null)
				return CreateErrorResult(result.Error);

			return new CallToolResult
			{
				Content = [new TextContentBlock { Text = resultWriter.ToString() }],
			};
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
				_startupOptions.WindowsCredentials,
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
