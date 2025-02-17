using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

using LinqToDB.Expressions.Types;

namespace LinqToDB.DataProvider
{
	public class OleDbProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Lock _syncRoot = new();
		private static OleDbProviderAdapter? _instance;

		public const string AssemblyName    = "System.Data.OleDb";
		public const string ClientNamespace = "System.Data.OleDb";

		private OleDbProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,

			Action<DbParameter, OleDbType> dbTypeSetter,
			Func  <DbParameter, OleDbType> dbTypeGetter,
			Func  <DbConnection, Guid, object[]?, DataTable> schemaTableGetter,
			Func<DbConnection, OleDbConnection> connectionWrapper)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			GetOleDbSchemaTable = schemaTableGetter;

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

		public Action<DbParameter, OleDbType> SetDbType { get; }
		public Func  <DbParameter, OleDbType> GetDbType { get; }

		public Func<DbConnection, Guid, object[]?, DataTable> GetOleDbSchemaTable { get; }

		internal Func<DbConnection, OleDbConnection> ConnectionWrapper { get; }

		public static OleDbProviderAdapter GetInstance()
		{
			if (_instance == null)
			{
				lock (_syncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
					if (_instance == null)
#pragma warning restore CA1508 // Avoid dead conditional code
					{
#if NETFRAMEWORK
						var assembly = typeof(System.Data.OleDb.OleDbConnection).Assembly;
#else
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");
#endif

						var connectionType  = assembly.GetType($"{ClientNamespace}.OleDbConnection" , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.OleDbDataReader" , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.OleDbParameter"  , true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.OleDbCommand"    , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.OleDbTransaction", true)!;
						var dbType          = assembly.GetType($"{ClientNamespace}.OleDbType"       , true)!;

						var typeMapper = new TypeMapper();
						typeMapper.RegisterTypeWrapper<OleDbConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<OleDbType>(dbType);
						typeMapper.RegisterTypeWrapper<OleDbParameter>(parameterType);
						typeMapper.FinalizeMappings();

						var dbTypeBuilder = typeMapper.Type<OleDbParameter>().Member(p => p.OleDbType);
						var typeSetter    = dbTypeBuilder.BuildSetter<DbParameter>();
						var typeGetter    = dbTypeBuilder.BuildGetter<DbParameter>();

						var oleDbSchemaTableGetter = typeMapper.BuildFunc<DbConnection, Guid, object[]?, DataTable>(typeMapper.MapLambda((OleDbConnection conn, Guid schema, object[]? restrictions) => conn.GetOleDbSchemaTable(schema, restrictions)));

						_instance = new OleDbProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							typeMapper.BuildTypedFactory<string, OleDbConnection, DbConnection>((string connectionString) => new OleDbConnection(connectionString)),
							typeSetter,
							typeGetter,
							oleDbSchemaTableGetter,
							typeMapper.Wrap<OleDbConnection>);
					}
			}

			return _instance;
		}

		#region Wrappers

		// not wrapper, just copy of constant
		internal static class OleDbSchemaGuid
		{
			public static readonly Guid Foreign_Keys = new Guid(3367314116, 23795, 4558, 173, 229, 0, 170, 0, 68, 119, 61);
		}

		[Wrapper]
		private sealed class OleDbParameter
		{
			public OleDbType OleDbType { get; set; }
		}

		[Wrapper]
		internal sealed class OleDbConnection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; } =
			{
				// [0]: get Provider
				(Expression<Func<OleDbConnection, string>>)((OleDbConnection this_) => this_.Provider),
			};

			public OleDbConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public OleDbConnection(string connectionString) => throw new NotImplementedException();

			public DataTable GetOleDbSchemaTable(Guid schema, object[]? restrictions) => throw new NotImplementedException();

			// implementation returns string.Empty instead of null
			public string Provider => ((Func<OleDbConnection, string>)CompiledWrappers[0])(this);
		}

		[Wrapper]
		public enum OleDbType
		{
			BigInt           = 20,
			Binary           = 128,
			Boolean          = 11,
			BSTR             = 8,
			Char             = 129,
			Currency         = 6,
			Date             = 7,
			DBDate           = 133,
			DBTime           = 134,
			DBTimeStamp      = 135,
			Decimal          = 14,
			Double           = 5,
			Empty            = 0,
			Error            = 10,
			Filetime         = 64,
			Guid             = 72,
			IDispatch        = 9,
			Integer          = 3,
			IUnknown         = 13,
			LongVarBinary    = 205,
			LongVarChar      = 201,
			LongVarWChar     = 203,
			Numeric          = 131,
			PropVariant      = 138,
			Single           = 4,
			SmallInt         = 2,
			TinyInt          = 16,
			UnsignedBigInt   = 21,
			UnsignedInt      = 19,
			UnsignedSmallInt = 18,
			UnsignedTinyInt  = 17,
			VarBinary        = 204,
			VarChar          = 200,
			Variant          = 12,
			VarNumeric       = 139,
			VarWChar         = 202,
			WChar            = 130
		}

		// not wrapper, OLE DB enum
		// https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ms722704%28v%3dvs.85%29
		/// <summary>
		/// DBCOLUMNFLAGS OLE DB enumeration.
		/// https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ms722704(v=vs.85).
		/// </summary>
		[Flags]
		public enum ColumnFlags : long
		{
			IsBookmark        = 0x00001,
			MayDefer          = 0x00002,
			Write             = 0x00004,
			WriteUnknown      = 0x00008,
			IsFixedLength     = 0x00010,
			IsNullable        = 0x00020,
			MayBeNull         = 0x00040,
			IsLong            = 0x00080,
			IsRowId           = 0x00100,
			IsRowVer          = 0x00200,
			CacheDeferred     = 0x00400,
			ScaleIsNegative   = 0x00800,
			Reserved          = 0x01000,
			IsRowUrl          = 0x02000,
			IsDefaultStream   = 0x04000,
			IsCollection      = 0x08000,
			IsStream          = 0x10000,
			IsRowset          = 0x20000,
			IsRow             = 0x40000,
			RowSpecificColumn = 0x80000,
		}
		#endregion
	}
}
