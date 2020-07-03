using System;

namespace LinqToDB.DataProvider.SQLite
{
	public class SQLiteProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _systemSyncRoot = new object();
		private static readonly object _msSyncRoot     = new object();

		private static SQLiteProviderAdapter? _systemDataSQLite;
		private static SQLiteProviderAdapter? _microsoftDataSQLite;

		public const string SystemDataSQLiteAssemblyName       = "System.Data.SQLite";
		public const string SystemDataSQLiteClientNamespace    = "System.Data.SQLite";

		public const string MicrosoftDataSQLiteAssemblyName    = "Microsoft.Data.Sqlite";
		public const string MicrosoftDataSQLiteClientNamespace = "Microsoft.Data.Sqlite";

		private SQLiteProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			bool disposeCommandOnError)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			DisposeCommandOnError = disposeCommandOnError;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		/// <summary>
		/// Enables workaround for https://github.com/aspnet/EntityFrameworkCore/issues/17521
		/// for Microsoft.Data.Sqlite v3.0.0.
		/// </summary>
		internal bool DisposeCommandOnError { get; }

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

			var disposeCommandOnError = connectionType.AssemblyQualifiedName == "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60";

			return new SQLiteProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				disposeCommandOnError);
		}

		public static SQLiteProviderAdapter GetInstance(string name)
		{
			if (name == ProviderName.SQLiteClassic)
			{
				if (_systemDataSQLite == null)
					lock (_systemSyncRoot)
						if (_systemDataSQLite == null)
							_systemDataSQLite = CreateAdapter(SystemDataSQLiteAssemblyName, SystemDataSQLiteClientNamespace, "SQLite");

				return _systemDataSQLite;
			}
			else
			{
				if (_microsoftDataSQLite == null)
					lock (_msSyncRoot)
						if (_microsoftDataSQLite == null)
							_microsoftDataSQLite = CreateAdapter(MicrosoftDataSQLiteAssemblyName, MicrosoftDataSQLiteClientNamespace, "Sqlite");

				return _microsoftDataSQLite;
			}
		}
	}
}
