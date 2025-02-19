using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

using LinqToDB.DataProvider;
using LinqToDB.Internal.Expressions.Types;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Lock _syncRoot = new();
		private static SqlCeProviderAdapter? _instance;

		public const string AssemblyName        = "System.Data.SqlServerCe";
		public const string ClientNamespace     = "System.Data.SqlServerCe";
		public const string ProviderFactoryName = "System.Data.SqlServerCe.4.0";

		private SqlCeProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,

			Action<DbParameter, SqlDbType>   dbTypeSetter,
			Func  <DbParameter, SqlDbType>   dbTypeGetter,
			Func  <string,      SqlCeEngine> sqlCeEngineCreator)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;
			_connectionFactory = connectionFactory;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			CreateSqlCeEngine = sqlCeEngineCreator;
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

		public Action<DbParameter, SqlDbType> SetDbType { get; }
		public Func  <DbParameter, SqlDbType> GetDbType { get; }

		public Func<string, SqlCeEngine> CreateSqlCeEngine { get; }

		public static SqlCeProviderAdapter GetInstance()
		{
			if (_instance == null)
			{
				lock (_syncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
					if (_instance == null)
#pragma warning restore CA1508 // Avoid dead conditional code
					{
						var assembly = LinqToDB.Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.SqlCeConnection" , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.SqlCeDataReader" , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.SqlCeParameter"  , true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.SqlCeCommand"    , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.SqlCeTransaction", true)!;
						var sqlCeEngine     = assembly.GetType($"{ClientNamespace}.SqlCeEngine"     , true)!;

						var typeMapper = new TypeMapper();
						typeMapper.RegisterTypeWrapper<SqlCeConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<SqlCeEngine>(sqlCeEngine);
						typeMapper.RegisterTypeWrapper<SqlCeParameter>(parameterType);
						typeMapper.FinalizeMappings();

						var dbTypeBuilder = typeMapper.Type<SqlCeParameter>().Member(p => p.SqlDbType);
						var typeSetter    = dbTypeBuilder.BuildSetter<DbParameter>();
						var typeGetter    = dbTypeBuilder.BuildGetter<DbParameter>();

						var connectionFactory = typeMapper.BuildTypedFactory<string, SqlCeConnection, DbConnection>((connectionString) => new SqlCeConnection(connectionString));

						_instance = new SqlCeProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							connectionFactory,
							typeSetter,
							typeGetter,
							typeMapper.BuildWrappedFactory((string connectionString) => new SqlCeEngine(connectionString))!);
					}
			}

			return _instance;
		}

		#region Wrappers

		[Wrapper]
		public class SqlCeEngine : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: CreateDatabase
				(SqlCeEngine this_) => this_.CreateDatabase(),
				// [1]: Dispose
				(SqlCeEngine this_) => this_.Dispose(),
			};

			public SqlCeEngine(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public SqlCeEngine(string connectionString) => throw new NotImplementedException();

			public void CreateDatabase() => ((Action<SqlCeEngine>)CompiledWrappers[0])(this);
			public void Dispose()        => ((Action<SqlCeEngine>)CompiledWrappers[1])(this);
		}

		[Wrapper]
		private sealed class SqlCeParameter
		{
			public SqlDbType SqlDbType { get; set; }
		}

		[Wrapper]
		private sealed class SqlCeConnection
		{
			public SqlCeConnection(string connectionString) => throw new NotImplementedException();
		}

		#endregion
	}
}
