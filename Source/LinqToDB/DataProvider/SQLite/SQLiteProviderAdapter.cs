using System;
using LinqToDB.Expressions;

namespace LinqToDB.DataProvider.SQLite;

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
		bool    disposeCommandOnError,
		bool    supportsRowValue,
		bool    supportsUpdateFrom,
		bool    supportsDateOnly,
		Action? clearAllPulls)
	{
		ConnectionType  = connectionType;
		DataReaderType  = dataReaderType;
		ParameterType   = parameterType;
		CommandType     = commandType;
		TransactionType = transactionType;

		DisposeCommandOnError = disposeCommandOnError;
		SupportsRowValue      = supportsRowValue;
		SupportsUpdateFrom    = supportsUpdateFrom;
		SupportsDateOnly      = supportsDateOnly;

		ClearAllPools = clearAllPulls;
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

	// ROW VALUE feature introduced in SQLite 3.15.0.
	internal bool SupportsRowValue { get; }
	// UPDATE FROM feature introduced in SQLite 3.33.0.
	internal bool SupportsUpdateFrom { get; }
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

		var disposeCommandOnError = connectionType.AssemblyQualifiedName == "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60";

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

		var supportsRowValue   = version >= (clientNamespace == MicrosoftDataSQLiteClientNamespace ? RowValueMinVersionMDS   : RowValueMinVersionSDS);
		var supportsUpdateFrom = version >= (clientNamespace == MicrosoftDataSQLiteClientNamespace ? UpdateFromMinVersionMDS : UpdateFromMinVersionSDS);
		var supportsDateOnly   = clientNamespace == MicrosoftDataSQLiteClientNamespace && assembly.GetName().Version >= MinDateOnlyAssemblyVersionMDS;

		return new SQLiteProviderAdapter(
			connectionType,
			dataReaderType,
			parameterType,
			commandType,
			transactionType,
			disposeCommandOnError,
			supportsRowValue,
			supportsUpdateFrom,
			supportsDateOnly,
			clearAllPools);
	}

	private static readonly Version ClearPoolsMinVersionMDS       = new (6, 0, 0);
	private static readonly Version ClearPoolsMinVersionSDS       = new (1, 0, 55);
	private static readonly Version RowValueMinVersionMDS         = new (2, 0, 0);
	private static readonly Version RowValueMinVersionSDS         = new (1, 0, 104);
	private static readonly Version UpdateFromMinVersionMDS       = new (3, 1, 20);
	private static readonly Version UpdateFromMinVersionSDS       = new (1, 0, 114);
	private static readonly Version MinDateOnlyAssemblyVersionMDS = new (6, 0, 0);
	

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

	#region wrappers
	[Wrapper]
	private class SqliteConnection
	{
		public static void ClearAllPools() => throw new NotImplementedException();
	}

	[Wrapper]
	private class SQLiteConnection
	{
		public static void ClearAllPools() => throw new NotImplementedException();
	}
	#endregion
}
