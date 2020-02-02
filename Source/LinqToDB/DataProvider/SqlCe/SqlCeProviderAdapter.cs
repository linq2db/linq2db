using System;
using System.Data;

namespace LinqToDB.DataProvider.SqlCe
{
	using System.Data.Common;
	using LinqToDB.Expressions;

	public class SqlCeProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static SqlCeProviderAdapter? _instance;

		public const string AssemblyName = "System.Data.SqlServerCe";

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
			{
				lock (_syncRoot)
				{
					if (_instance == null)
					{
						var assembly = Type.GetType($"System.Data.SqlServerCe.SqlCeConnection, {AssemblyName}", false)?.Assembly
#if !NETSTANDARD2_0
							?? DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0").GetType().Assembly
#endif
							;

						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType("System.Data.SqlServerCe.SqlCeConnection", true);
						var dataReaderType  = assembly.GetType("System.Data.SqlServerCe.SqlCeDataReader", true);
						var parameterType   = assembly.GetType("System.Data.SqlServerCe.SqlCeParameter", true);
						var commandType     = assembly.GetType("System.Data.SqlServerCe.SqlCeCommand", true);
						var transactionType = assembly.GetType("System.Data.SqlServerCe.SqlCeTransaction", true);

						var sqlCeEngine     = assembly.GetType("System.Data.SqlServerCe.SqlCeEngine", true);

						var typeMapper = new TypeMapper(parameterType, sqlCeEngine);
						typeMapper.RegisterWrapper<SqlCeEngine>();
						typeMapper.RegisterWrapper<SqlCeParameter>();

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
							(string connectionString) => typeMapper.CreateAndWrap(() => new SqlCeEngine(connectionString))!);
					}
				}
			}

			return _instance;
		}

		#region Wrappers

		[Wrapper]
		public class SqlCeEngine : TypeWrapper, IDisposable
		{
			public SqlCeEngine(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlCeEngine(string connectionString) => throw new NotImplementedException();

			public void CreateDatabase() => this.WrapAction(t => t.CreateDatabase());
			public void Dispose()        => this.WrapAction(t => t.Dispose());
		}

		[Wrapper]
		internal class SqlCeParameter
		{
			public SqlDbType SqlDbType { get; set; }
		}

		#endregion
	}
}
