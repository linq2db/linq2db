using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.SQLite
{
	using SqlQuery;
	using SqlProvider;

	public class SQLiteSqlBuilder : BasicSqlBuilder
	{
		public SQLiteSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber)
		{
			StringBuilder.AppendLine("SELECT last_insert_rowid()");
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SQLiteSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override string LimitFormat  { get { return "LIMIT {0}";  } }
		protected override string OffsetFormat { get { return "OFFSET {0}"; } }

		public override bool IsNestedJoinSupported { get { return false; } }

		protected override void BuildFromClause()
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause();
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return "@" + value;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;
					}

					return "[" + value + "]";

				case ConvertType.NameToDatabase:
				case ConvertType.NameToOwner:
				case ConvertType.NameToQueryTable:
					if (value != null)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("].[", name.Split('.'));

						return "[" + value + "]";
					}

					break;

				case ConvertType.SprocParameterToName:
					{
						var name = (string)value;
						return name.Length > 0 && name[0] == '@'? name.Substring(1): name;
					}
			}

			return value;
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.Int32 : StringBuilder.Append("INTEGER"); break;
				default             : base.BuildDataType(type);        break;
			}
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("PRIMARY KEY AUTOINCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(string pkName, IEnumerable<string> fieldNames)
		{
			if (SelectQuery.CreateTable.Table.Fields.Values.Any(f => f.IsIdentity))
			{
				while (StringBuilder[StringBuilder.Length - 1] != ',')
					StringBuilder.Length--;
				StringBuilder.Length--;
			}
			else
			{
			AppendIndent();
				StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
				StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
				StringBuilder.Append(")");
			}
		}

		protected override void BuildPredicate(ISqlPredicate predicate)
		{
			var exprExpr = predicate as SelectQuery.Predicate.ExprExpr;

			if (exprExpr != null)
			{
				var leftType  = exprExpr.Expr1.SystemType;
				var rightType = exprExpr.Expr2.SystemType;

				if (IsDateTime(leftType) || IsDateTime(rightType))
				{

					var l = new SqlFunction(leftType, "$Convert$", SqlDataType.GetDataType(leftType),
						SqlDataType.GetDataType(leftType), exprExpr.Expr1);

					var r = new SqlFunction(rightType, "$Convert$", SqlDataType.GetDataType(rightType),
						SqlDataType.GetDataType(rightType), exprExpr.Expr2);

					exprExpr.Expr1 = l;
					exprExpr.Expr2 = r;
				}
			}

			base.BuildPredicate(predicate);
		}
		
		public override StringBuilder BuildTableName(StringBuilder sb, string database, string owner, string table)
		{
		    if (database != null)
			sb.Append(database).Append(".");

		    return sb.Append(table);
		}

		private static bool IsDateTime(Type type)
		{
			return    type == typeof(DateTime)
				   || type == typeof(DateTimeOffset)
				   || type == typeof(DateTime?)
				   || type == typeof(DateTimeOffset?);
		}
	}
}
