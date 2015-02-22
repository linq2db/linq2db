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
	using SqlQuery;

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

			SetValueToSqlConverter(typeof(String),         (sb,dt,v) => ConvertStringToSql        (sb, dt, v.ToString()));
			SetValueToSqlConverter(typeof(Char),           (sb,dt,v) => ConvertCharToSql          (sb, dt, (char)v));
			SetValueToSqlConverter(typeof(DateTime),       (sb,dt,v) => ConvertDateTimeToSql      (sb, (DateTime)v));
			SetValueToSqlConverter(typeof(TimeSpan),       (sb,dt,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v));
			SetValueToSqlConverter(typeof(byte[]),         (sb,dt,v) => ConvertBinaryToSql        (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),         (sb,dt,v) => ConvertBinaryToSql        (sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), int.MaxValue));
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

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("char(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, string value)
		{
			string start;

			switch (sqlDataType.DataType)
			{
				case DataType.Char    :
				case DataType.VarChar :
				case DataType.Text    :
					start = "'";
					break;
				default               :
					start = "N'";
					break;
			}

			DataTools.ConvertStringToSql(stringBuilder, "+", start, AppendConversion, value);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, char value)
		{
			string start;

			switch (sqlDataType.DataType)
			{
				case DataType.Char    :
				case DataType.VarChar :
				case DataType.Text    :
					start = "'";
					break;
				default               :
					start = "N'";
					break;
			}

			DataTools.ConvertCharToSql(stringBuilder, start, AppendConversion, value);
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, DateTime value)
		{
			var format =
				value.Millisecond == 0
					? value.Hour == 0 && value.Minute == 0 && value.Second == 0
						? "yyyy-MM-dd"
						: "yyyy-MM-ddTHH:mm:ss"
					: "yyyy-MM-ddTHH:mm:ss.fff";

			stringBuilder
				.Append('\'')
				.Append(value.ToString(format))
				.Append('\'')
				;
		}

		static void ConvertTimeSpanToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, TimeSpan value)
		{
			if (sqlDataType.DataType == DataType.Int64)
			{
				stringBuilder.Append(value.Ticks);
			}
			else
			{
				var format = value.Days > 0
					? value.Milliseconds > 0
						? "d\\.hh\\:mm\\:ss\\.fff"
						: "d\\.hh\\:mm\\:ss"
					: value.Milliseconds > 0
						? "hh\\:mm\\:ss\\.fff"
						: "hh\\:mm\\:ss";

				stringBuilder
					.Append('\'')
					.Append(value.ToString(format))
					.Append('\'')
					;
			}
		}

		static void ConvertDateTimeOffsetToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, DateTimeOffset value)
		{
			var format = "'{0:yyyy-MM-dd HH:mm:ss.fffffff zzz}'";

			switch (sqlDataType.Precision ?? sqlDataType.Scale)
			{
				case 0 : format = "'{0:yyyy-MM-dd HH:mm:ss zzz}'"; break;
				case 1 : format = "'{0:yyyy-MM-dd HH:mm:ss.f zzz}'"; break;
				case 2 : format = "'{0:yyyy-MM-dd HH:mm:ss.ff zzz}'"; break;
				case 3 : format = "'{0:yyyy-MM-dd HH:mm:ss.fff zzz}'"; break;
				case 4 : format = "'{0:yyyy-MM-dd HH:mm:ss.ffff zzz}'"; break;
				case 5 : format = "'{0:yyyy-MM-dd HH:mm:ss.fffff zzz}'"; break;
				case 6 : format = "'{0:yyyy-MM-dd HH:mm:ss.ffffff zzz}'"; break;
				case 7 : format = "'{0:yyyy-MM-dd HH:mm:ss.fffffff zzz}'"; break;
			}

			stringBuilder.AppendFormat(format, value);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));
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
