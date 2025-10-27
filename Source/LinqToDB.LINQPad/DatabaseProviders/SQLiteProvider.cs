using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

using Microsoft.Data.Sqlite;

namespace LinqToDB.LINQPad;

internal sealed class SQLiteProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.SQLiteClassic, "Official Client (System.Data.SQLite)"   ),
		new(ProviderName.SQLiteMS,      "Microsof Client (Microsoft.Data.Sqlite)")
	];

#if NETFRAMEWORK
	static SQLiteProvider()
	{
		// temporary, see SQLite.Runtime.props notes
		Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sds"));

		// data adapter not implemented by MS provider
		// https://github.com/dotnet/efcore/issues/13838
		// but needed for linqpad 5 to run sql
		// so we implement own factory class with adapter and use this
		// hack to force linqpad to use our GetProviderFactory implementation
		//
		// note: this is needed for SQL panel to work with MS provider
		typeof(SqliteFactory)
			.GetField("Instance", BindingFlags.Static | BindingFlags.Public)
			?.SetValue(null, null);
	}
#endif

	public SQLiteProvider()
		: base(ProviderName.SQLite, "SQLite", _providers)
	{
	}

	public override void ClearAllPools(string providerName)
	{
		if (providerName == ProviderName.SQLiteClassic)
			SQLiteConnection.ClearAllPools();
		else
			SqliteConnection.ClearAllPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// no information in schema
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		if (providerName == ProviderName.SQLiteClassic)
			return SQLiteFactory.Instance;
		else
#if NETFRAMEWORK
			return MsDbProviderFactory.Instance;
#else
			return SqliteFactory.Instance;
#endif
	}

#if NETFRAMEWORK
	sealed class MsDbProviderFactory : DbProviderFactory
	{
		private MsDbProviderFactory()
		{
		}

		public static readonly MsDbProviderFactory Instance = new();

		public override DbCommand CreateCommand() => new SqliteCommand();

		public override DbConnection CreateConnection() => new SqliteConnection();

		public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new SqliteConnectionStringBuilder();

		public override DbParameter CreateParameter() => new SqliteParameter();

		public override DbDataAdapter CreateDataAdapter() => new SqliteDataAdapter();

		sealed class SqliteDataAdapter : DbDataAdapter
		{
		}
	}
#endif
}
