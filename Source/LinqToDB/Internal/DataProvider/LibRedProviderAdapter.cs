using System;
using System.Data.Common;
using System.Threading;

using LinqToDB.Internal.Expressions.Types;

namespace LinqToDB.Internal.DataProvider
{
	public sealed class LibRedProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Lock _syncRoot = new();
		private static LibRedProviderAdapter? _instance;

		public const string AssemblyName    = "LibRed.Ado";
		public const string ClientNamespace = "LibRed.Data";

		private LibRedProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,

			Func  <string, bool> databaseExists,
			Action<string>       dropDatabase,
			Action<DbConnection> clearPool)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			DatabaseExists = databaseExists;
			DropDatabase   = dropDatabase;
			ClearPool      = clearPool;
		}

#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

#endregion

		public Func  <string, bool> DatabaseExists { get; }
		public Action<string>       DropDatabase   { get; }
		public Action<DbConnection> ClearPool      { get; }

		public static LibRedProviderAdapter GetInstance()
		{
			if (_instance == null)
			{
				lock (_syncRoot)
					_instance ??= CreateAdapter();
			}

			return _instance;

			static LibRedProviderAdapter CreateAdapter()
			{
				var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null);
				if (assembly == null)
					throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

				var connectionType  = assembly.GetType($"{ClientNamespace}.LibRedConnection" , true)!;
				var dataReaderType  = assembly.GetType($"{ClientNamespace}.LibRedDataReader" , true)!;
				var parameterType   = assembly.GetType($"{ClientNamespace}.LibRedParameter"  , true)!;
				var commandType     = assembly.GetType($"{ClientNamespace}.LibRedCommand"    , true)!;
				var transactionType = assembly.GetType($"{ClientNamespace}.LibRedTransaction", true)!;

				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<LibRedConnection>(connectionType);
				typeMapper.FinalizeMappings();

				return new LibRedProviderAdapter(
					connectionType,
					dataReaderType,
					parameterType,
					commandType,
					transactionType,
					typeMapper.BuildTypedFactory<string, LibRedConnection, DbConnection>(connectionString => new LibRedConnection(connectionString)),
					typeMapper.BuildFunc<string, bool>(typeMapper.MapLambda((string connectionString) => LibRedConnection.DatabaseExists(connectionString))),
					typeMapper.BuildAction<string>(typeMapper.MapActionLambda((string connectionString) => LibRedConnection.DropDatabase(connectionString))),
					typeMapper.BuildAction<DbConnection>(typeMapper.MapActionLambda((LibRedConnection connection) => LibRedConnection.ClearPool(connection))));
			}
		}

		#region Wrappers

		[Wrapper]
		private sealed class LibRedConnection
		{
			public LibRedConnection(string connectionString) => throw new NotSupportedException();

			public static bool DatabaseExists(string connectionString)  => throw new NotSupportedException();
			public static void DropDatabase  (string connectionString)  => throw new NotSupportedException();
			public static void ClearPool     (LibRedConnection conn)    => throw new NotSupportedException();
		}

		#endregion
	}
}
