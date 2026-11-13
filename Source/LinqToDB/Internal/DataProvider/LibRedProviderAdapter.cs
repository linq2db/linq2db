using System;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using LinqToDB.Common;
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
			Func<string, DbConnection> connectionFactory)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			// bound on first use: the statics belong to a prerelease surface, and a signature change there must not
			// fail every LibRed connection
			_databaseManagement = new(() => CreateDatabaseManagement(connectionType), LazyThreadSafetyMode.ExecutionAndPublication);
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

		readonly Lazy<DatabaseManagement> _databaseManagement;

		public Func  <string, bool>   DatabaseExists => _databaseManagement.Value.DatabaseExists;
		/// <summary>
		/// Creates a database file: connection string, and <see langword="true"/> for an <c>.accdb</c> (ACE 12) file,
		/// <see langword="false"/> for an <c>.mdb</c> (Jet 4) one.
		/// </summary>
		public Action<string, bool>   CreateDatabase => _databaseManagement.Value.CreateDatabase;
		public Action<string>         DropDatabase   => _databaseManagement.Value.DropDatabase;
		public Action<DbConnection>   ClearPool      => _databaseManagement.Value.ClearPool;

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
					typeMapper.BuildTypedFactory<string, LibRedConnection, DbConnection>(connectionString => new LibRedConnection(connectionString)));
			}
		}

		static DatabaseManagement CreateDatabaseManagement(Type connectionType)
		{
			var typeMapper = new TypeMapper();
			typeMapper.RegisterTypeWrapper<LibRedConnection>(connectionType);
			typeMapper.FinalizeMappings();

			// CreateDatabase(string connectionString, Collation? collation, JetVersion version): the TypeMapper cannot
			// wrap the nullable Collation struct, so the call is built directly
			var create = connectionType.GetMethod("CreateDatabase", BindingFlags.Public | BindingFlags.Static)
				?? throw new InvalidOperationException($"{connectionType.FullName}.CreateDatabase not found");

			var parameters       = create.GetParameters();
			var versionType      = parameters[2].ParameterType;
			var connectionString = Expression.Parameter(typeof(string), "connectionString");
			var accdb            = Expression.Parameter(typeof(bool),   "accdb");

			var createDatabase = Expression.Lambda<Action<string, bool>>(
				Expression.Call(
					create,
					connectionString,
					Expression.Default(parameters[1].ParameterType),
					Expression.Condition(
						accdb,
						Expression.Constant(Enum.Parse(versionType, "Version12_2007"), versionType),
						Expression.Constant(Enum.Parse(versionType, "Version4"),       versionType))),
				connectionString,
				accdb)
				.CompileExpression();

			return new DatabaseManagement(
				typeMapper.BuildFunc<string, bool>(typeMapper.MapLambda((string cs) => LibRedConnection.DatabaseExists(cs))),
				createDatabase,
				typeMapper.BuildAction<string>(typeMapper.MapActionLambda((string cs) => LibRedConnection.DropDatabase(cs))),
				typeMapper.BuildAction<DbConnection>(typeMapper.MapActionLambda((LibRedConnection connection) => LibRedConnection.ClearPool(connection))));
		}

		sealed record DatabaseManagement(
			Func  <string, bool> DatabaseExists,
			Action<string, bool> CreateDatabase,
			Action<string>       DropDatabase,
			Action<DbConnection> ClearPool);

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
