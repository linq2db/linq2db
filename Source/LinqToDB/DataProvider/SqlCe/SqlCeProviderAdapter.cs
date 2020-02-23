using System;
using System.Data;

namespace LinqToDB.DataProvider.SqlCe
{
	using System.Linq.Expressions;
	using LinqToDB.Expressions;

	public class SqlCeProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
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
			Action<IDbDataParameter, SqlDbType>   dbTypeSetter,
			Func  <IDbDataParameter, SqlDbType>   dbTypeGetter,
			Func  <string,           SqlCeEngine> sqlCeEngineCreator)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			CreateSqlCeEngine = sqlCeEngineCreator;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Action<IDbDataParameter, SqlDbType> SetDbType { get; }
		public Func  <IDbDataParameter, SqlDbType> GetDbType { get; }

		public Func<string, SqlCeEngine> CreateSqlCeEngine { get; }

		public static SqlCeProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.SqlCeConnection" , true);
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.SqlCeDataReader" , true);
						var parameterType   = assembly.GetType($"{ClientNamespace}.SqlCeParameter"  , true);
						var commandType     = assembly.GetType($"{ClientNamespace}.SqlCeCommand"    , true);
						var transactionType = assembly.GetType($"{ClientNamespace}.SqlCeTransaction", true);
						var sqlCeEngine     = assembly.GetType($"{ClientNamespace}.SqlCeEngine"     , true);

						var typeMapper = new TypeMapper();
						typeMapper.RegisterTypeWrapper<SqlCeEngine>(sqlCeEngine);
						typeMapper.RegisterTypeWrapper<SqlCeParameter>(parameterType);
						typeMapper.FinalizeMappings();

						var dbTypeBuilder = typeMapper.Type<SqlCeParameter>().Member(p => p.SqlDbType);
						var typeSetter    = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						var typeGetter    = dbTypeBuilder.BuildGetter<IDbDataParameter>();

						_instance = new SqlCeProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							typeSetter,
							typeGetter,
							typeMapper.BuildWrappedFactory((string connectionString) => new SqlCeEngine(connectionString))!);
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
				(Expression<Action<SqlCeEngine>>)((SqlCeEngine this_) => this_.CreateDatabase()),
				// [1]: Dispose
				(Expression<Action<SqlCeEngine>>)((SqlCeEngine this_) => this_.Dispose()),
			};

			public SqlCeEngine(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
			}

			public SqlCeEngine(string connectionString) => throw new NotImplementedException();

			public void CreateDatabase() => ((Action<SqlCeEngine>)CompiledWrappers[0])(this);
			public void Dispose()        => ((Action<SqlCeEngine>)CompiledWrappers[1])(this);
		}

		[Wrapper]
		private class SqlCeParameter
		{
			public SqlDbType SqlDbType { get; set; }
		}

		#endregion
	}
}
