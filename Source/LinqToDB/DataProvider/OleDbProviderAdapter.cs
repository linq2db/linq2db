using System;
using System.Data;
using LinqToDB.Expressions;

namespace LinqToDB.DataProvider
{
	public class OleDbProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static OleDbProviderAdapter? _instance;

		public const string AssemblyName = "System.Data.OleDb";

		private OleDbProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Action<IDbDataParameter, OleDbType> dbTypeSetter,
			Func  <IDbDataParameter, OleDbType> dbTypeGetter,
			Func  <IDbConnection, Guid, object[]?, DataTable> schemaTableGetter)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			GetOleDbSchemaTable = schemaTableGetter;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Action<IDbDataParameter, OleDbType> SetDbType { get; }
		public Func  <IDbDataParameter, OleDbType> GetDbType { get; }

		public Func<IDbConnection, Guid, object[]?, DataTable> GetOleDbSchemaTable { get; }

		public static OleDbProviderAdapter GetInstance()
		{
			if (_instance == null)
			{
				lock (_syncRoot)
				{
					if (_instance == null)
					{
#if NET45 || NET46
						var assembly = typeof(System.Data.OleDb.OleDbConnection).Assembly;
#else
						var assembly = LinqToDB.Common.Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");
#endif

						var connectionType  = assembly.GetType("System.Data.OleDb.OleDbConnection" , true);
						var dataReaderType  = assembly.GetType("System.Data.OleDb.OleDbDataReader" , true);
						var parameterType   = assembly.GetType("System.Data.OleDb.OleDbParameter"  , true);
						var commandType     = assembly.GetType("System.Data.OleDb.OleDbCommand"    , true);
						var transactionType = assembly.GetType("System.Data.OleDb.OleDbTransaction", true);
						var dbType          = assembly.GetType("System.Data.OleDb.OleDbType"       , true);

						var typeMapper = new TypeMapper(connectionType, parameterType, dbType);
						typeMapper.RegisterWrapper<OleDbType>();
						typeMapper.RegisterWrapper<OleDbParameter>();
						typeMapper.RegisterWrapper<OleDbConnection>();

						var dbTypeBuilder = typeMapper.Type<OleDbParameter>().Member(p => p.OleDbType);
						var typeSetter    = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						var typeGetter    = dbTypeBuilder.BuildGetter<IDbDataParameter>();

						var oleDbSchemaTableGetter = typeMapper.BuildFunc<IDbConnection, Guid, object[]?, DataTable>(typeMapper.MapLambda((OleDbConnection conn, Guid schema, object[]? restrictions) => conn.GetOleDbSchemaTable(schema, restrictions)));

						_instance = new OleDbProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							typeSetter,
							typeGetter,
							oleDbSchemaTableGetter);
					}
				}
			}

			return _instance;
		}

		#region Wrappers

		// not wrapper, just copy of constant
		public static class OleDbSchemaGuid
		{
			public static readonly Guid Foreign_Keys = new Guid(3367314116, 23795, 4558, 173, 229, 0, 170, 0, 68, 119, 61);
		}

		[Wrapper]
		internal class OleDbParameter
		{
			public OleDbType OleDbType { get; set; }
		}

		[Wrapper]
		internal class OleDbConnection
		{
			public DataTable GetOleDbSchemaTable(Guid schema, object[]? restrictions) => throw new NotImplementedException();
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
		#endregion
	}
}
