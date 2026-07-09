using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.Connection;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Commands.SchemaInspection;
using LinqToDB.CommandLine.Commands.Skill;

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
			Name        = "linq2db_info",
			Title       = "Get linq2db query configuration",
			ReadOnly    = true,
			Idempotent  = true,
			OpenWorld   = false,
			Destructive = false)]
		[Description("""
			Returns non-secret linq2db MCP query configuration information.

			Use this tool before generating SQL when supported database providers, available profiles, selected providers, SQL dialects, default output format, row limits, or unsafe SQL policy are unknown.

			Use linq2db_schema to inspect database objects for a selected profile.

			Use linq2db_skill for the full linq2db CLI/MCP usage guide, including supported providers and external provider loading instructions.

			This tool never returns connection strings, passwords, provider assembly paths, impersonation credentials, or environment variable values.
			""")]
		public CallToolResult Info(CancellationToken cancellationToken = default)
		{
			return new McpInfoTool(_startupOptions, Console.Error).Info(cancellationToken);
		}

		[McpServerTool(
			Name        = "linq2db_skill",
			Title       = "Get linq2db CLI skill",
			ReadOnly    = true,
			Idempotent  = true,
			OpenWorld   = false,
			Destructive = false)]
		[Description("""
			Returns the full embedded linq2db CLI agent skill as Markdown.

			Use this tool when detailed guidance is needed for linq2db query execution, MCP usage, schema inspection, configuration profiles, supported database providers, provider names, external provider loading, SQL safety rules, output formats, row limits, timeouts, impersonation, or agent responsibilities.

			This tool returns documentation only. It does not access a database, read configuration, read environment variables, or return secrets.
			""")]
		public CallToolResult Skill(CancellationToken cancellationToken = default)
		{
			return new CallToolResult
			{
				Content = [new TextContentBlock { Text = SkillResource.ReadMarkdown() }],
			};
		}

		[McpServerTool(
			Name        = "linq2db_schema",
			Title       = "Get database schema",
			ReadOnly    = true,
			Idempotent  = true,
			OpenWorld   = true,
			Destructive = false)]
		[Description("""
			Returns provider-independent database schema metadata for the selected linq2db query/MCP profile.

			Use this tool before generating SQL when table names, column names, keys, relationships, schemas, or catalogs are unknown.

			This tool reads database metadata only. It does not read table data, execute user-provided SQL, modify schema, or return secrets.

			Procedures and functions are not supported.

			Provider, connection string, credentials, impersonation, provider assembly location, and timeout setup are configured only at MCP server startup or in trusted configuration profiles.
			""")]
		public async Task<CallToolResult> Schema(
			[Description("""
				Optional configuration profile override.

				Use only a profile name returned by linq2db_info or explicitly provided by the user. Do not invent profile names.

				If omitted, the MCP server startup profile or default profile is used.
				Requires MCP server startup with --config.
				""")]                                                                             string?   profile                     = null,
			[Description("Prefer provider-specific .NET types in schema metadata.")]              bool?     preferProviderSpecificTypes = null,
			[Description("Read table and view metadata.")]                                        bool?     getTables                   = null,
			[Description("Read foreign key metadata.")]                                           bool?     getForeignKeys              = null,
			[Description("Map char(1) metadata to string instead of char.")]                      bool?     generateChar1AsString       = null,
			[Description("Ignore SQL Server temporal history tables when provider supports it.")] bool?     ignoreSystemHistoryTables   = null,
			[Description("Default schema name.")]                                                 string?   defaultSchema               = null,
			[Description("Optional schema name filters. Exact names only.")]                      string[]? filterSchemas              = null,
			[Description("Optional catalog name filters. Exact names only.")]                     string[]? filterCatalogs             = null,
			[Description("Optional table or view name filters. Matches name, schema.name, or catalog.schema.name. Use regex: or rx: prefix for regular expressions.")] string[]? filterTables = null,
			CancellationToken cancellationToken = default)
		{
			var errorWriter        = new StringWriter(CultureInfo.InvariantCulture);
			var environment        = new McpQueryEnvironment(errorWriter);
			var connectionResolver = new ConnectionSettingsResolver(environment);
			var connection         = connectionResolver.Resolve(CreateConnectionOptionValues(profile));

			if (connection == null)
				return CreateErrorResult(errorWriter.ToString());

			var settings = new SchemaInspectionSettingsResolver(environment).Resolve(
				connection,
				new SchemaInspectionOptionValues(
					profile,
					preferProviderSpecificTypes,
					getTables,
					getForeignKeys,
					generateChar1AsString,
					ignoreSystemHistoryTables,
					defaultSchema,
					filterSchemas,
					filterCatalogs,
					filterTables,
					null,
					null,
					false));

			if (settings == null)
				return CreateErrorResult(errorWriter.ToString());

			using var resultWriter = new StringWriter(CultureInfo.InvariantCulture);
			var result = await new SchemaInspectionExecutor(settings).Execute(resultWriter, cancellationToken).ConfigureAwait(false);

			if (result.Error != null)
				return CreateErrorResult(result.Error);

			return new CallToolResult
			{
				Content = [new TextContentBlock { Text = resultWriter.ToString() }],
			};
		}

		[McpServerTool(
			Name        = "linq2db_query",
			Title       = "Execute linq2db SQL query",
			OpenWorld   = true)]
		[Description("""
			Executes one SQL statement against a database configured by linq2db CLI MCP startup options or query configuration profiles.

			The SQL dialect is determined by the selected profile/provider. Call linq2db_info first if available profiles, providers, or SQL dialects are unknown. Call linq2db_schema before generating SQL when table names, column names, keys, or relationships are unknown. Call linq2db_skill when detailed linq2db CLI/MCP usage guidance is needed.

			Use this tool for read-oriented database inspection, diagnostics, schema/data exploration, row counts, sample records, data-quality checks, and investigation workflows that require live database facts.

			Multiple SQL statements are always rejected. SQL safety validation is a guardrail, not a security boundary.

			The tool cannot accept provider names, connection strings, passwords, provider assembly paths, or impersonation credentials. Those are configured only at MCP server startup or in trusted configuration profiles.

			Do not use this tool for INSERT, UPDATE, DELETE, MERGE, DDL, stored procedure execution, administrative commands, or other write/destructive operations unless the user explicitly approved that exact operation and the selected profile allows unsafe SQL confirmation.
			""")]
		public async Task<CallToolResult> Query(
			[Description("""
				Single SQL statement to execute.

				Use provider-appropriate SQL syntax based on the selected profile's dialect. Call linq2db_info first if the dialect is unknown.

				Prefer SELECT or WITH queries for inspection. Multiple statements are rejected.

				Use explicit column aliases for expressions and joins because json output requires unique column names.
				""")] string sql,
			[Description("""
				Optional configuration profile override.

				Use only a profile name returned by linq2db_info or explicitly provided by the user. Do not invent profile names.

				If omitted, the MCP server startup profile or default profile is used.
				Requires MCP server startup with --config.
				""")] string? profile = null,
			[Description("""
				Optional maximum number of result rows to read.

				Use a small value for exploratory queries. Use 0 only when the user explicitly needs the full result set.
				""")] int? maxRows = null,
			[Description("""
				Optional output format override.

				Allowed values: json, json-table.

				Use json-table by default when column metadata, duplicate column names, expressions, or joins are involved.
				Use json only when object-shaped rows with unique column names are preferred.
				""")] string? output = null,
			[Description("""
				Unsafe SQL confirmation flag.

				Set this to true only after explicit user approval for this exact SQL operation.
				This flag is honored only when the selected profile uses unsafeSql=confirm.
				Never set this flag for normal read-only inspection queries.
				""")] bool allowUnsafeSql = false,
			CancellationToken cancellationToken = default)
		{
			var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
			var environment = new McpQueryEnvironment(errorWriter);
			var values      = CreateOptionValues(sql, profile, maxRows, output, allowUnsafeSql);
			var resolver    = new QueryExecutionSettingsResolver(environment);
			var settings    = resolver.Resolve(values);

			if (settings == null)
				return CreateErrorResult(errorWriter.ToString());

			settings = settings with { DiagnosticWriter = Console.Error };

			if (!IsMcpOutputFormat(settings.Output))
				return CreateErrorResult($"MCP query execution supports only 'json' and 'json-table' output. The selected profile resolves output='{settings.Output}'. Pass output='json-table' or output='json' in the tool call, or update the profile for MCP usage.");

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

		ConnectionOptionValues CreateConnectionOptionValues(string? profile)
		{
			return new ConnectionOptionValues(
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
				_startupOptions.LockTimeout);
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
