using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.DB2
{
	using SqlQuery;
	using SqlProvider;

	abstract class DB2SqlBuilderBase : BasicSqlBuilder
	{
		protected DB2SqlBuilderBase(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		SqlField _identityField;

		protected abstract DB2Version Version { get; }

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity ? 1 + trun.Table.Fields.Values.Count(f => f.IsIdentity) : 1;

			if (Version == DB2Version.LUW && statement is SqlInsertStatement insertStatement && insertStatement.Insert.WithIdentity)
			{
				_identityField = insertStatement.Insert.Into.GetIdentityField();

				if (_identityField == null)
					return 2;
			}

			return 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table.Fields.Values.Skip(commandNumber - 1).First(f => f.IsIdentity);

				StringBuilder.Append("ALTER TABLE ");
				ConvertTableName(StringBuilder, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName);
				StringBuilder
					.Append(" ALTER ")
					.Append(Convert(field.PhysicalName, ConvertType.NameToQueryField))
					.AppendLine(" RESTART WITH 1")
					;
			}
			else
			{
				StringBuilder.AppendLine("SELECT identity_val_local() FROM SYSIBM.SYSDUMMY1");
			}
		}

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var table = truncateTable.Table;

			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(table, null);
			StringBuilder.Append(" IMMEDIATE");
			StringBuilder.AppendLine();
		}

		protected override void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, int indent, bool skipAlias)
		{
			Statement     = statement;
			StringBuilder = sb;
			Indent        = indent;
			SkipAlias     = skipAlias;

			if (_identityField != null)
			{
				indent += 2;

				AppendIndent().AppendLine("SELECT");
				AppendIndent().Append("\t");
				BuildExpression(_identityField, false, true);
				sb.AppendLine();
				AppendIndent().AppendLine("FROM");
				AppendIndent().AppendLine("\tNEW TABLE");
				AppendIndent().AppendLine("\t(");
			}

			base.BuildSql(commandNumber, statement, sb, indent, skipAlias);

			if (_identityField != null)
				sb.AppendLine("\t)");
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			if (Version == DB2Version.zOS)
			{
				StringBuilder
					.AppendLine(";")
					.AppendLine("SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1");
			}
		}

		protected override void BuildSql()
		{
			AlternativeBuildSql(false, base.BuildSql, "\t0");
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().AppendLine("SELECT");
				BuildColumns(selectQuery);
				AppendIndent().AppendLine("FROM SYSIBM.SYSDUMMY1 FETCH FIRST 1 ROW ONLY");
			}
			else
				base.BuildSelectClause(selectQuery);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue == null ? "FETCH FIRST {0} ROWS ONLY" : null;
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate())
				base.BuildFromClause(statement, selectQuery);
		}

		protected override void BuildColumnExpression(SelectQuery selectQuery, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
					wrap = expr is SqlExpression ex && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlSearchCondition;
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.DateTime  : StringBuilder.Append("timestamp");      break;
				case DataType.DateTime2 : StringBuilder.Append("timestamp");      break;
				default                 : base.BuildDataType(type, createDbType); break;
			}
		}

		public static DB2IdentifierQuoteMode IdentifierQuoteMode = DB2IdentifierQuoteMode.Auto;

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return "@" + value;

				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return ":" + value;

				case ConvertType.SprocParameterToName:
					if (value != null)
					{
						var str = value.ToString();
						return str.Length > 0 && str[0] == ':'? str.Substring(1): str;
					}

					break;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToQueryTableAlias:
					if (value != null && IdentifierQuoteMode != DB2IdentifierQuoteMode.None)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '"')
							return name;

						if (IdentifierQuoteMode == DB2IdentifierQuoteMode.Quote ||
							name.StartsWith("_") ||
							name.Any(c => char.IsLower(c) || char.IsWhiteSpace(c)))
							return '"' + name + '"';
					}

					break;
			}

			return value;
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM SYSIBM.SYSDUMMY1 FETCH FIRST 1 ROW ONLY");
		}

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.Append("VALUES ");

			foreach (var col in insertClause.Into.Fields)
				StringBuilder.Append("(DEFAULT)");

			StringBuilder.AppendLine();
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("GENERATED ALWAYS AS IDENTITY");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string schema, string table)
		{
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			// "db..table" syntax not supported
			if (database != null && schema == null)
				throw new LinqToDBException("DB2 requires schema name if database name provided.");

			return base.BuildTableName(sb, database, schema, table);
		}

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			if (parameter.DbType == DbType.Decimal && parameter.Value is decimal)
			{
				var d = new SqlDecimal((decimal)parameter.Value);
				return "(" + d.Precision + "," + d.Scale + ")";
			}

			dynamic p = parameter;
			return p.DB2Type.ToString();
		}
	}
}
