using System;
using System.Text;

namespace LinqToDB.DataProvider.Firebird
{
	using LinqToDB.Common;
	using LinqToDB.Data;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;
	using System.Linq.Expressions;

	public class FirebirdMappingSchema : MappingSchema
	{
		public FirebirdMappingSchema() : this(ProviderName.Firebird, null)
		{
		}

		public FirebirdMappingSchema(params MappingSchema[] mappingSchemas) : this(ProviderName.Firebird, mappingSchemas)
		{
		}

		protected FirebirdMappingSchema(string configuration, MappingSchema[]? mappingSchemas) : base(configuration, mappingSchemas)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			// firebird string literals can contain only limited set of characters, so we should encode them
			SetValueToSqlConverter(typeof(string)  , (sb, dt, v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char)    , (sb, dt, v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[])  , (sb, dt, v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary)  , (sb, dt, v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(DateTime), (sb, dt, v) => BuildDateTime(sb, dt, (DateTime)v));
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			var dbType = dt.Type.DbType ?? "timestamp";
			var format = "CAST('{0:yyyy-MM-dd HH:mm:ss.fff}' AS {1})";

			if (value.Millisecond == 0)
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0
					? "CAST('{0:yyyy-MM-dd}' AS {1})"
					: "CAST('{0:yyyy-MM-dd HH:mm:ss}' AS {1})";

			stringBuilder.AppendFormat(format, value, dbType);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("X'");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));

			stringBuilder.Append("'");
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			if (value == string.Empty)
				stringBuilder.Append("''");
			else
				if (FirebirdConfiguration.IsLiteralEncodingSupported && NeedsEncoding(value))
					MakeUtf8Literal(stringBuilder, Encoding.UTF8.GetBytes(value));
				else
				{
					stringBuilder
						.Append("'")
						.Append(value.Replace("'", "''"))
						.Append("'");
				}
		}

		static bool NeedsEncoding(string str)
		{
			foreach (char t in str)
				if (NeedsEncoding(t))
					return true;

			return false;
		}

		static bool NeedsEncoding(char c)
		{
			return c == '\x00' || c >= '\x80';
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			if (FirebirdConfiguration.IsLiteralEncodingSupported && NeedsEncoding(value))
				MakeUtf8Literal(stringBuilder, Encoding.UTF8.GetBytes(new[] {value}));
			else
			{
				stringBuilder
					.Append("'")
					.Append(value == '\'' ? '\'' : value)
					.Append("'");
			}
		}

		private static void MakeUtf8Literal(StringBuilder stringBuilder, byte[] bytes)
		{
			stringBuilder.Append("_utf8 x'");

			foreach (var bt in bytes)
			{
				stringBuilder.AppendFormat("{0:X2}", bt);
			}

			stringBuilder.Append("'");
		}

		internal static MappingSchema Instance { get; } = new FirebirdMappingSchema();
	}

	public class Firebird25MappingSchema : MappingSchema
	{
		public Firebird25MappingSchema()
			: this(ProviderName.Firebird25)
		{
		}

		public Firebird25MappingSchema(params MappingSchema[] schemas)
				: this(ProviderName.Firebird25, schemas)
		{
		}

		protected Firebird25MappingSchema(string providerName, params MappingSchema[] schemas)
				: base(providerName, Array<MappingSchema>.Append(schemas, FirebirdMappingSchema.Instance))
		{
			//SetValueToSqlConverter(typeof(bool), (sb, dt, v) => sb.Append((bool)v ? "'1'" : "'0'"));
			//SetConvertExpression<bool, DataParameter>(v => new DataParameter(null, v ? '1' : '0', DataType.Char));
			//SetConvertExpression<bool?, DataParameter>(v => new DataParameter(null, v == null ? null : (v.Value ? '1' : '0'), DataType.Char));
		}
	}

	public class Firebird25Dialect1MappingSchema : Firebird25MappingSchema
	{
		public Firebird25Dialect1MappingSchema()
			: this(ProviderName.Firebird25Dialect1)
		{
		}

		public Firebird25Dialect1MappingSchema(params MappingSchema[] schemas)
				: this(ProviderName.Firebird25Dialect1, schemas)
		{
		}

		protected Firebird25Dialect1MappingSchema(string providerName, params MappingSchema[] schemas)
				: base(providerName, schemas)
		{
		}
	}

	public class Firebird3MappingSchema : Firebird25MappingSchema
	{
		public Firebird3MappingSchema()
			: this(ProviderName.Firebird3)
		{
		}

		public Firebird3MappingSchema(params MappingSchema[] schemas)
				: this(ProviderName.Firebird3, schemas)
		{
		}

		protected Firebird3MappingSchema(string providerName, params MappingSchema[] schemas)
				: base(providerName, schemas)
		{
			// restore boolean mapping
			SetValueToSqlConverter(typeof(bool), (sb, dt, v) => sb.Append((bool)v ? "true" : "false"));
			SetConvertExpression<bool, DataParameter>(v => new DataParameter(null, v, DataType.Boolean));
			SetConvertExpression<bool?, DataParameter>(v => new DataParameter(null, v, DataType.Boolean));

			// remap char-based mapping
			// TODO
			//SetValueToSqlConverter(typeof(bool), (sb, dt, v) => sb.Append((bool)v ? "'1'" : "'0'"));
			Expression<Func<bool, DataParameter>> converter = v => new DataParameter(null, v ? '1' : '0', DataType.Char);
			Expression<Func<bool?, DataParameter>> converterN = v => new DataParameter(null, v == null ? null : (v.Value ? '1' : '0'), DataType.Char);
			SetConvertExpression(new DbDataType(typeof(bool), DataType.Char), new DbDataType(typeof(DataParameter)), converter);
			SetConvertExpression(new DbDataType(typeof(bool?), DataType.Char), new DbDataType(typeof(DataParameter)), converterN);
		}
	}

	public class Firebird3Dialect1MappingSchema : Firebird3MappingSchema
	{
		public Firebird3Dialect1MappingSchema()
			: this(ProviderName.Firebird3Dialect1)
		{
		}

		public Firebird3Dialect1MappingSchema(params MappingSchema[] schemas)
				: this(ProviderName.Firebird3Dialect1, schemas)
		{
		}

		protected Firebird3Dialect1MappingSchema(string providerName, params MappingSchema[] schemas)
				: base(providerName, schemas)
		{
		}
	}

	public class Firebird4MappingSchema : Firebird3MappingSchema
	{
		public Firebird4MappingSchema()
			: this(ProviderName.Firebird4)
		{
		}

		public Firebird4MappingSchema(params MappingSchema[] schemas)
				: this(ProviderName.Firebird4, schemas)
		{
		}

		protected Firebird4MappingSchema(string providerName, params MappingSchema[] schemas)
				: base(providerName, schemas)
		{
		}
	}

	public class Firebird4Dialect1MappingSchema : Firebird4MappingSchema
	{
		public Firebird4Dialect1MappingSchema()
			: this(ProviderName.Firebird4Dialect1)
		{
		}

		public Firebird4Dialect1MappingSchema(params MappingSchema[] schemas)
				: this(ProviderName.Firebird4Dialect1, schemas)
		{
		}

		protected Firebird4Dialect1MappingSchema(string providerName, params MappingSchema[] schemas)
				: base(providerName, schemas)
		{
		}
	}
}
