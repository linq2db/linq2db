using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

using LinqToDB.DataProvider;

namespace LinqToDB.LINQPad;

/// <summary>
/// Provides database provider abstraction.
/// </summary>
internal interface IDatabaseProvider
{
	/// <summary>
	/// Gets database name (generic <see cref="ProviderName"/> value).
	/// </summary>
	string Database { get; }

	/// <summary>
	/// Gets database provider name for UI.
	/// </summary>
	string Description { get; }

	/// <summary>
	/// When <c>true</c>, database provider supports secondary connection for database schema population.
	/// </summary>
	bool SupportsSecondaryConnection { get; }

	/// <summary>
	/// Release all connections.
	/// </summary>
	void ClearAllPools(string providerName);

	/// <summary>
	/// Returns last schema update time.
	/// </summary>
	DateTime? GetLastSchemaUpdate(ConnectionSettings settings);

	/// <summary>
	/// Returns additional reference assemblies for dynamic model compilation (except main provider assembly).
	/// </summary>
	IReadOnlyCollection<Assembly> GetAdditionalReferences(string providerName);

	/// <summary>
	/// List of supported provider names for provider.
	/// </summary>
	IReadOnlyList<ProviderInfo> Providers { get; }

	/// <summary>
	/// When <c>true</c>, connection settings UI doesn't allow user to select provider type.
	/// <see cref="GetProviderByConnectionString(string)"/> method will be used to infer provider automatically.
	/// Note that provider selection also unavailable when there is only one provider supported by database.
	/// </summary>
	bool AutomaticProviderSelection { get; }

	/// <summary>
	/// Tries to infer provider by database connection string.
	/// </summary>
	/// <param name="connectionString">Connection string could contain password manager tokens.</param>
	/// <returns></returns>
	ProviderInfo? GetProviderByConnectionString(string connectionString);

	/// <summary>
	/// Returns <c>true</c>, if specified provider for current database provider supports provider assembly path configuration.
	/// </summary>
	bool IsProviderPathSupported(string providerName);

	/// <summary>
	/// If provider supports assembly path configuration, method
	/// returns help text for configuration UI to help user locate and/or install provider.
	/// </summary>
	string? GetProviderAssemblyName(string providerName);

	/// <summary>
	/// If provider supports assembly path configuration, method could return URL to provider download page.
	/// </summary>
#pragma warning disable CA1055 // URI-like return values should not be strings
	string? GetProviderDownloadUrl(string? providerName);
#pragma warning restore CA1055 // URI-like return values should not be strings

	/// <summary>
	/// If provider supports assembly path configuration (<see cref="IsProviderPathSupported(string)"/>), method tries to return default path to provider assembly,
	/// but only if assembly exists on specified path.
	/// </summary>
	string? TryGetDefaultPath(string providerName);

	/// <summary>
	/// If provider supports assembly path configuration (<see cref="IsProviderPathSupported(string)"/>), method
	/// performs provider factory registration to allow Linq To DB locate provider assembly.
	/// </summary>
	void RegisterProviderFactory(string providerName, string providerPath);

	// Technically, factory is needed for raw SQL queries only for LINQPad 5 as v6+ has code to work without factory. Still it wasn't backported to LINQPad 5 and doesn't hurt to support.
	// LINQPad currently calls only CreateCommand and CreateDataAdapter methods.
	/// <summary>
	/// Returns ADO.NET provider classes factory.
	/// </summary>
	DbProviderFactory GetProviderFactory(string providerName);

	/// <summary>
	/// Returns linq2db data provider.
	/// </summary>
	/// <param name="providerName">Provider name.</param>
	/// <param name="connectionString">Connection string must be already resolved against password manager.</param>
	IDataProvider GetDataProvider(string providerName, string connectionString);

#if NETFRAMEWORK
	/// <summary>
	/// Performs clanup on domain unload.
	/// </summary>
	void Unload();
#endif
}
