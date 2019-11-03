using System;
using System.Data;
using System.Reflection;
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

			internal static Type?                                            ParameterType;
			internal static Type?                                            ConnectionType;
			internal static Action<IDbDataParameter, OleDbType>?             TypeSetter;
			internal static Func<IDbDataParameter, OleDbType>?               TypeGetter;
			internal static Func<IDbConnection, Guid, object[]?, DataTable>? OleDbSchemaTableGetter;

			internal static void Initialize(Assembly assembly)
			{
				if (_oleDbTypeMapper == null)
				{
					lock (_syncRoot)
					{
						if (_oleDbTypeMapper == null)
						{
							ConnectionType = assembly.GetType("System.Data.OleDb.OleDbConnection", true);
							ParameterType  = assembly.GetType("System.Data.OleDb.OleDbParameter",  true);
							var dbType     = assembly.GetType("System.Data.OleDb.OleDbType",       true);

							_oleDbTypeMapper  = new TypeMapper(ConnectionType, ParameterType, dbType);

							var dbTypeBuilder      = _oleDbTypeMapper.Type<OleDbParameter>().Member(p => p.OleDbType);
							TypeSetter             = dbTypeBuilder.BuildSetter<IDbDataParameter>();
							TypeGetter             = dbTypeBuilder.BuildGetter<IDbDataParameter>();

							OleDbSchemaTableGetter = (Func<IDbConnection, Guid, object[]?, DataTable>)_oleDbTypeMapper.MapLambda((OleDbConnection conn, Guid schema, object[]? restrictions) => conn.GetOleDbSchemaTable(schema, restrictions)).Compile();
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
				BigInt,
				Binary,
				Boolean,
				BSTR,
				Char,
				Currency,
				Date,
				DBDate,
				DBTime,
				DBTimeStamp,
				Decimal,
				Double,
				Empty,
				Error,
				Filetime,
				Guid,
				IDispatch,
				Integer,
				IUnknown,
				LongVarBinary,
				LongVarChar,
				LongVarWChar,
				Numeric,
				PropVariant,
				Single,
				SmallInt,
				TinyInt,
				UnsignedBigInt,
				UnsignedInt,
				UnsignedSmallInt,
				UnsignedTinyInt,
				VarBinary,
				VarChar,
				Variant,
				VarNumeric,
				VarWChar,
				WChar
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

		//private static Type GetDynamicType(string typeName, Assembly assemly)
		//{
		//	return assemly.GetType(typeName, true);
		//}


		//internal static Action<IDbDataParameter, OdbcType> GetODbcTypeSetter(Assembly fromAssembly)
		//{
		//	var parameterType = GetDynamicType("System.Data.Odbc.OdbcParameter", fromAssembly);
		//	var dbType = GetDynamicType("System.Data.Odbc.OdbcType", fromAssembly);

		//	var mapper = new TypeMapper(parameterType, dbType);

		//	return mapper.Type<OdbcParameter>().Member(p => p.OdbcType).BuildSetter<IDbDataParameter>();
		//}
	}

	#region ODBC wrappers
	[Wrapper]
	public enum OdbcType
	{
		BigInt,
		Binary,
		Bit,
		Char,
		Date,
		DateTime,
		Decimal,
		Double,
		Image,
		Int,
		NChar,
		NText,
		Numeric,
		NVarChar,
		Real,
		SmallDateTime,
		SmallInt,
		Text,
		Time,
		Timestamp,
		TinyInt,
		UniqueIdentifier,
		VarBinary,
		VarChar
	}

	[Wrapper]
	internal class OdbcParameter
	{
		public OdbcType OdbcType { get; set; }
	}

	#endregion

}
