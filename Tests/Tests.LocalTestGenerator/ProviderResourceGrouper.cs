using System.Collections.ObjectModel;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

using Tests.Tools;

namespace Tests.LocalTestGenerator
{
	internal sealed record ResourceKey(string Key, string DisplayName);

	internal sealed record ProviderResourceGrouping(
		IReadOnlyList<ProviderResourceGroup> Groups,
		IReadOnlyList<string>                Warnings);

	internal sealed class ProviderResourceGroup
	{
		private readonly Dictionary<string, SectionProviders> _sections  = new(StringComparer.Ordinal);
		private readonly SortedSet<string>                    _providers = new(StringComparer.Ordinal);

		public ProviderResourceGroup(ResourceKey resourceKey)
		{
			ResourceKey = resourceKey;
		}

		public ResourceKey ResourceKey { get; }
		public string      TargetFramework { get; set; } = "";
		public string      ProjectName { get; set; } = "";

		public IReadOnlyList<string>                         ProviderNames => _providers.ToArray();
		public IReadOnlyDictionary<string, SectionProviders> Sections      => new ReadOnlyDictionary<string, SectionProviders>(_sections);

		public void Add(string sectionName, TestSettings sourceSettings, string providerName, TestConnection connection)
		{
			if (!_sections.TryGetValue(sectionName, out var section))
			{
				section = new SectionProviders(sourceSettings);
				_sections.Add(sectionName, section);
			}

			section.Add(providerName, connection);
			_providers.Add(providerName);
		}
	}

	internal sealed class SectionProviders
	{
		private readonly SortedDictionary<string, TestConnection> _connections = new(StringComparer.Ordinal);
		private readonly SortedSet<string>                        _providers   = new(StringComparer.Ordinal);

		public SectionProviders(TestSettings sourceSettings)
		{
			SourceSettings = sourceSettings;
		}

		public TestSettings SourceSettings { get; }

		public IReadOnlyList<string>                       Providers   => _providers.ToArray();
		public IReadOnlyDictionary<string, TestConnection> Connections => new ReadOnlyDictionary<string, TestConnection>(_connections);

		public void Add(string providerName, TestConnection connection)
		{
			_providers.Add(providerName);
			_connections[providerName] = connection;
		}
	}

	internal static class ProviderResourceGrouper
	{
		public static ProviderResourceGrouping Group(TestSettingsProvider settingsProvider, IEnumerable<TargetFrameworkConfiguration> targetFrameworks)
		{
			var groups   = new SortedDictionary<string, ProviderResourceGroup>(StringComparer.Ordinal);
			var warnings = new List<string>();

			foreach (var targetFramework in targetFrameworks)
			{
				var sectionName = targetFramework.ConfigurationSection;
				var settings = settingsProvider.GetSettings(sectionName);

				foreach (var providerName in settings.Providers ?? [])
				{
					if (!settings.Connections!.TryGetValue(providerName, out var connection) || string.IsNullOrWhiteSpace(connection.ConnectionString))
					{
						warnings.Add($"provider '{providerName}' has no connection string and will be skipped.");
						continue;
					}

					var resourceKey = CreateResourceKey(providerName, connection);
					var groupKey    = $"{targetFramework.TargetFramework}|{resourceKey.Key}";

					if (!groups.TryGetValue(groupKey, out var group))
					{
						group = new ProviderResourceGroup(resourceKey);
						group.TargetFramework = targetFramework.TargetFramework;
						groups.Add(groupKey, group);
					}

					group.Add(sectionName, settings, providerName, connection);
				}
			}

			var realGroups = groups.Values.ToArray();

			AssignProjectNames(realGroups);

			return new ProviderResourceGrouping(realGroups, warnings);
		}

		private static void AssignProjectNames(IReadOnlyList<ProviderResourceGroup> groups)
		{
			var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var group in groups.OrderBy(static g => g.ProviderNames[0], StringComparer.Ordinal))
			{
				var representative = group.ProviderNames
					.OrderBy(static p => p.Length)
					.ThenBy(static p => p, StringComparer.Ordinal)
					.First();

				var projectName = $"Generated.Tests.{ProjectNameSanitizer.Sanitize(group.TargetFramework)}.{ProjectNameSanitizer.Sanitize(representative)}";

				if (!usedNames.Add(projectName))
				{
					var suffix = StableHash($"{group.TargetFramework}|{group.ResourceKey.Key}")[..8];
					projectName = $"{projectName}_{suffix}";
					usedNames.Add(projectName);
				}

				group.ProjectName = projectName;
			}
		}

		private static ResourceKey CreateResourceKey(string providerName, TestConnection connection)
		{
			var provider         = connection.Provider ?? providerName;
			var connectionString = connection.ConnectionString!;
			var family           = GetProviderFamily(providerName, provider);
			var values           = ReadConnectionString(connectionString);

			if (family is "sqlserver")
			{
				var server   = GetValue(values, "data source",     "server", "address", "addr", "network address") ?? "";
				var database = GetValue(values, "initial catalog", "database")                                     ?? "";

				if (server.Length > 0 || database.Length > 0)
					return CreateServerResourceKey("sqlserver", server, null, database);
			}

			if (family is "postgresql" or "mysql" or "mariadb" or "clickhouse" or "oracle" or "firebird" or "sybase" or "saphana" or "db2" or "informix" or "ydb")
			{
				var server   = GetValue(values, "host", "server", "data source", "datasource", "servernode", "location") ?? "";
				var port     = GetValue(values, "port");
				var database = GetValue(values, "database", "initial catalog", "user id", "uid", "current schema") ?? "";

				if (server.Length > 0 || database.Length > 0)
					return CreateServerResourceKey(family, server, port, database);
			}

			if (family is "sqlite" or "access" or "sqlce" or "duckdb")
			{
				var file = GetValue(values, "data source", "data source", "filename", "file name", "dbq", "database");

				if (!string.IsNullOrWhiteSpace(file))
				{
					var normalized = NormalizeFilePath(file);
					return new ResourceKey(
						$"{family}:file:{normalized.ToUpperInvariant()}",
						$"{family}://{normalized}");
				}
			}

			var canonical = string.Join(";", values.OrderBy(static p => p.Key, StringComparer.Ordinal).Select(static p => $"{p.Key}={p.Value}"));
			var hash      = StableHash(canonical);

			return new ResourceKey(
				$"{family}:hash:{hash}",
				$"{family}://{hash}");
		}

		private static ResourceKey CreateServerResourceKey(string scheme, string server, string? port, string database)
		{
			server   = NormalizeServer(server);
			database = database.Trim();

			var authority = string.IsNullOrWhiteSpace(port) ? server : $"{server}:{port.Trim()}";
			var display   = $"{scheme}://{authority}/{database}";

			return new ResourceKey(display.ToUpperInvariant(), display);
		}

		private static string GetProviderFamily(string providerName, string provider)
		{
			var value = $"{providerName} {provider}";

			if (value.Contains("ClickHouse", StringComparison.OrdinalIgnoreCase))
				return "clickhouse";

			if (value.Contains("Postgre", StringComparison.OrdinalIgnoreCase) ||
			    value.Contains("Npgsql",  StringComparison.OrdinalIgnoreCase))
				return "postgresql";

			if (value.Contains("MariaDB", StringComparison.OrdinalIgnoreCase))
				return "mariadb";

			if (value.Contains("MySql",          StringComparison.OrdinalIgnoreCase) ||
			    value.Contains("MySqlConnector", StringComparison.OrdinalIgnoreCase))
				return "mysql";

			if (value.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
				return "oracle";

			if (value.Contains("Firebird", StringComparison.OrdinalIgnoreCase))
				return "firebird";

			if (value.Contains("Sybase", StringComparison.OrdinalIgnoreCase))
				return "sybase";

			if (value.Contains("SapHana", StringComparison.OrdinalIgnoreCase) ||
			    value.Contains("Hana",    StringComparison.OrdinalIgnoreCase))
				return "saphana";

			if (value.Contains("Informix", StringComparison.OrdinalIgnoreCase))
				return "informix";

			if (value.Contains("DB2",      StringComparison.OrdinalIgnoreCase) ||
			    value.Contains("IBM.Data", StringComparison.OrdinalIgnoreCase))
				return "db2";

			if (value.Contains("Ydb", StringComparison.OrdinalIgnoreCase))
				return "ydb";

			if (value.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ||
			    value.Contains("SqlClient", StringComparison.OrdinalIgnoreCase))
				return "sqlserver";

			if (value.Contains("SQLite", StringComparison.OrdinalIgnoreCase) ||
			    value.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
				return "sqlite";

			if (value.Contains("Access", StringComparison.OrdinalIgnoreCase))
				return "access";

			if (value.Contains("SqlCe", StringComparison.OrdinalIgnoreCase))
				return "sqlce";

			if (value.Contains("DuckDB", StringComparison.OrdinalIgnoreCase))
				return "duckdb";

			return ProjectNameSanitizer.Sanitize(providerName.Split('.')[0]).ToLowerInvariant();
		}

		private static Dictionary<string, string> ReadConnectionString(string connectionString)
		{
			var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
			var values  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (string key in builder.Keys)
				values[key.Trim()] = Convert.ToString(builder[key])?.Trim() ?? "";

			return values;
		}

		private static string? GetValue(Dictionary<string, string> values, params string[] names)
		{
			foreach (var name in names)
				if (values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
					return value;

			return null;
		}

		private static string NormalizeServer(string server)
		{
			server = server.Trim();

			if (server is "." or "(local)")
				return "localhost";

			return server;
		}

		private static string NormalizeFilePath(string file)
		{
			file = file.Trim();

			if (file.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
				return file;

			return Path.GetFullPath(Environment.ExpandEnvironmentVariables(file));
		}

		private static string StableHash(string value)
		{
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
			return Convert.ToHexString(bytes).ToLowerInvariant();
		}
	}
}
