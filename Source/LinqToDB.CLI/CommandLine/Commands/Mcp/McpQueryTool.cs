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

			Call this tool first when available profiles, providers, SQL dialects, output defaults, row limits, or execute availability are unknown. Use linq2db_schema to inspect database objects. Use linq2db_skill for the full usage guide.

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

			Use it for detailed configuration, provider loading, SQL safety, output, timeout, impersonation, and workflow guidance. This documentation-only tool does not access a database or return secrets.
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

			It reads metadata only, does not read table data or execute SQL, and does not return secrets. Procedures and functions are not supported.
			""")]
		public async Task<CallToolResult> Schema(
			[Description("""
				Optional profile returned by linq2db_info or explicitly provided by the user. If omitted, the server startup/default profile is used. Requires server startup with --config.
				""")]                                                                             string?   profile                     = null,
			[Description("Schema detail level. Allowed values: full, names. Use names for compact object discovery, then request full metadata with filters.")] string? detailLevel = null,
			[Description("Prefer provider-specific .NET types in schema metadata.")]              bool?     preferProviderSpecificTypes = null,
			[Description("Read table and view metadata.")]                                        bool?     getTables                   = null,
			[Description("Read foreign key metadata.")]                                           bool?     getForeignKeys              = null,
			[Description("Map char(1) metadata to string instead of char.")]                      bool?     generateChar1AsString       = null,
			[Description("Ignore SQL Server temporal history tables when provider supports it.")] bool?     ignoreSystemHistoryTables   = null,
			[Description("Default schema name.")]                                                 string?   defaultSchema               = null,
			[Description("Optional schema name filters. Exact names only.")]                      string[]? filterSchemas               = null,
			[Description("Optional catalog name filters. Exact names only.")]                     string[]? filterCatalogs              = null,
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
					detailLevel,
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

			settings = settings with { MaxOutputBytes = _startupOptions.MaxResponseBytes };

			using var resultWriter = new StringWriter(CultureInfo.InvariantCulture);

			var result = await new SchemaInspectionExecutor(settings).Execute(resultWriter, cancellationToken);

			if (result.Error != null)
				return CreateErrorResult(result.Error);

			return new CallToolResult
			{
				Content = [new TextContentBlock { Text = resultWriter.ToString() }],
			};
		}

		[McpServerTool(
			Name        = "linq2db_query",
			Title       = "Execute read-only SQL query",
			ReadOnly    = true,
			Idempotent  = false,
			OpenWorld   = true,
			Destructive = false)]
		[Description("""
			Executes one read-only SQL statement using a configured linq2db profile.

			Call linq2db_info first when the profile or SQL dialect is unknown. Call linq2db_schema when database objects are unknown and call linq2db_skill for detailed guidance.

			Multiple statements and SQL that cannot be classified as read-only are rejected.

			Do not use it for write-capable SQL. Use linq2db_execute only when available and after explicit user approval for the exact operation.
			""")]
		public async Task<CallToolResult> Query(
			[Description("""
				One provider-appropriate read-only SQL statement. Prefer SELECT or WITH. Use explicit aliases for expressions and joins because json output requires unique column names.
				""")] string sql,
			[Description("""
				Optional profile returned by linq2db_info or explicitly provided by the user. If omitted, the server startup/default profile is used. Requires server startup with --config.
				""")] string? profile = null,
			[Description("""
				Optional row limit. Prefer a small value for exploration; use 0 only when the full result set is explicitly needed.
				""")] int? maxRows = null,
			[Description("""
				Optional output: json or json-table. Prefer json-table for metadata, duplicate column names, expressions, or joins; use json for object-shaped rows with unique names.
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
				return CreateErrorResult($"MCP query execution supports only 'json' and 'json-table' output. The selected profile resolves output='{settings.Output}'. Pass output='json-table' or output='json' in the tool call, or update the profile for MCP usage.");

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
				QueryExecutionMode.Query,
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
				_startupOptions.Credentials,
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
