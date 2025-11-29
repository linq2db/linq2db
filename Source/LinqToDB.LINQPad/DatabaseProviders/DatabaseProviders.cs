using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data.Common;

using LinqToDB.DataProvider;

namespace LinqToDB.LINQPad;

internal static class DatabaseProviders
{
	public static readonly FrozenDictionary<string, IDatabaseProvider> Providers;
	public static readonly FrozenDictionary<string, IDatabaseProvider> ProvidersByProviderName;

#pragma warning disable CA1810 // Initialize reference type static fields inline
	static DatabaseProviders()
#pragma warning restore CA1810 // Initialize reference type static fields inline
	{
		var providers           = new Dictionary<string, IDatabaseProvider  >();
		var providersByName     = new Dictionary<string, IDatabaseProvider  >();

		Register(providers, providersByName, new AccessProvider    ());
		Register(providers, providersByName, new FirebirdProvider  ());
		Register(providers, providersByName, new MySqlProvider     ());
		Register(providers, providersByName, new PostgreSQLProvider());
		Register(providers, providersByName, new SybaseAseProvider ());
		Register(providers, providersByName, new SQLiteProvider    ());
		Register(providers, providersByName, new SqlCeProvider     ());
#if !NETFRAMEWORK
		if (IntPtr.Size == 8)
		{
			Register(providers, providersByName, new DB2Provider     ());
			Register(providers, providersByName, new InformixProvider());
		}
#else
		Register(providers, providersByName, new InformixProvider  ());
		Register(providers, providersByName, new DB2Provider       ());
#endif
		Register(providers, providersByName, new SapHanaProvider   ());
		Register(providers, providersByName, new OracleProvider    ());
		Register(providers, providersByName, new SqlServerProvider ());
		Register(providers, providersByName, new ClickHouseProvider());
		Register(providers, providersByName, new YdbProvider       ());

		Providers               = providers.ToFrozenDictionary();
		ProvidersByProviderName = providersByName.ToFrozenDictionary();

		static void Register(
			Dictionary<string, IDatabaseProvider> providers,
			Dictionary<string, IDatabaseProvider> providersByName,
			IDatabaseProvider                     provider)
		{
			providers.Add(provider.Database, provider);

			foreach (var info in provider.Providers)
				providersByName.Add(info.Name, provider);
		}
	}

	public static void Unload()
	{
#if NETFRAMEWORK
		foreach (var provider in Providers.Values)
			provider.Unload();
#endif
	}

	public static void Init()
	{
		// trigger .cctors

#if NETFRAMEWORK
		// no harm in loading it here unconditionally instead of trying to detect all places where we really need it
		DB2Provider.LoadAssembly();
#endif
	}

	public static DbConnection CreateConnection(ConnectionSettings settings)
	{
		return GetDataProvider(settings).CreateConnection(settings.Connection.GetFullConnectionString()!);
	}

	public static DbProviderFactory? GetProviderFactory(ConnectionSettings settings) => GetProviderByName(settings.Connection.Provider!).GetProviderFactory(settings.Connection.Provider!);

	public static IDataProvider GetDataProvider(ConnectionSettings settings)
	{
		return GetDataProvider(settings.Connection.Provider, settings.Connection.GetFullConnectionString(), settings.Connection.ProviderPath);
	}

	/// <param name="providerName">Provider name.</param>
	/// <param name="connectionString">Connection string must be already resolved against password manager.</param>
	/// <param name="providerPath">Optional path to provider assembly.</param>
	public static IDataProvider GetDataProvider(string? providerName, string? connectionString, string? providerPath)
	{
		if (string.IsNullOrWhiteSpace(providerName))
			throw new LinqToDBLinqPadException("Can not activate provider. Provider is not selected.");

		if (string.IsNullOrWhiteSpace(connectionString))
			throw new LinqToDBLinqPadException($"Can not activate provider '{providerName}'. Connection string not specified.");

		var databaseProvider = GetProviderByName(providerName!);
		if (providerPath != null)
			databaseProvider.RegisterProviderFactory(providerName!, providerPath);

		return databaseProvider.GetDataProvider(providerName!, connectionString!);
	}

	private static IDatabaseProvider GetProviderByName(string providerName)
	{
		if (ProvidersByProviderName.TryGetValue(providerName, out var provider))
			return provider;

		throw new LinqToDBLinqPadException($"Cannot find database provider '{providerName}'");
	}

	/// <summary>
	/// Gets database provider abstraction by database name.
	/// </summary>
	/// <param name="database">Database name (identifier of provider abstraction).</param>
	public static IDatabaseProvider GetProvider(string? database)
	{
		if (database != null && Providers.TryGetValue(database, out var provider))
			return provider;

		throw new LinqToDBLinqPadException($"Cannot find provider for database '{database}'");
	}
}
