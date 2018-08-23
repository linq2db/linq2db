using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using SqlQuery;
	using SqlProvider;
	using System.Globalization;

	class InformixSqlBuilder : BasicSqlBuilder
	{
		public InformixSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity ? 1 + trun.Table.Fields.Values.Count(f => f.IsIdentity) : 1;
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table.Fields.Values.Skip(commandNumber - 1).First(f => f.IsIdentity);

				StringBuilder.Append("ALTER TABLE ");
				ConvertTableName(StringBuilder, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName);
				StringBuilder
					.Append(" MODIFY ")
					.Append(Convert(field.PhysicalName, ConvertType.NameToQueryField))
					.AppendLine(" SERIAL(1)")
					;
			}
			else
			{
				StringBuilder.AppendLine("SELECT DBINFO('sqlca.sqlerrd1') FROM systables where tabid = 1");
			}
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			StringBuilder.Append("TRUNCATE TABLE ");
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new InformixSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, int indent, bool skipAlias)
		{
			base.BuildSql(commandNumber, statement, sb, indent, skipAlias);

			sb
				.Replace("NULL IS NOT NULL", "1=0")
				.Replace("NULL IS NULL",     "1=1");
		}

//		protected override bool ParenthesizeJoin(List<SelectQuery.JoinedTable> joins)
//		{
//			return joins.Any(j => j.JoinType == SelectQuery.JoinType.Inner && j.Condition.Conditions.IsNullOrEmpty());
//		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().Append("SELECT FIRST 1").AppendLine();
				BuildColumns(selectQuery);
				AppendIndent().Append("FROM SYSTABLES").AppendLine();
			}
			else if (selectQuery.Select.IsDistinct)
			{
				AppendIndent();
				StringBuilder.Append("SELECT");
				BuildSkipFirst(selectQuery);
				StringBuilder.Append(" DISTINCT");
				StringBuilder.AppendLine();
				BuildColumns(selectQuery);
			}
			else
				base.BuildSelectClause(selectQuery);
		}

		protected override string FirstFormat(SelectQuery selectQuery) => "FIRST {0}";
		protected override string SkipFormat  => "SKIP {0}";

		protected override void BuildLikePredicate(SqlPredicate.Like predicate)
		{
			if (predicate.IsNot)
				StringBuilder.Append("NOT ");

			var precedence = GetPrecedence(predicate);

			BuildExpression(precedence, predicate.Expr1);
			StringBuilder.Append(" LIKE ");
			BuildExpression(precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				StringBuilder.Append(" ESCAPE ");
				BuildExpression(precedence, predicate.Escape);
			}
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.VarBinary  : StringBuilder.Append("BYTE");                      break;
				case DataType.Boolean    : StringBuilder.Append("BOOLEAN");                   break;
				case DataType.DateTime   : StringBuilder.Append("datetime year to second");   break;
				case DataType.DateTime2  : StringBuilder.Append("datetime year to fraction"); break;
				case DataType.Time       :
					StringBuilder.Append("INTERVAL HOUR TO FRACTION");
					StringBuilder.AppendFormat("({0})", (type.Length ?? 5).ToString(CultureInfo.InvariantCulture));
					break;
				case DataType.Date       : StringBuilder.Append("DATETIME YEAR TO DAY");      break;
				case DataType.SByte      :
				case DataType.Byte       : StringBuilder.Append("SmallInt");                  break;
				case DataType.SmallMoney : StringBuilder.Append("Decimal(10,4)");             break;
				case DataType.Decimal    :
					StringBuilder.Append("Decimal");
					if (type.Precision != null && type.Scale != null)
						StringBuilder.AppendFormat(
							"({0}, {1})",
							type.Precision.Value.ToString(CultureInfo.InvariantCulture),
							type.Scale.Value.ToString(CultureInfo.InvariantCulture));
					break;
				default                  : base.BuildDataType(type, createDbType);            break;
			}
		}

		/// <summary>
		/// Check if identifier is valid without quotation. Expects non-zero length string as input.
		/// </summary>
		private bool IsValidIdentifier(string name)
		{
			// https://www.ibm.com/support/knowledgecenter/en/SSGU8G_12.1.0/com.ibm.sqls.doc/ids_sqs_1660.htm
			// TODO: add informix-specific reserved words list
			// TODO: Letter definitions is: In the default locale, must be an ASCII character in the range A to Z or a to z
			// add support for other locales later
			return !IsReserved(name) &&
				((name[0] >= 'a' && name[0] <= 'z') || (name[0] >= 'A' && name[0] <= 'Z') || name[0] == '_') &&
				name.All(c =>
					(c >= 'a' && c <= 'z') ||
					(c >= 'A' && c <= 'Z') ||
					(c >= '0' && c <= '9') ||
					c == '$' ||
					c == '_');
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryTable:
					if (value != null)
					{
						var name = value.ToString();
						if (name.Length > 0 && !IsValidIdentifier(name))
						{
							// I wonder what to do if identifier has " in name?
							return '"' + name + '"';
						}
					}

					break;
				case ConvertType.NameToQueryParameter   : return "?";
				case ConvertType.NameToCommandParameter :
				case ConvertType.NameToSprocParameter   : return ":" + value;
				case ConvertType.SprocParameterToName   :
					if (value != null)
					{
						var str = value.ToString();
						return (str.Length > 0 && str[0] == ':')? str.Substring(1): str;
					}

					break;
			}

			return value;
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
					StringBuilder.Append("SERIAL8");
					return;
				}
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string schema, string table)
		{
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			// TODO: FQN could also contain server name, but we don't have such API for now
			// https://www.ibm.com/support/knowledgecenter/en/SSGU8G_12.1.0/com.ibm.sqls.doc/ids_sqs_1652.htm
			if (database != null)
				sb.Append(database).Append(":");

			if (schema != null)
				sb.Append(schema).Append(".");

			return sb.Append(table);
		}

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.IfxType.ToString();
		}
	}
}
