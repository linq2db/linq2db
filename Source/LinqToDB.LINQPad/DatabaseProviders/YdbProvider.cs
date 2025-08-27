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
		// На текущий момент публичного API для очистки пулов в Ydb.Sdk.Ado нет — оставляем no-op.
		// Если появится — можно вызвать статический метод через рефлексию:
		// TryInvokeStatic("Ydb.Sdk.Ado.YdbConnection, Ydb.Sdk", "ClearAllPools");
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// В системной схеме YDB времени модификации таблиц нет — возвращаем null.
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		// Пытаемся найти фабрику провайдера разными известными именами типов.
		// 1) Официальный SDK (Ydb.Sdk) с ADO.NET-провайдером
		// 2) Старый пакет Yandex.Ydb.ADO (если кто-то использует легаси)
		var factory =
			TryGetFactory("Ydb.Sdk.Ado.YdbFactory, Ydb.Sdk") ??
			TryGetFactory("Ydb.Sdk.Ado.YdbProviderFactory, Ydb.Sdk") ??
			TryGetFactory("Yandex.Ydb.ADO.YdbFactory, Yandex.Ydb.ADO") ??
			TryGetFactory("Yandex.Ydb.ADO.YdbProviderFactory, Yandex.Ydb.ADO");

		if (factory != null)
			return factory;

		throw new LinqToDBLinqPadException(
			"Не найден ADO.NET-провайдер YDB. Убедитесь, что пакет Ydb.Sdk (с ADO) доступен приложению " +
			"(например, рядом с LINQPad.exe или в probing-пути). См. документацию YDB .NET SDK.");
	}

	private static DbProviderFactory? TryGetFactory(string qualifiedTypeName)
	{
		var t = Type.GetType(qualifiedTypeName, throwOnError: false);
		if (t == null) return null;

		// Большинство фабрик имеют статическое свойство Instance/Singleton
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
