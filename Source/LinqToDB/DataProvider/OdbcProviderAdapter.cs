using System;
using System.Data.Common;
using System.Linq.Expressions;

using LinqToDB.Expressions.Types;

namespace LinqToDB.DataProvider
{
	public class OdbcProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static OdbcProviderAdapter? _instance;

		public const string AssemblyName    = "System.Data.Odbc";
		public const string ClientNamespace = "System.Data.Odbc";

		private OdbcProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,
			Action<DbParameter, OdbcType> dbTypeSetter,
			Func  <DbParameter, OdbcType> dbTypeGetter,
			Func<DbConnection, OdbcConnection> connectionWrapper)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			ConnectionWrapper = connectionWrapper;
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

		public Action<DbParameter, OdbcType> SetDbType { get; }
		public Func  <DbParameter, OdbcType> GetDbType { get; }

		internal Func<DbConnection, OdbcConnection> ConnectionWrapper { get; }

		public static OdbcProviderAdapter GetInstance()
		{
			if (_instance == null)
			{
				lock (_syncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
					if (_instance == null)
#pragma warning restore CA1508 // Avoid dead conditional code
					{
#if NETFRAMEWORK
						var assembly = typeof(System.Data.Odbc.OdbcConnection).Assembly;
#else
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null);
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
						typeMapper.RegisterTypeWrapper<OdbcType>(dbType);
						typeMapper.RegisterTypeWrapper<OdbcParameter>(parameterType);
						typeMapper.RegisterTypeWrapper<OdbcConnection>(connectionType);
						typeMapper.FinalizeMappings();

						var connectionFactory = typeMapper.BuildTypedFactory<string, OdbcConnection, DbConnection>((string connectionString) => new OdbcConnection(connectionString));

						var dbTypeBuilder = typeMapper.Type<OdbcParameter>().Member(p => p.OdbcType);
						var typeSetter    = dbTypeBuilder.BuildSetter<DbParameter>();
						var typeGetter    = dbTypeBuilder.BuildGetter<DbParameter>();

						_instance = new OdbcProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							connectionFactory,
							typeSetter,
							typeGetter,
							typeMapper.Wrap<OdbcConnection>);
					}
			}

			return _instance;
		}

		#region Wrappers

		[Wrapper]
		internal sealed class OdbcConnection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; } =
			{
				// [0]: get Driver
				(Expression<Func<OdbcConnection, string>>)((OdbcConnection this_) => this_.Driver),
			};

			public OdbcConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public OdbcConnection(string connectionString) => throw new NotImplementedException();

			// implementation returns string.Empty instead of null
			public string Driver => ((Func<OdbcConnection, string>)CompiledWrappers[0])(this);
		}

		[Wrapper]
		private sealed class OdbcParameter
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
		#endregion
	}
}
