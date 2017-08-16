using System;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using SqlQuery;
	using SqlProvider;

	public class PostgreSQLSqlBuilder : BasicSqlBuilder
	{
		public PostgreSQLSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber)
		{
			var into = SelectQuery.Insert.Into;
			var attr = GetSequenceNameAttribute(into, false);
			var name =
				attr != null ?
					attr.SequenceName :
					Convert(
						string.Format("{0}_{1}_seq", into.PhysicalName, into.GetIdentityField().PhysicalName),
						ConvertType.NameToQueryField);

			name = Convert(name, ConvertType.NameToQueryTable);

			var database = GetTableDatabaseName(into);
			var owner    = GetTableOwnerName   (into);

			AppendIndent()
				.Append("SELECT currval('");

			BuildTableName(StringBuilder, database, owner, name.ToString());

			StringBuilder.AppendLine("')");
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override string LimitFormat  { get { return "LIMIT {0}";   } }
		protected override string OffsetFormat { get { return "OFFSET {0} "; } }

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.SByte         :
				case DataType.Byte          : StringBuilder.Append("SmallInt");       break;
				case DataType.Money         : StringBuilder.Append("Decimal(19,4)");  break;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10,4)");  break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");      break;
				case DataType.DateTimeOffset: StringBuilder.Append("TimeStampTZ");    break;
				case DataType.Boolean       : StringBuilder.Append("Boolean");        break;
				case DataType.NVarChar      :
					StringBuilder.Append("VarChar");
					if (type.Length > 0)
						StringBuilder.Append('(').Append(type.Length).Append(')');
					break;
				case DataType.Undefined      :
					if (type.Type == typeof(string))
						goto case DataType.NVarChar;
					break;
				case DataType.Json           : StringBuilder.Append("json");           break;
				case DataType.BinaryJson     : StringBuilder.Append("jsonb");          break;
				case DataType.Guid	     : StringBuilder.Append("uuid");           break;
				default                      : base.BuildDataType(type, createDbType); break;
			}
		}

		protected override void BuildFromClause()
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause();
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.PostgreSQL);
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
				case ConvertType.NameToDatabase:
				case ConvertType.NameToOwner:
					if (value != null && IdentifierQuoteMode != PostgreSQLIdentifierQuoteMode.None)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '"')
							return name;

						if (IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Quote)
							return '"' + name + '"';

						if (IsReserved(name))
							return '"' + name + '"';

						if (name
#if NETFX_CORE
								.ToCharArray()
#endif
								.Any(c => char.IsWhiteSpace(c) || (IdentifierQuoteMode == PostgreSQLIdentifierQuoteMode.Auto && char.IsUpper(c))))
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

		public override ISqlExpression GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
			{
				var attr = GetSequenceNameAttribute(table, false);

				if (attr != null)
				{
					var name     = Convert(attr.SequenceName, ConvertType.NameToQueryTable).ToString();
					var database = GetTableDatabaseName(table);
					var owner    = GetTableOwnerName   (table);

					var sb = BuildTableName(new StringBuilder(), database, owner, name);

					return new SqlExpression("nextval('" + sb + "')", Precedence.Primary);
				}
			}

			return base.GetIdentityExpression(table);
		}

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.DataType == DataType.Int32)
				{
					StringBuilder.Append("SERIAL");
					return;
				}

				if (field.DataType == DataType.Int64)
				{
					StringBuilder.Append("BIGSERIAL");
					return;
				}
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override bool BuildJoinType(SelectQuery.JoinedTable join)
		{
			switch (join.JoinType)
			{
				case SelectQuery.JoinType.CrossApply : StringBuilder.Append("INNER JOIN LATERAL "); return true;
				case SelectQuery.JoinType.OuterApply : StringBuilder.Append("LEFT JOIN LATERAL ");  return true;
			}

			return base.BuildJoinType(join);
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string owner, string table)
		{
			// "db..table" syntax not supported and postgresql doesn't support database name, if it is not current database
			// so we can clear database name to avoid error from server
			if (database != null && owner == null)
				database = null;

			return base.BuildTableName(sb, database, owner, table);
		}

#if !SILVERLIGHT

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.NpgsqlDbType.ToString();
		}

#endif
	}
}
