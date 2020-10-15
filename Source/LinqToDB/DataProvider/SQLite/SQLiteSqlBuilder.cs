using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SQLite
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;
	using Common;
	using Tools;

	public class SQLiteSqlBuilder : BasicSqlBuilder
	{
		public SQLiteSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity && trun.Table!.IdentityFields.Count > 0 ? 2 : 1;
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				StringBuilder
					.Append("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='")
					.Append(trun.Table!.PhysicalName)
					.AppendLine("'")
					;
			}
			else
			{
				StringBuilder.AppendLine("SELECT last_insert_rowid()");
			}
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SQLiteSqlBuilder(MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0}";
		}

		public override bool IsNestedJoinSupported => false;

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append('@').Append(value);

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (value.Length > 0 && value[0] == '[')
						return sb.Append(value);

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.NameToDatabase:
				case ConvertType.NameToSchema:
				case ConvertType.NameToQueryTable:
					if (value.Length > 0 && value[0] == '[')
						return sb.Append(value);

					if (value.IndexOf('.') > 0)
						value = string.Join("].[", value.Split('.'));

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == '@'
						? sb.Append(value.Substring(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Int32 : StringBuilder.Append("INTEGER");                      break;
				default             : base.BuildDataTypeFromDataType(type, forCreateTable); break;
			}
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("PRIMARY KEY AUTOINCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			if (createTable.Table!.IdentityFields.Count > 0)
			{
				while (StringBuilder[StringBuilder.Length - 1] != ',')
					StringBuilder.Length--;
				StringBuilder.Length--;
			}
			else
			{
				AppendIndent();
				StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
				StringBuilder.Append(")");
			}
		}

		protected override void BuildPredicate(ISqlPredicate predicate)
		{
			if (predicate is SqlPredicate.ExprExpr exprExpr)
			{
				var leftType  = QueryHelper.GetDbDataType(exprExpr.Expr1);
				var rightType = QueryHelper.GetDbDataType(exprExpr.Expr2);

				if ((IsDateTime(leftType) || IsDateTime(rightType)) &&
					!(exprExpr.Expr1 is IValueContainer container1 && container1.Value == null ||
					  exprExpr.Expr2 is IValueContainer container2 && container2.Value == null))
				{
					if (!(exprExpr.Expr1 is SqlFunction func1 && (func1.Name == "$Convert$" || func1.Name == "DateTime")))
					{
						var l = new SqlFunction(leftType.SystemType, "$Convert$", SqlDataType.GetDataType(leftType.SystemType),
							new SqlDataType(leftType), exprExpr.Expr1);
						exprExpr.Expr1 = l;
					}

					if (!(exprExpr.Expr2 is SqlFunction func2 && (func2.Name == "$Convert$" || func2.Name == "DateTime")))
					{
						var r = new SqlFunction(rightType.SystemType, "$Convert$", new SqlDataType(rightType),
							new SqlDataType(rightType), exprExpr.Expr2);
						exprExpr.Expr2 = r;
					}
				}
			}

			base.BuildPredicate(predicate);
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			if (database != null && database.Length == 0) database = null;

			if (database != null)
				sb.Append(database).Append(".");

			return sb.Append(table);
		}

		static bool IsDateTime(DbDataType dbDataType)
		{
			if (dbDataType.DataType.In(DataType.Date, DataType.Time, DataType.DateTime, DataType.DateTime2,
				DataType.DateTimeOffset, DataType.SmallDateTime, DataType.Timestamp))
				return true;

			if (dbDataType.DataType != DataType.Undefined)
				return false;

			return IsDateTime(dbDataType.SystemType);
		}

		static bool IsDateTime(Type type)
		{
			return
				type == typeof(DateTime) ||
				type == typeof(DateTimeOffset) ||
				type == typeof(DateTime?) ||
				type == typeof(DateTimeOffset?);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			StringBuilder.Append(table.TableOptions.HasIsTemporary()
				? "CREATE TEMPORARY TABLE "
				: "CREATE TABLE ");

			if (table.TableOptions.HasCreateIfNotExists())
				StringBuilder.Append("IF NOT EXISTS ");
		}
	}
}
