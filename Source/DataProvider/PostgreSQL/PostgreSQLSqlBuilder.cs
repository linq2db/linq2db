using System;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using SqlQuery;
	using SqlProvider;

	class PostgreSQLSqlBuilder : BasicSqlBuilder
	{
		public PostgreSQLSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			var into = SelectQuery.Insert.Into;
			var attr = GetSequenceNameAttribute(into, false);
			var name =
				attr != null ?
					attr.SequenceName :
					Convert(
						string.Format("{0}_{1}_seq", into.PhysicalName, into.GetIdentityField().PhysicalName),
						ConvertType.NameToQueryField);

			AppendIndent(sb)
				.Append("SELECT currval('")
				.Append(name)
				.AppendLine("')");
		}

		protected override ISqlBuilder CreateSqlProvider()
		{
			return new PostgreSQLSqlBuilder(SqlOptimizer, SqlProviderFlags);
		}

		protected override string LimitFormat  { get { return "LIMIT {0}";   } }
		protected override string OffsetFormat { get { return "OFFSET {0} "; } }

		protected override void BuildValue(StringBuilder sb, object value)
		{
			if (value is bool)
				sb.Append(value);
			else
				base.BuildValue(sb, value);
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.SByte         :
				case DataType.Byte          : sb.Append("SmallInt");        break;
				case DataType.Money         : sb.Append("Decimal(19,4)");   break;
				case DataType.SmallMoney    : sb.Append("Decimal(10,4)");   break;
#if !MONO
				case DataType.DateTime2     :
#endif
				case DataType.SmallDateTime :
				case DataType.DateTime      : sb.Append("TimeStamp");       break;
				case DataType.Boolean       : sb.Append("Boolean");         break;
				case DataType.NVarChar      :
					sb.Append("VarChar");
					if (type.Length > 0)
						sb.Append('(').Append(type.Length).Append(')');
					break;
				case DataType.Undefined      :
					if (type.Type == typeof(string))
						goto case DataType.NVarChar;
					break;
				default                      : base.BuildDataType(sb, type); break;
			}
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		public static PostgreSQLIdentifierQuoteMode IdentifierQuoteMode = PostgreSQLIdentifierQuoteMode.Auto;

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToQueryTableAlias:
					if (value != null && IdentifierQuoteMode != PostgreSQLIdentifierQuoteMode.None)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '"')
							return name;

						if (IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Quote || name.Any(c => char.IsUpper(c) || char.IsWhiteSpace(c)))
							return '"' + name + '"';
					}

					break;

				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return ":" + value;

				case ConvertType.SprocParameterToName:
					if (value != null)
					{
						var str = value.ToString();
						return (str.Length > 0 && str[0] == ':')? str.Substring(1): str;
					}

					break;
			}

			return value;
		}

		public override ISqlExpression GetIdentityExpression(SqlTable table, SqlField identityField, bool forReturning)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
			{
				var attr = GetSequenceNameAttribute(table, false);
	
				if (attr != null)
					return new SqlExpression("nextval('" + attr.SequenceName+"')", Precedence.Primary);
			}

			return base.GetIdentityExpression(table, identityField, forReturning);
		}

		protected override void BuildCreateTableFieldType(StringBuilder sb, SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.DataType == DataType.Int32)
				{
					sb.Append("SERIAL");
					return;
				}

				if (field.DataType == DataType.Int64)
				{
					sb.Append("BIGSERIAL");
					return;
				}
			}

			base.BuildCreateTableFieldType(sb, field);
		}
	}
}
