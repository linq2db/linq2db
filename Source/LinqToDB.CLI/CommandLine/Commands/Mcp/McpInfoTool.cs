using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

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
		const string DefaultProfileName = "default";
		const int    DefaultMaxRows     = 1000;
		const string DefaultOutput      = "json-table";

		static readonly JsonSerializerOptions _jsonSerializerOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

		static readonly string[] _supportedOutputFormats     = ["json", "json-table"];
		static readonly string[] _queryCommandOutputFormats  = ["json", "json-table", "csv"];

		static readonly McpSupportedProviderInfo[] _supportedProviders =
		[
			new("SQL Server",     ["SqlServer"],                         "SQL Server T-SQL",           true,  null),
			new("SQLite",         ["SQLite", "SQLite.MS"],               "SQLite",                     true,  null),
			new("PostgreSQL",     ["PostgreSQL"],                        "PostgreSQL",                 true,  null),
			new("MySQL",          ["MySql"],                             "MySQL",                      true,  null),
			new("MariaDB",        ["MariaDB"],                           "MariaDB",                    true,  null),
			new("Oracle Managed", ["Oracle.Managed", "Oracle"],         "Oracle SQL",                 true,  null),
			new("Firebird",       ["Firebird"],                          "Firebird SQL",               true,  null),
			new("Sybase ASE",     ["Sybase", "Sybase.Managed", "ASE"], "Sybase ASE T-SQL",           true,  null),
			new("ODBC",           ["ODBC", "Odbc"],                     "ODBC provider-specific SQL", true,  "Requires the matching ODBC driver to be installed on the host."),
			new("OLE DB",         ["OLEDB", "OleDb"],                   "OLE DB provider-specific SQL", true, "Requires the matching OLE DB provider to be installed on the host."),
			new("ClickHouse",     ["ClickHouse.Driver", "ClickHouse.Octonica", "ClickHouse.MySql"], "ClickHouse SQL", true, null),
			new("DuckDB",         ["DuckDB"],                            "DuckDB SQL",                 true,  null),
			new("YDB",            ["YDB"],                               "YDB SQL",                    true,  null),
			new("IBM DB2",        ["DB2", "DB2.LUW", "DB2.z/OS"],      "IBM DB2 SQL",                false, "Requires providerLocation pointing to IBM.Data.Db2.dll from the Net.IBM.Data.Db2 package."),
			new("IBM Informix",   ["Informix", "Informix.DB2"],         "Informix SQL",               false, "Requires providerLocation pointing to IBM.Data.Db2.dll from the Net.IBM.Data.Db2 package."),
		];

		readonly McpQueryStartupOptions _startupOptions = startupOptions;
		readonly TextWriter             _diagnostics    = diagnostics;

		public CallToolResult Info(CancellationToken cancellationToken = default)
		{
			var environment = new McpQueryEnvironment(TextWriter.Null);
			var profiles    = new List<McpProfileInfo>();
			string defaultProfile;
			bool defaultProfileUsable;

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

				defaultProfile = _startupOptions.Profile ?? DefaultProfileName;

				if (!Contains(profileNames, defaultProfile))
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

				defaultProfileUsable = ContainsProfile(profiles, defaultProfile);
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
								name    = "linq2db.cli",
								command = "mcp",
							},
							defaultProfile,
							defaultProfileUsable,
							profiles,
							supportedProviders = _supportedProviders,
							supportedOutputFormats = _supportedOutputFormats,
							queryCommandOutputFormats = _queryCommandOutputFormats,
							rules = new
							{
								singleStatementOnly                         = true,
								multipleStatementsRejected                  = true,
								readOrientedByDefault                       = true,
								sqlGuardIsSecurityBoundary                  = false,
								sqlGuardWarning                             = "SQL validation is a best-effort guardrail, not a security boundary. Use restricted database accounts as the primary protection.",
								connectionStringPlaceholdersEscaped          = false,
								connectionStringPlaceholderWarning           = "Connection string {0}/{1} placeholders are formatted with raw user/password values. Use trusted startup/config sources and provider-supported connection string quoting for special characters.",
								providerInputAllowedInToolCall              = false,
								connectionStringInputAllowedInToolCall      = false,
								credentialsInputAllowedInToolCall           = false,
								providerLocationInputAllowedInToolCall      = false,
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

			var provider    = _startupOptions.Provider ?? configuration?.Provider;
			var output      = _startupOptions.Output ?? configuration?.Output ?? DefaultOutput;
			var maxRows     = _startupOptions.MaxRows != null ? ParseRowCount(_startupOptions.MaxRows, out error) : configuration?.MaxRows ?? DefaultMaxRows;
			var impersonate = _startupOptions.Impersonate ?? configuration?.Impersonate ?? false;

			if (error != null)
				return null!;

			if (provider == null)
			{
				if (!string.Equals(name, DefaultProfileName, StringComparison.Ordinal) || profileCount == 1)
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
				(configuration?.UnsafeSqlPolicy ?? UnsafeSqlPolicy.Deny).ToString().ToLowerInvariant(),
				impersonate);
		}

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

		static bool Contains(IReadOnlyList<string> values, string value)
		{
			foreach (var item in values)
			{
				if (string.Equals(item, value, StringComparison.Ordinal))
					return true;
			}

			return false;
		}

		static bool ContainsProfile(IReadOnlyList<McpProfileInfo> profiles, string name)
		{
			foreach (var profile in profiles)
			{
				if (string.Equals(profile.Name, name, StringComparison.Ordinal))
					return true;
			}

			return false;
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
			string  UnsafeSqlPolicy,
			bool    ImpersonationEnabled);

		sealed record McpSupportedProviderInfo(
			string   Name,
			string[] ProviderNames,
			string   Dialect,
			bool     Bundled,
			string?  Notes);
	}
}
