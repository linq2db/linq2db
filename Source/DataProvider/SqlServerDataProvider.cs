using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;

namespace LinqToDB.DataProvider
{
	using Common;
	using Expressions;

	public class SqlServerDataProvider : DataProviderBase
	{
		public SqlServerDataProvider()
		{
			Version = SqlServerVersion.v2008;
			Init();
		}

		public SqlServerDataProvider(SqlServerVersion version)
		{
			Version = version;
			Init();
		}

		void Init()
		{
			MappingSchema.SetConvertExpression<SqlXml,XmlReader>(s => s.IsNull ? DefaultValue<XmlReader>.Value : s.CreateReader());

			MappingSchema.SetDefaultValue(SqlXml.Null);
		}

		public override string           Name         { get { return LinqToDB.ProviderName.SqlServer; } }
		public override string           ProviderName { get { return typeof(SqlConnection).Namespace; } }
		public          SqlServerVersion Version      { get; set; }

		public override IDbConnection CreateConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}

		public override void Configure(string name, string value)
		{
			if (name == "version") switch (value)
			{
				case "2005" : Version = SqlServerVersion.v2005; break;
				case "2008" : Version = SqlServerVersion.v2008; break;
				case "2012" : Version = SqlServerVersion.v2012; break;
			}
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(SqlDataReader));
		}

		public override Expression GetReaderExpression(IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			if (toType == typeof(DateTimeOffset))
				return (Expression<Func<SqlDataReader, DateTimeOffset>>)(rd => rd.GetDateTimeOffset(idx));

			if (toType == typeof(TimeSpan))
				return (Expression<Func<SqlDataReader, TimeSpan>>)(rd => rd.GetTimeSpan(idx));

			return base.GetReaderExpression(reader, idx, readerExpression, toType);
		}

		protected override MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx)
		{
			var mi = base.GetReaderMethodInfo(reader, idx);

			if (mi != null)
				return mi;

			var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

			if (type == typeof(SqlBinary))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlBinary  (0));
			if (type == typeof(SqlBoolean))  return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlBoolean (0));
			if (type == typeof(SqlByte))     return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlByte    (0));
			if (type == typeof(SqlDateTime)) return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDateTime(0));
			if (type == typeof(SqlDecimal))  return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDecimal (0));
			if (type == typeof(SqlDouble))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDouble  (0));
			if (type == typeof(SqlGuid))     return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlGuid    (0));
			if (type == typeof(SqlInt16))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt16   (0));
			if (type == typeof(SqlInt32))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt32   (0));
			if (type == typeof(SqlInt64))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt64   (0));
			if (type == typeof(SqlMoney))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlMoney   (0));
			if (type == typeof(SqlSingle))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlSingle  (0));
			if (type == typeof(SqlString))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlString  (0));
			if (type == typeof(SqlXml))      return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlXml     (0));

			//if (type == typeof(SqlFileStream))

			return null;
		}
	}
}
