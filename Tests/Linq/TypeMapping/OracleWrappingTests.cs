using System;
using System.Data.Common;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.Oracle;
using LinqToDB.Internal.Expressions.Types;

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
		internal sealed class OracleParameter
		{
			public OracleDbType OracleDbType { get; set; }
		}

		[Wrapper]
		internal sealed class OracleDataReader
		{
			public OracleDate GetOracleDate(int idx) => throw new NotImplementedException();
		}
	}

	[TestFixture]
	public class OracleWrappingTests : TestBase
	{

		private Type GetDynamicType(string dbTypeName, Type connectionType)
		{
			var dbType = connectionType.Assembly.GetType(dbTypeName.Contains(".") ? dbTypeName : connectionType.Namespace + "." + dbTypeName, true);
			return dbType!;
		}

		private Type GetDynamicTypesType(string dbTypeName, Type connectionType)
		{
			var dbType = connectionType.Assembly.GetType("Oracle.ManagedDataAccess" + ".Types." + dbTypeName, true);
			return dbType!;
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

			var oracleMapper = new TypeMapper();
			oracleMapper.RegisterTypeWrapper<OracleWrappers.OracleParameter>(oracleParameter);
			oracleMapper.RegisterTypeWrapper<OracleWrappers.OracleDbType>(oracleDbType);
			oracleMapper.RegisterTypeWrapper<OracleWrappers.OracleDataReader>(oracleDataReader);

			oracleMapper.FinalizeMappings();

			using var instance = new Oracle.ManagedDataAccess.Client.OracleParameter();

			var setterAction = oracleMapper.Type<OracleParameter>().Member(p => p.OracleDbType).BuildSetter<DbParameter>();
			setterAction(instance, OracleDbType.Blob);

			var expr = oracleMapper.MapLambda((OracleDataReader r, int i) => r.GetOracleDate(i));

		}
	}
}
