using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

using LinqToDB.Internal.Common;

namespace LinqToDB.LINQPad;

internal sealed class YdbProvider() : DatabaseProviderBase(ProviderName.Ydb, "YDB", _providers)
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.Ydb, "YDB (YQL dialect)")
	];

	public override void ClearAllPools(string providerName)
	{
		// At the moment, there is no public API in Ydb.Sdk.Ado to clear connection pools — so this is a no-op.
		// If such an API appears in the future, we can invoke the static method via reflection:
		// TryInvokeStatic("Ydb.Sdk.Ado.YdbConnection, Ydb.Sdk", "ClearAllPools");
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// YDB system schema does not store modification timestamps for tables — return null.
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		// Try to locate the provider factory by different known type names:
		// 1) Official SDK (Ydb.Sdk) with ADO.NET provider
		// 2) Legacy package Yandex.Ydb.ADO (for backward compatibility)
		var factory =
			TryGetFactory("Ydb.Sdk.Ado.YdbFactory, Ydb.Sdk") ??
			TryGetFactory("Ydb.Sdk.Ado.YdbProviderFactory, Ydb.Sdk") ??
			TryGetFactory("Yandex.Ydb.ADO.YdbFactory, Yandex.Ydb.ADO") ??
			TryGetFactory("Yandex.Ydb.ADO.YdbProviderFactory, Yandex.Ydb.ADO");

		if (factory != null)
			return factory;

		throw new LinqToDBLinqPadException(
			"YDB ADO.NET provider not found. Make sure the Ydb.Sdk package (with ADO) is available " +
			"to the application (e.g., next to LINQPad.exe or in a probing path). See the YDB .NET SDK documentation.");
	}

	private static DbProviderFactory? TryGetFactory(string qualifiedTypeName)
	{
		var t = Type.GetType(qualifiedTypeName, throwOnError: false);
		if (t == null) return null;

		// Most factories expose a static property Instance/Singleton
		var pi = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
		      ?? t.GetProperty("Singleton", BindingFlags.Public | BindingFlags.Static);

		if (pi?.GetValue(null) is DbProviderFactory f)
			return f;

		return ActivatorExt.CreateInstance(t) as DbProviderFactory;
	}

	// private static void TryInvokeStatic(string qualifiedTypeName, string methodName)
	// {
	// 	var t  = Type.GetType(qualifiedTypeName, throwOnError: false);
	// 	var mi = t?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
	// 	mi?.InvokeExt(null, null);
	// }
}
