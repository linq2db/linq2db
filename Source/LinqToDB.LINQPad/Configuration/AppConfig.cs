using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml;

using LinqToDB.Configuration;

namespace LinqToDB.LINQPad;

/// <summary>
/// Implements Linq To DB connection settings provider, which use data from JSON config.
/// Used as settings source for static data context.
/// </summary>
internal sealed class AppConfig(IConnectionStringSettings[] connectionStrings) : ILinqToDBSettings
{
#pragma warning disable CA1859 // change return type
	public static ILinqToDBSettings LoadJson(string configPath)
#pragma warning restore CA1859 // change return type
	{
		var config = JsonSerializer.Deserialize<JsonConfig>(File.ReadAllText(configPath));

		if (config?.ConnectionStrings?.Count is null or 0)
			return new AppConfig([]);

		var connections = new Dictionary<string, ConnectionStringSettings>(StringComparer.InvariantCultureIgnoreCase);
		foreach (var cn in config.ConnectionStrings)
		{
			if (cn.Key.EndsWith("_ProviderName", StringComparison.InvariantCultureIgnoreCase))
				continue;

			connections.Add(cn.Key, new ConnectionStringSettings(cn.Key, PasswordManager.ResolvePasswordManagerFields(cn.Value)));
		}

		foreach (var cn in config.ConnectionStrings)
		{
			if (!cn.Key.EndsWith("_ProviderName", StringComparison.InvariantCultureIgnoreCase))
				continue;

			var key = cn.Key.Substring(0, cn.Key.Length - "_ProviderName".Length);
			if (connections.TryGetValue(key, out var cs))
				cs.ProviderName = cn.Value;
		}

		return new AppConfig([.. connections.Values]);
	}

#pragma warning disable CA1859 // change return type
	public static ILinqToDBSettings LoadAppConfig(string configPath)
#pragma warning restore CA1859 // change return type
	{
		var xml = new XmlDocument() { XmlResolver = null };
		using var reader = XmlReader.Create(new StringReader(File.ReadAllText(configPath)), new XmlReaderSettings() { XmlResolver = null });
		xml.Load(reader);
		
		var connections = xml.SelectNodes("/configuration/connectionStrings/add");

		if (connections?.Count is null or 0)
			return new AppConfig([]);

		var settings = new List<ConnectionStringSettings>();

		foreach (XmlElement node in connections)
		{
			var name             = node.Attributes["name"            ]?.Value;
			var connectionString = node.Attributes["connectionString"]?.Value;
			var providerName     = node.Attributes["providerName"    ]?.Value;

			if (name != null && connectionString != null)
				settings.Add(new ConnectionStringSettings(name, PasswordManager.ResolvePasswordManagerFields(connectionString)) { ProviderName = providerName });
		}

		return new AppConfig([.. settings]);
	}

	IEnumerable<IDataProviderSettings>     ILinqToDBSettings.DataProviders        => [];
	string?                                ILinqToDBSettings.DefaultConfiguration => null;
	string?                                ILinqToDBSettings.DefaultDataProvider  => null;
	IEnumerable<IConnectionStringSettings> ILinqToDBSettings.ConnectionStrings    => connectionStrings;

#pragma warning disable CA1812 // Remove unused type
	private sealed class JsonConfig
#pragma warning restore CA1812 // Remove unused type
	{
		public IDictionary<string, string>? ConnectionStrings { get; set; }
	}

	/// <param name="name">Connection name.</param>
	/// <param name="connectionString">Must be connection string without password manager tokens.</param>
	private sealed class ConnectionStringSettings(string name, string connectionString) : IConnectionStringSettings
	{
		string IConnectionStringSettings.ConnectionString => connectionString;
		string IConnectionStringSettings.Name             => name;
		bool   IConnectionStringSettings.IsGlobal         => false;

		public string? ProviderName { get; set; }
	}
}
