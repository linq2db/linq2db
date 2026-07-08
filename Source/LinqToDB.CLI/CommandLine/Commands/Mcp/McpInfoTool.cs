using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

using LinqToDB.CommandLine.Commands.QueryExecution;

using ModelContextProtocol.Protocol;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// Non-secret MCP query configuration discovery logic.
	/// </summary>
	sealed class McpInfoTool(McpQueryStartupOptions startupOptions)
	{
		const string StartupProfileName = "startup";
		const string DefaultProfileName = "default";
		const int    DefaultMaxRows     = 1000;
		const string DefaultOutput      = "json-table";

		static readonly JsonSerializerOptions _jsonSerializerOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

		static readonly string[] _supportedOutputFormats = ["json", "json-table"];

		readonly McpQueryStartupOptions _startupOptions = startupOptions;

		public CallToolResult Info(CancellationToken cancellationToken = default)
		{
			var environment = new McpQueryEnvironment(TextWriter.Null);
			var profiles    = new List<McpProfileInfo>();
			string defaultProfile;

			if (_startupOptions.Config == null)
			{
				defaultProfile = StartupProfileName;

				var profile = CreateProfileInfo(StartupProfileName, null, out var error);

				if (error != null)
					return CreateErrorResult(error);

				profiles.Add(profile);
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

					var profile = CreateProfileInfo(profileName, configuration, out error);

					if (error != null)
						return CreateErrorResult(error);

					profiles.Add(profile);
				}
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
							profiles,
							supportedOutputFormats = _supportedOutputFormats,
							rules = new
							{
								singleStatementOnly                         = true,
								multipleStatementsRejected                  = true,
								readOrientedByDefault                       = true,
								sqlGuardIsSecurityBoundary                  = false,
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

		McpProfileInfo CreateProfileInfo(string name, QueryExecutionConfiguration? configuration, out string? error)
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
				error = "Cannot load linq2db query configuration: provider is not configured.";
				return null!;
			}

			error = null;
			return new McpProfileInfo(
				name,
				configuration?.Description,
				provider,
				GetDialectName(provider),
				output,
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

		static string GetDialectName(string providerName)
		{
			if (IsProvider(providerName, "SqlServer"))  return "SQL Server T-SQL";
			if (IsProvider(providerName, "SQLite"))     return "SQLite";
			if (IsProvider(providerName, "PostgreSQL")) return "PostgreSQL";
			if (IsProvider(providerName, "MySql"))      return "MySQL";
			if (IsProvider(providerName, "MariaDB"))    return "MariaDB";
			if (IsProvider(providerName, "Oracle"))     return "Oracle SQL";
			if (IsProvider(providerName, "Firebird"))   return "Firebird SQL";
			if (IsProvider(providerName, "DB2"))        return "IBM DB2 SQL";
			if (IsProvider(providerName, "Informix"))   return "Informix SQL";
			if (IsProvider(providerName, "ClickHouse")) return "ClickHouse SQL";
			if (IsProvider(providerName, "DuckDB"))     return "DuckDB SQL";
			if (IsProvider(providerName, "Sybase") || string.Equals(providerName, "ASE", StringComparison.OrdinalIgnoreCase)) return "Sybase ASE T-SQL";
			if (IsProvider(providerName, "Access"))     return "Microsoft Access SQL";
			if (IsProvider(providerName, "ODBC") || IsProvider(providerName, "Odbc")) return "ODBC provider-specific SQL";
			if (IsProvider(providerName, "OLEDB") || IsProvider(providerName, "OleDb")) return "OLE DB provider-specific SQL";
			if (IsProvider(providerName, "YDB"))        return "YDB SQL";

			return "provider-specific SQL";
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

		static bool IsProvider(string providerName, string family)
		{
			return string.Equals(providerName, family, StringComparison.OrdinalIgnoreCase)
				|| providerName.StartsWith(family + ".", StringComparison.OrdinalIgnoreCase);
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
			int     MaxRows,
			string  UnsafeSqlPolicy,
			bool    ImpersonationEnabled);
	}
}
