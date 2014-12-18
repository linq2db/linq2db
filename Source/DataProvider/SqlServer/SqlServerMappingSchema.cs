using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Xml;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;

	using Expressions;

	using Mapping;

	public class SqlServerMappingSchema : MappingSchema
	{
		public SqlServerMappingSchema()
			: base(ProviderName.SqlServer)
		{
			SetConvertExpression<SqlXml,XmlReader>(
				s => s.IsNull ? DefaultValue<XmlReader>.Value : s.CreateReader(),
				s => s.CreateReader());

			SetConvertExpression<string,SqlXml>(s => new SqlXml(new MemoryStream(Encoding.UTF8.GetBytes(s))));

			AddScalarType(typeof(SqlBinary),   SqlBinary.  Null, true, DataType.VarBinary);
			AddScalarType(typeof(SqlBoolean),  SqlBoolean. Null, true, DataType.Boolean);
			AddScalarType(typeof(SqlByte),     SqlByte.    Null, true, DataType.Byte);
			AddScalarType(typeof(SqlDateTime), SqlDateTime.Null, true, DataType.DateTime);
			AddScalarType(typeof(SqlDecimal),  SqlDecimal. Null, true, DataType.Decimal);
			AddScalarType(typeof(SqlDouble),   SqlDouble.  Null, true, DataType.Double);
			AddScalarType(typeof(SqlGuid),     SqlGuid.    Null, true, DataType.Guid);
			AddScalarType(typeof(SqlInt16),    SqlInt16.   Null, true, DataType.Int16);
			AddScalarType(typeof(SqlInt32),    SqlInt32.   Null, true, DataType.Int32);
			AddScalarType(typeof(SqlInt64),    SqlInt64.   Null, true, DataType.Int64);
			AddScalarType(typeof(SqlMoney),    SqlMoney.   Null, true, DataType.Money);
			AddScalarType(typeof(SqlSingle),   SqlSingle.  Null, true, DataType.Single);
			AddScalarType(typeof(SqlString),   SqlString.  Null, true, DataType.NVarChar);
			AddScalarType(typeof(SqlXml),      SqlXml.     Null, true, DataType.Xml);

			try
			{
				foreach (var typeName in new[] { "SqlHierarchyId", "SqlGeography", "SqlGeometry" })
				{
					var type = Type.GetType("Microsoft.SqlServer.Types.{0}, Microsoft.SqlServer.Types".Args(typeName));

					if (type == null)
						continue;

					var p = type.GetProperty("Null");
					var l = Expression.Lambda<Func<object>>(
						Expression.Convert(Expression.Property(null, p), typeof(object)));

					var nullValue = l.Compile()();

					AddScalarType(type, nullValue, true, DataType.Udt);

					SqlServerDataProvider.SetUdtType(type, typeName.Substring(3).ToLower());
				}
			}
			catch
			{
			}

			SetValueToSqlConverter(typeof(String),         (sb,v) => ConvertStringToSql1       (sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),           (sb,v) => ConvertCharToSql1         (sb, (char)v));
			SetValueToSqlConverter(typeof(DateTime),       (sb,v) => ConvertDateTimeToSql      (sb, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,v) => ConvertDateTimeOffsetToSql(sb, (DateTimeOffset)v));
			SetValueToSqlConverter(typeof(byte[]),         (sb,v) => ConvertBinaryToSql        (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),         (sb,v) => ConvertBinaryToSql        (sb, ((Binary)v).ToArray()));

			SetValueToSqlConverter(typeof(String), DataType.Char,    (sb,v) => ConvertStringToSql2(sb, v.ToString()));
			SetValueToSqlConverter(typeof(String), DataType.VarChar, (sb,v) => ConvertStringToSql2(sb, v.ToString()));
			SetValueToSqlConverter(typeof(String), DataType.Text,    (sb,v) => ConvertStringToSql2(sb, v.ToString()));

			SetValueToSqlConverter(typeof(Char),   DataType.Char,    (sb,v) => ConvertCharToSql2(sb, (char)v));
			SetValueToSqlConverter(typeof(Char),   DataType.VarChar, (sb,v) => ConvertCharToSql2(sb, (char)v));
			SetValueToSqlConverter(typeof(Char),   DataType.Text,    (sb,v) => ConvertCharToSql2(sb, (char)v));
		}

		internal static SqlServerMappingSchema Instance = new SqlServerMappingSchema();

		public override LambdaExpression TryGetConvertExpression(Type @from, Type to)
		{
			if (@from           != to          &&
				@from.FullName  == to.FullName &&
				@from.Namespace == "Microsoft.SqlServer.Types")
			{
				var p = Expression.Parameter(@from);

				return Expression.Lambda(
					Expression.Call(to, "Parse", new Type[0],
						Expression.New(
							MemberHelper.ConstructorOf(() => new SqlString("")),
							Expression.Call(
								Expression.Convert(p, typeof(object)),
								"ToString",
								new Type[0]))),
					p);
			}

			return base.TryGetConvertExpression(@from, to);
		}

		static void ConvertStringToSql1(StringBuilder stringBuilder, string value)
		{
			stringBuilder
				.Append("N\'")
				.Append(value.Replace("'", "''"))
				.Append('\'');
		}

		static void ConvertStringToSql2(StringBuilder stringBuilder, string value)
		{
			stringBuilder
				.Append('\'')
				.Append(value.Replace("'", "''"))
				.Append('\'');
		}

		static void ConvertCharToSql1(StringBuilder stringBuilder, char value)
		{
			stringBuilder.Append("N\'");

			if (value == '\'') stringBuilder.Append("''");
			else               stringBuilder.Append(value);

			stringBuilder.Append('\'');
		}

		static void ConvertCharToSql2(StringBuilder stringBuilder, char value)
		{
			stringBuilder.Append('\'');

			if (value == '\'') stringBuilder.Append("''");
			else               stringBuilder.Append(value);

			stringBuilder.Append('\'');
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, DateTime value)
		{
			var format = "'{0:yyyy-MM-ddTHH:mm:ss.fff}'";

			if (value.Millisecond == 0)
			{
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
					"'{0:yyyy-MM-dd}'" :
					"'{0:yyyy-MM-ddTHH:mm:ss}'";
			}

			stringBuilder.AppendFormat(format, value);
		}

		static void ConvertDateTimeOffsetToSql(StringBuilder stringBuilder, DateTimeOffset value)
		{
			stringBuilder.AppendFormat("'{0:yyyy-MM-dd HH:mm:ss.ffffff zzz}'", value);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");

			foreach (var b in value)
				stringBuilder.AppendFormat(b.ToString("X2"));
		}
	}

	public class SqlServer2000MappingSchema : MappingSchema
	{
		public SqlServer2000MappingSchema()
			: base(ProviderName.SqlServer2000, SqlServerMappingSchema.Instance)
		{
		}

		public override LambdaExpression TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}

	public class SqlServer2005MappingSchema : MappingSchema
	{
		public SqlServer2005MappingSchema()
			: base(ProviderName.SqlServer2005, SqlServerMappingSchema.Instance)
		{
		}

		public override LambdaExpression TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}

	public class SqlServer2008MappingSchema : MappingSchema
	{
		public SqlServer2008MappingSchema()
			: base(ProviderName.SqlServer2008, SqlServerMappingSchema.Instance)
		{
		}

		public override LambdaExpression TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}

	public class SqlServer2012MappingSchema : MappingSchema
	{
		public SqlServer2012MappingSchema()
			: base(ProviderName.SqlServer2012, SqlServerMappingSchema.Instance)
		{
		}

		public override LambdaExpression TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}
}
