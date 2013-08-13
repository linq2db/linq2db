using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Extensions;
	using SqlQuery;
	using SqlProvider;

	public abstract class SqlServerSqlProvider : BasicSqlProvider
	{
		protected SqlServerSqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected virtual  bool BuildAlternativeSql { get { return true; } }

		protected override string FirstFormat
		{
			get { return SelectQuery.Select.SkipValue == null ? "TOP ({0})" : null; }
		}

		protected override void BuildSql(StringBuilder sb)
		{
			if (BuildAlternativeSql)
				AlternativeBuildSql(sb, true, base.BuildSql);
			else
				base.BuildSql(sb);
		}

		protected override void BuildGetIdentity(StringBuilder sb)
		{
			sb
				.AppendLine()
				.AppendLine("SELECT SCOPE_IDENTITY()");
		}

		protected override void BuildOrderByClause(StringBuilder sb)
		{
			if (!BuildAlternativeSql || !NeedSkip)
				base.BuildOrderByClause(sb);
		}

		protected override IEnumerable<SelectQuery.Column> GetSelectedColumns()
		{
			if (BuildAlternativeSql && NeedSkip && !SelectQuery.OrderBy.IsEmpty)
				return AlternativeGetSelectedColumns(base.GetSelectedColumns);
			return base.GetSelectedColumns();
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			switch (expr.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:
					{
						var be = (SqlBinaryExpression)expr;

						switch (be.Operation)
						{
							case "%":
								{
									var type1 = be.Expr1.SystemType.ToUnderlying();

									if (type1 == typeof(double) || type1 == typeof(float))
									{
										return new SqlBinaryExpression(
											be.Expr2.SystemType,
											new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, be.Expr1),
											be.Operation,
											be.Expr2);
									}

									break;
								}
						}

						break;
					}

				case QueryElementType.SqlFunction:
					{
						var func = (SqlFunction)expr;

						switch (func.Name)
						{
							case "Convert" :
								{
									if (func.SystemType.ToUnderlying() == typeof(ulong) &&
										func.Parameters[1].SystemType.IsFloatType())
										return new SqlFunction(
											func.SystemType,
											func.Name,
											func.Precedence,
											func.Parameters[0],
											new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

									break;
								}
						}

						break;
					}
			}

			return expr;
		}

		public ISqlExpression ConvertConvertFunction(SqlFunction func)
		{
			switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
			{
				case TypeCode.DateTime :

					if (func.Name == "Convert")
					{
						var type1 = func.Parameters[1].SystemType.ToUnderlying();

						if (IsTimeDataType(func.Parameters[0]))
						{
							if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
								return new SqlExpression(
									func.SystemType, "Cast(Convert(Char, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

							if (func.Parameters[1].SystemType == typeof(string))
								return func.Parameters[1];

							return new SqlExpression(
								func.SystemType, "Convert(Char, {0}, 114)", Precedence.Primary, func.Parameters[1]);
						}

						if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
						{
							if (IsDateDataType(func.Parameters[0], "Datetime"))
								return new SqlExpression(
									func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
						}

						if (func.Parameters.Length == 2 && func.Parameters[0] is SqlDataType && func.Parameters[0] == SqlDataType.DateTime)
							return new SqlFunction(func.SystemType, func.Name, func.Precedence, func.Parameters[0], func.Parameters[1], new SqlValue(120));
					}

					break;
			}

			return func;
		}

		protected override void BuildDeleteClause(StringBuilder sb)
		{
			var table = SelectQuery.Delete.Table != null ?
				(SelectQuery.From.FindTableSource(SelectQuery.Delete.Table) ?? SelectQuery.Delete.Table) :
				SelectQuery.From.Tables[0];

			AppendIndent(sb)
				.Append("DELETE ")
				.Append(Convert(GetTableAlias(table), ConvertType.NameToQueryTableAlias))
				.AppendLine();
		}

		protected override void BuildUpdateTableName(StringBuilder sb)
		{
			var table = SelectQuery.Update.Table != null ?
				(SelectQuery.From.FindTableSource(SelectQuery.Update.Table) ?? SelectQuery.Update.Table) :
				SelectQuery.From.Tables[0];

			if (table is SqlTable)
				BuildPhysicalTable(sb, table, null);
			else
				sb.Append(Convert(GetTableAlias(table), ConvertType.NameToQueryTableAlias));
		}

		protected override void BuildString(StringBuilder sb, string value)
		{
			foreach (var ch in value)
			{
				if (ch > 127)
				{
					sb.Append("N");
					break;
				}
			}

			base.BuildString(sb, value);
		}

		protected override void BuildChar(StringBuilder sb, char value)
		{
			if (value > 127)
				sb.Append("N");

			base.BuildChar(sb, value);
		}

		protected override void BuildColumnExpression(StringBuilder sb, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SelectQuery.SearchCondition)
					wrap = true;
				else
				{
					var ex = expr as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SelectQuery.SearchCondition;
				}
			}

			if (wrap) sb.Append("CASE WHEN ");
			base.BuildColumnExpression(sb, expr, alias, ref addAlias);
			if (wrap) sb.Append(" THEN 1 ELSE 0 END");
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
					if (value != null)
					{
						var str = value.ToString();
						return str.Length > 0 && str[0] == '@'? str.Substring(1): str;
					}
					break;
			}

			return value;
		}

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsUpdateInsert(sb);
		}

		protected override void BuildDateTime(StringBuilder sb, object value)
		{
			sb.Append("'{0:yyyy-MM-ddTHH:mm:ss.fff}'".Args(value));
		}

		protected override void BuildCreateTableIdentityAttribute2(StringBuilder sb, SqlField field)
		{
			sb.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(StringBuilder sb, string pkName, IEnumerable<string> fieldNames)
		{
			sb.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			sb.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			sb.Append(")");
		}
	}
}
