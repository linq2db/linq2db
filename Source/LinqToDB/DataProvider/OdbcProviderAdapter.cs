using System;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.DataProvider
{
	public class OdbcProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new ();
		private static OdbcProviderAdapter? _instance;

		public const string AssemblyName    = "System.Data.Odbc";
		public const string ClientNamespace = "System.Data.Odbc";

		private OdbcProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, OdbcConnection>       createConnection,
			Action<IDbDataParameter, OdbcType> dbTypeSetter,
			Func  <IDbDataParameter, OdbcType> dbTypeGetter)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			_createConnection = createConnection;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Action<IDbDataParameter, OdbcType> SetDbType { get; }
		public Func  <IDbDataParameter, OdbcType> GetDbType { get; }

		private readonly Func<string, OdbcConnection> _createConnection;
		public OdbcConnection CreateConnection(string connectionString) => _createConnection(connectionString);

		public static OdbcProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
#if NETFRAMEWORK
						var assembly = typeof(System.Data.Odbc.OdbcConnection).Assembly;
#else
						var assembly = LinqToDB.Common.Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");
#endif

						var connectionType  = assembly.GetType($"{ClientNamespace}.OdbcConnection" , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.OdbcDataReader" , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.OdbcParameter"  , true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.OdbcCommand"    , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.OdbcTransaction", true)!;
						var dbType          = assembly.GetType($"{ClientNamespace}.OdbcType", true)!;

						var typeMapper = new TypeMapper();
						typeMapper.RegisterTypeWrapper<OdbcConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<OdbcType>(dbType);
						typeMapper.RegisterTypeWrapper<OdbcParameter>(parameterType);
						typeMapper.FinalizeMappings();

						var dbTypeBuilder = typeMapper.Type<OdbcParameter>().Member(p => p.OdbcType);
						var typeSetter    = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						var typeGetter    = dbTypeBuilder.BuildGetter<IDbDataParameter>();

						_instance = new OdbcProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							typeMapper.BuildWrappedFactory((string connectionString) => new OdbcConnection(connectionString)),
							typeSetter,
							typeGetter);
					}

			return _instance;
		}

		#region Wrappers

		[Wrapper]
		private class OdbcParameter
		{
			public OdbcType OdbcType { get; set; }
		}

		[Wrapper]
		public enum OdbcType
		{
			BigInt           = 1,
			Binary           = 2,
			Bit              = 3,
			Char             = 4,
			Date             = 23,
			DateTime         = 5,
			Decimal          = 6,
			Double           = 8,
			Image            = 9,
			Int              = 10,
			NChar            = 11,
			NText            = 12,
			Numeric          = 7,
			NVarChar         = 13,
			Real             = 14,
			SmallDateTime    = 16,
			SmallInt         = 17,
			Text             = 18,
			Time             = 24,
			Timestamp        = 19,
			TinyInt          = 20,
			UniqueIdentifier = 15,
			VarBinary        = 21,
			VarChar          = 22
		}

		[Wrapper]
		public class OdbcConnection : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: CreateCommand
				(Expression<Func<OdbcConnection, IDbCommand>>)((OdbcConnection this_) => this_.CreateCommand()),
				// [1]: Open
				(Expression<Action<OdbcConnection>>          )((OdbcConnection this_) => this_.Open()),
				// [2]: Dispose
				(Expression<Action<OdbcConnection>>          )((OdbcConnection this_) => this_.Dispose()),
			};

			public OdbcConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public OdbcConnection(string connectionString) => throw new NotImplementedException();

			public IDbCommand CreateCommand() => ((Func<OdbcConnection, IDbCommand>)CompiledWrappers[0])(this);
			public void Open()                => ((Action<OdbcConnection>)CompiledWrappers[1])(this);
			public void Dispose()             => ((Action<OdbcConnection>)CompiledWrappers[2])(this);
		}
		#endregion
	}
}
