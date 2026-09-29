using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;

using LinqToDB.CommandLine.Commands.Connection;
using LinqToDB.CommandLine.Commands.QueryExecution;

using ModelContextProtocol.Protocol;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// Non-secret MCP query configuration discovery logic.
	/// </summary>
	sealed class McpInfoTool(McpQueryStartupOptions startupOptions, TextWriter diagnostics)
	{
		const string StartupProfileName = "startup";
		const string DefaultOutput      = "json-table";

		static readonly JsonSerializerOptions _jsonSerializerOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

		static readonly string[] _supportedOutputFormats     = ["json", "json-table"];
		static readonly string[] _queryCommandOutputFormats  = ["json", "json-table", "csv"];

		static readonly McpSupportedProviderInfo[] _supportedProviders =
		[
			new("SQL Server",     ["SqlServer"],                       "SQL Server T-SQL",             true,  null),
			new("SQLite",         ["SQLite", "SQLite.MS"],             "SQLite",                       true,  null),
			new("PostgreSQL",     ["PostgreSQL"],                      "PostgreSQL",                   true,  null),
			new("MySQL",          ["MySql", "MySqlConnector"],         "MySQL",                        true,  null),
			new("MariaDB",        ["MariaDB"],                         "MariaDB",                      true,  null),
			new("Oracle Managed", ["Oracle.Managed", "Oracle"],        "Oracle SQL",                   true,  null),
			new("Firebird",       ["Firebird"],                        "Firebird SQL",                 true,  null),
			new("Sybase ASE",     ["Sybase", "Sybase.Managed", "Sybase.Native"], "Sybase ASE T-SQL",    true,  null),
			new("Microsoft Access", ["Access", "Access.Odbc", "Access.Jet.OleDb", "Access.Jet.Odbc", "Access.Ace.OleDb", "Access.Ace.Odbc"], "Microsoft Access SQL", true, "Requires the matching ODBC driver or OLE DB provider to be installed on the host."),
			new("ClickHouse",     ["ClickHouse.Driver", "ClickHouse.Octonica", "ClickHouse.MySql"], "ClickHouse SQL", true, null),
			new("DuckDB",         ["DuckDB"],                          "DuckDB SQL",                   true,  null),
			new("YDB",            ["YDB"],                             "YDB SQL",                      true,  null),
			new("SAP HANA",       ["SapHana", "SapHana.Odbc", "SapHana.Native"], "SAP HANA SQL",        false, "Requires the SAP HANA ODBC driver installed on the host for SapHana.Odbc, or providerLocation pointing to Sap.Data.Hana.Net.v8.0.dll for SapHana.Native."),
			new("SQL Server Compact", ["SqlCe"],                       "SQL Server Compact SQL",       false, "Requires the System.Data.SqlServerCe assembly to be resolvable on the host, or providerLocation pointing to it."),
			new("IBM DB2",        ["DB2", "DB2.LUW", "DB2.z/OS"],      "IBM DB2 SQL",                  false, "Requires providerLocation pointing to IBM.Data.Db2.dll from the Net.IBM.Data.Db2 package."),
			new("IBM Informix",   ["Informix", "Informix.DB2"],        "Informix SQL",                 false, "Informix requires providerLocation pointing to IBM.Data.Informix.dll. Informix.DB2 uses the DB2-based provider and requires IBM.Data.Db2.dll from the Net.IBM.Data.Db2 package."),
		];

		readonly McpQueryStartupOptions _startupOptions = startupOptions;
		readonly TextWriter             _diagnostics    = diagnostics;

		public CallToolResult Info(CancellationToken cancellationToken = default)
		{
			var environment = new McpQueryEnvironment(TextWriter.Null);
			var profiles    = new List<McpProfileInfo>();

			string defaultProfile;
			bool   defaultProfileUsable;

			if (_startupOptions.Config == null)
			{
				defaultProfile = StartupProfileName;

				var profileInfo = CreateProfileInfo(StartupProfileName, null, 1, out var error);

				if (error != null)
					return CreateErrorResult(error);

				if (profileInfo == null)
					return CreateErrorResult("Cannot load linq2db query configuration: provider is not configured.");

				profiles.Add(profileInfo);

				defaultProfileUsable = true;
			}
			else
			{
				if (!QueryExecutionConfiguration.TryLoadProfileNames(environment, _startupOptions.Config, out var profileNames, out var error))
					return CreateErrorResult($"Cannot load linq2db query configuration: {error}");

				defaultProfile = _startupOptions.Profile ?? QueryExecutionDefaults.DefaultProfileName;

				if (_startupOptions.Profile != null && !profileNames.Any(item => string.Equals(item, defaultProfile, StringComparison.Ordinal)))
					return CreateErrorResult($"Cannot load linq2db query configuration: profile '{defaultProfile}' not found.");

				foreach (var profileName in profileNames)
				{
					if (!QueryExecutionConfiguration.TryLoad(environment, _startupOptions.Config, profileName, out var configuration, out error))
						return CreateErrorResult($"Cannot load linq2db query configuration: {error}");

					var profileInfo = CreateProfileInfo(profileName, configuration, profileNames.Count, out error);

					if (error != null)
						return CreateErrorResult(error);

					if (profileInfo != null)
						profiles.Add(profileInfo);
				}

				if (profiles.Count == 0)
					return CreateErrorResult("Cannot load linq2db query configuration: no configured profiles with provider were found.");

				defaultProfileUsable = profiles.Exists(profile => string.Equals(profile.Name, defaultProfile, StringComparison.Ordinal));
			}

			return new CallToolResult
			{
				Content =
				[
					new TextContentBlock
					{
						Text = JsonSerializer.Serialize(new
						{
							server = new
							{
								name               = "linq2db.cli",
								command            = "mcp",
								executeToolEnabled = _startupOptions.EnableExecuteTool,
								maxResponseBytes   = _startupOptions.MaxResponseBytes,
							},
							defaultProfile,
							defaultProfileUsable,
							profiles,
							supportedProviders = _supportedProviders,
							supportedOutputFormats = _supportedOutputFormats,
							queryCommandOutputFormats = _queryCommandOutputFormats,
							rules = new
							{
								singleStatementOnly                            = true,
								multipleStatementsRejected                     = true,
								readOrientedByDefault                          = true,
								queryToolReadOnly                              = true,
								executeToolDisabledByDefault                   = true,
								executeRequiresProfileEnableExecute            = true,
								sqlGuardIsSecurityBoundary                     = false,
								sqlGuardWarning                                = "SQL validation is a best-effort guardrail, not a security boundary. Use restricted database accounts as the primary protection.",
								connectionStringPlaceholdersEscaped            = false,
								connectionStringPlaceholderWarning             = "Connection string {0}/{1} placeholders are formatted with raw user/password values. Use trusted startup/config sources and provider-supported connection string quoting for special characters.",
								providerInputAllowedInToolCall                 = false,
								connectionStringInputAllowedInToolCall         = false,
								credentialsInputAllowedInToolCall              = false,
								providerLocationInputAllowedInToolCall         = false,
								impersonationCredentialsInputAllowedInToolCall = false,
							},
						}, _jsonSerializerOptions),
					},
				],
			};
		}

		McpProfileInfo? CreateProfileInfo(string name, QueryExecutionConfiguration? configuration, int profileCount, out string? error)
		{
			error = null;

			var provider      = _startupOptions.Provider     ?? configuration?.Provider;
			var output        = _startupOptions.Output       ?? configuration?.Output ?? DefaultOutput;
			var enableExecute = configuration?.EnableExecute ?? false;
			var impersonate   = _startupOptions.Impersonate  ?? configuration?.Impersonate ?? false;
			var maxRows       = _startupOptions.MaxRows != null ? ParseRowCount(_startupOptions.MaxRows, out error) : configuration?.MaxRows ?? QueryExecutionDefaults.DefaultMaxRows;

			if (error != null)
				return null!;

			if (provider == null)
			{
				if (!string.Equals(name, QueryExecutionDefaults.DefaultProfileName, StringComparison.Ordinal) || profileCount == 1)
					_diagnostics.WriteLine($"Configuration profile '{name}' doesn't configure provider and will not be returned by linq2db_info.");

				return null;
			}

			error = null;
			return new McpProfileInfo(
				name,
				configuration?.Description,
				provider,
				ProviderDialectCatalog.GetDialect(provider),
				output,
				IsMcpOutputFormat(output),
				maxRows,
				enableExecute,
				impersonate);

			static int ParseRowCount(string value, out string? error)
			{
				if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var rowCount) && rowCount >= 0)
				{
					error = null;
					return rowCount;
				}

				error = "Cannot load linq2db query configuration: option '--max-rows' must be a non-negative integer row count.";
				return -1;
			}
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

		sealed record McpProfileInfo(
			string  Name,
			string? Description,
			string  Provider,
			string  Dialect,
			string  DefaultOutput,
			bool    DefaultOutputSupportedByMcp,
			int     MaxRows,
			bool    EnableExecute,
			bool    ImpersonationEnabled);

		sealed record McpSupportedProviderInfo(
			string   Name,
			string[] ProviderNames,
			string   Dialect,
			bool     Bundled,
			string?  Notes);

	}
}
