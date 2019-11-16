using System;
using System.Data;
using LinqToDB.Expressions;

namespace LinqToDB.DataProvider.Wrappers
{
	// oledb and odbc mappings moved to separate class from access and hana providers for reuse by other
	// oledb/odbc providers in future (still it will make sense to create base oledb and odbc providers in that case)
	internal static class Mappers
	{
		internal class OleDb
		{
			private static readonly object _syncRoot = new object();
			private static TypeMapper?     _oleDbTypeMapper;

			internal static Type                                            ParameterType = null!;
			internal static Type                                            ConnectionType = null!;
			internal static Action<IDbDataParameter, OleDbType>             TypeSetter = null!;
			internal static Func<IDbDataParameter, OleDbType>               TypeGetter = null!;
			internal static Func<IDbConnection, Guid, object[]?, DataTable> OleDbSchemaTableGetter = null!;

			internal static void Initialize()
			{
				if (_oleDbTypeMapper == null)
				{
					lock (_syncRoot)
					{
						if (_oleDbTypeMapper == null)
						{
#if NET45 || NET46
							ConnectionType = typeof(System.Data.OleDb.OleDbConnection);
#else
							ConnectionType = Type.GetType("System.Data.OleDb.OleDbConnection, System.Data.OleDb", true);
#endif
							ParameterType  = ConnectionType.Assembly.GetType("System.Data.OleDb.OleDbParameter",  true);
							var dbType     = ConnectionType.Assembly.GetType("System.Data.OleDb.OleDbType",       true);

							_oleDbTypeMapper  = new TypeMapper(ConnectionType, ParameterType, dbType);
							_oleDbTypeMapper.RegisterWrapper<OleDbType>();
							_oleDbTypeMapper.RegisterWrapper<OleDbParameter>();
							_oleDbTypeMapper.RegisterWrapper<OleDbConnection>();

							var dbTypeBuilder      = _oleDbTypeMapper.Type<OleDbParameter>().Member(p => p.OleDbType);
							TypeSetter             = dbTypeBuilder.BuildSetter<IDbDataParameter>();
							TypeGetter             = dbTypeBuilder.BuildGetter<IDbDataParameter>();

							OleDbSchemaTableGetter = _oleDbTypeMapper.BuildFunc<IDbConnection, Guid, object[]?, DataTable>(_oleDbTypeMapper.MapLambda((OleDbConnection conn, Guid schema, object[]? restrictions) => conn.GetOleDbSchemaTable(schema, restrictions)));
						}
					}
				}
			}

			#region OleDb wrappers
			// not wrapper, just copy of constant
			internal static class OleDbSchemaGuid
			{
				public static readonly Guid Foreign_Keys = new Guid(3367314116, 23795, 4558, 173, 229, 0, 170, 0, 68, 119, 61);
			}

			[Wrapper]
			internal enum OleDbType
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
			#endregion
		}
	}

	// current ODBC provider doesn't need any ODBC types
	//#region ODBC wrappers
	//[Wrapper]
	//public enum OdbcType
	//{
	//	BigInt,
	//	Binary,
	//	Bit,
	//	Char,
	//	Date,
	//	DateTime,
	//	Decimal,
	//	Double,
	//	Image,
	//	Int,
	//	NChar,
	//	NText,
	//	Numeric,
	//	NVarChar,
	//	Real,
	//	SmallDateTime,
	//	SmallInt,
	//	Text,
	//	Time,
	//	Timestamp,
	//	TinyInt,
	//	UniqueIdentifier,
	//	VarBinary,
	//	VarChar
	//}

	//[Wrapper]
	//internal class OdbcParameter
	//{
	//	public OdbcType OdbcType { get; set; }
	//}

	//#endregion

}
