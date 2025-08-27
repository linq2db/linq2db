using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.LINQPad;

internal abstract class DatabaseProviderBase(string database, string description, IReadOnlyList<ProviderInfo> providers) : IDatabaseProvider
{
	public string Database { get; } = database;
	public string Description { get; } = description;
	public IReadOnlyList<ProviderInfo> Providers { get; } = providers;

	public virtual bool SupportsSecondaryConnection { get; }
	public virtual bool AutomaticProviderSelection  { get; }

	public virtual IReadOnlyCollection<Assembly> GetAdditionalReferences      (string providerName                     ) => [];
	public virtual string?                       GetProviderAssemblyName      (string providerName                     ) => null;
	public virtual ProviderInfo?                 GetProviderByConnectionString(string connectionString                 ) => null;
#pragma warning disable CA1055 // URI-like return values should not be strings
	public virtual string?                       GetProviderDownloadUrl       (string? providerName                    ) => null;
#pragma warning restore CA1055 // URI-like return values should not be strings
	public virtual bool                          IsProviderPathSupported      (string providerName                     ) => false;
	public virtual void                          RegisterProviderFactory      (string providerName, string providerPath) { }
	public virtual string?                       TryGetDefaultPath            (string providerName                     ) => null;
#if NETFRAMEWORK
	public virtual void                          Unload                       (                                        ) { }
#endif

	public abstract void              ClearAllPools      (string providerName        );
	public abstract DateTime?         GetLastSchemaUpdate(ConnectionSettings settings);
	public abstract DbProviderFactory GetProviderFactory (string providerName        );

	/// <param name="providerName">Provider name.</param>
	/// <param name="connectionString">Connection string must be resolved against password manager already.</param>
	/// <returns></returns>
	public virtual IDataProvider GetDataProvider(string providerName, string connectionString)
	{
		return DataConnection.GetDataProvider(providerName, connectionString)
			?? throw new LinqToDBLinqPadException($"Can not activate provider '{providerName}'");
	}
}
