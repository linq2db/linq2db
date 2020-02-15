using System;
using System.Data;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Tests.TypeMapping
{
	namespace OracleWrappers
	{
		[Wrapper]
		public enum OracleDbType
		{
			BFile,
			Blob,
			Byte,
			Char,
			Clob,
			Date,
			Decimal,
			Double,
			Long,
			LongRaw,
			Int16,
			Int32,
			Int64,
			IntervalDS,
			IntervalYM,
			NClob,
			NChar,
			NVarchar2,
			Raw,
			RefCursor,
			Single,
			TimeStamp,
			TimeStampLTZ,
			TimeStampTZ,
			Varchar2,
			XmlType,
			BinaryDouble,
			BinaryFloat,
			Boolean,
		}

		[Wrapper]
		internal class OracleParameter
		{
			public OracleDbType OracleDbType { get; set; }
		}

		[Wrapper]
		internal class OracleDataReader
		{
			public OracleDate GetOracleDate(int idx) => throw new NotImplementedException();
		}

		[Wrapper]
		internal class GetOracleDate
		{

		}
	}

	[TestFixture]
	public class OracleWrappingTests : TestBase
	{

		private Type GetDynamicType(string dbTypeName, Type connectionType)
		{
			var dbType = connectionType.Assembly.GetType(dbTypeName.Contains(".") ? dbTypeName : connectionType.Namespace + "." + dbTypeName, true);
			return dbType;
		}

		private Type GetDynamicTypesType(string dbTypeName, Type connectionType)
		{
			var dbType = connectionType.Assembly.GetType("Oracle.ManagedDataAccess" + ".Types." + dbTypeName, true);
			return dbType;
		}

		[Test]
		public void Test([IncludeDataSources(false, TestProvName.AllOracleManaged)] string context)
		{
			var prov = DataConnection.GetDataProvider(context);

			var connectionType = ((OracleDataProvider)prov).Adapter.ConnectionType;

			var oracleParameter  = GetDynamicType("OracleParameter", connectionType);
			var oracleDbType     = GetDynamicType("OracleDbType", connectionType);
			var oracleDataReader = GetDynamicType("OracleDataReader", connectionType);

			var oracleDate = GetDynamicTypesType("OracleDate", connectionType);

			var oracleMapper = new TypeMapper(oracleParameter, oracleDbType, oracleDataReader, oracleDate);


//			oracleMapper.MapSetter<OracleParameter>(p => p.OracleDbType, () => OracleDbType.Date);

			var instance = new Oracle.ManagedDataAccess.Client.OracleParameter();

			oracleMapper.SetValue<OracleParameter>(instance, p => p.OracleDbType, OracleDbType.Date);

			var action = oracleMapper.Type<OracleParameter>().Member(p => p.OracleDbType).BuildSetter<IDbDataParameter>(OracleDbType.Single);
			action(instance);

			var setterAction = oracleMapper.Type<OracleParameter>().Member(p => p.OracleDbType).BuildSetter<IDbDataParameter>();
			setterAction(instance, OracleDbType.Blob);

			var expr = oracleMapper.MapLambda((OracleDataReader r, int i) => r.GetOracleDate(i));

		}
	}
}
