using System;

namespace LinqToDB.DataProvider.SQLite
{
	using LinqToDB.Expressions;

	public class SQLiteProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _systemSyncRoot = new ();
		private static readonly object _msSyncRoot     = new ();

		private static SQLiteProviderAdapter? _systemDataSQLite;
		private static SQLiteProviderAdapter? _microsoftDataSQLite;

		public const string SystemDataSQLiteAssemblyName       = "System.Data.SQLite";
		public const string SystemDataSQLiteClientNamespace    = "System.Data.SQLite";

		public const string MicrosoftDataSQLiteAssemblyName    = "Microsoft.Data.Sqlite";
		public const string MicrosoftDataSQLiteClientNamespace = "Microsoft.Data.Sqlite";

		private SQLiteProviderAdapter(
			Type    connectionType,
			Type    dataReaderType,
			Type    parameterType,
			Type    commandType,
			Type    transactionType,
			bool    supportsDateOnly,
			Action? clearAllPulls)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			SupportsDateOnly      = supportsDateOnly;

			ClearAllPools = clearAllPulls;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		// Classic driver does not store dates correctly
		internal bool SupportsDateOnly { get; }

		public Action? ClearAllPools { get; }

		private static SQLiteProviderAdapter CreateAdapter(string assemblyName, string clientNamespace, string prefix)
		{
			var assembly = Common.Tools.TryLoadAssembly(assemblyName, null);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {assemblyName}");

			var connectionType  = assembly.GetType($"{clientNamespace}.{prefix}Connection" , true)!;
			var dataReaderType  = assembly.GetType($"{clientNamespace}.{prefix}DataReader" , true)!;
			var parameterType   = assembly.GetType($"{clientNamespace}.{prefix}Parameter"  , true)!;
			var commandType     = assembly.GetType($"{clientNamespace}.{prefix}Command"    , true)!;
			var transactionType = assembly.GetType($"{clientNamespace}.{prefix}Transaction", true)!;

			var version = assembly.GetName().Version;

			Action? clearAllPools = null;
			if (clientNamespace == MicrosoftDataSQLiteClientNamespace)
			{
				if (version >= ClearPoolsMinVersionMDS)
				{
					var typeMapper = new TypeMapper();
					typeMapper.RegisterTypeWrapper<SqliteConnection>(connectionType);
					typeMapper.FinalizeMappings();
					clearAllPools = typeMapper.BuildAction(typeMapper.MapActionLambda(() => SqliteConnection.ClearAllPools()));
				}
			}
			else if (version >= ClearPoolsMinVersionSDS)
			{
				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<SQLiteConnection>(connectionType);
				typeMapper.FinalizeMappings();
				clearAllPools = typeMapper.BuildAction(typeMapper.MapActionLambda(() => SQLiteConnection.ClearAllPools()));
			}

			var supportsDateOnly   = clientNamespace == MicrosoftDataSQLiteClientNamespace && assembly.GetName().Version >= MinDateOnlyAssemblyVersionMDS;

			return new SQLiteProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				supportsDateOnly,
				clearAllPools);
		}

		private static readonly Version ClearPoolsMinVersionMDS       = new (6, 0, 0);
		private static readonly Version ClearPoolsMinVersionSDS       = new (1, 0, 55);
		private static readonly Version MinDateOnlyAssemblyVersionMDS = new (6, 0, 0);

		public static SQLiteProviderAdapter GetInstance(string name)
		{
			if (name == ProviderName.SQLiteClassic)
			{
				if (_systemDataSQLite == null)
				{
					lock (_systemSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_systemDataSQLite ??= CreateAdapter(SystemDataSQLiteAssemblyName, SystemDataSQLiteClientNamespace, "SQLite");
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _systemDataSQLite;
			}
			else
			{
				if (_microsoftDataSQLite == null)
				{
					lock (_msSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_microsoftDataSQLite ??= CreateAdapter(MicrosoftDataSQLiteAssemblyName, MicrosoftDataSQLiteClientNamespace, "Sqlite");
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _microsoftDataSQLite;
			}
		}

		#region wrappers
		[Wrapper]
		private sealed class SqliteConnection
		{
			public static void ClearAllPools() => throw new NotImplementedException();
		}

		[Wrapper]
		private sealed class SQLiteConnection
		{
			public static void ClearAllPools() => throw new NotImplementedException();
		}
		#endregion
	}
}
