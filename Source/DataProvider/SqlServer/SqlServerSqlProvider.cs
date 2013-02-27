using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Extensions;
	using SqlBuilder;
	using SqlProvider;

	public abstract class SqlServerSqlProvider : BasicSqlProvider
	{
		protected SqlServerSqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
			SqlProviderFlags.IsApplyJoinSupported = true;
		}

		protected override string FirstFormat
		{
			get { return SqlQuery.Select.SkipValue == null ? "TOP ({0})" : null; }
		}

		protected override void BuildSql(StringBuilder sb)
		{
			AlternativeBuildSql(sb, true, base.BuildSql);
		}

		protected override void BuildGetIdentity(StringBuilder sb)
		{
			sb
				.AppendLine()
				.AppendLine("SELECT SCOPE_IDENTITY()");
		}

		protected override void BuildOrderByClause(StringBuilder sb)
		{
			if (!NeedSkip)
				base.BuildOrderByClause(sb);
		}

		protected override IEnumerable<SqlQuery.Column> GetSelectedColumns()
		{
			if (NeedSkip && !SqlQuery.OrderBy.IsEmpty)
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

		protected override void BuildDeleteClause(StringBuilder sb)
		{
			AppendIndent(sb)
				.Append("DELETE ")
				.Append(Convert(GetTableAlias(SqlQuery.From.Tables[0]), ConvertType.NameToQueryTableAlias))
				.AppendLine();
		}

		protected override void BuildUpdateTableName(StringBuilder sb)
		{
			if (SqlQuery.Update.Table != null && SqlQuery.Update.Table != SqlQuery.From.Tables[0].Source)
				BuildPhysicalTable(sb, SqlQuery.Update.Table, null);
			else
				sb.Append(Convert(GetTableAlias(SqlQuery.From.Tables[0]), ConvertType.NameToQueryTableAlias));
		}

		protected override void BuildUnicodeString(StringBuilder sb, string value)
		{
			sb
				.Append("N\'")
				.Append(value.Replace("'", "''"))
				.Append('\'');
		}

		protected override void BuildColumn(StringBuilder sb, SqlQuery.Column col, ref bool addAlias)
		{
			var wrap = false;

			if (col.SystemType == typeof(bool))
			{
				if (col.Expression is SqlQuery.SearchCondition)
					wrap = true;
				else
				{
					var ex = col.Expression as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlQuery.SearchCondition;
				}
			}

			if (wrap) sb.Append("CASE WHEN ");
			base.BuildColumn(sb, col, ref addAlias);
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
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("].[", name.Split('.'));
					}

					return "[" + value + "]";

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
	}
}
