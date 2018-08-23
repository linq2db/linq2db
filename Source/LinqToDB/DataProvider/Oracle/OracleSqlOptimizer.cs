using System;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class OracleSqlOptimizer : BasicSqlOptimizer
	{
		public OracleSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, 30);

			var selectQuery = statement.SelectQuery;
			if (selectQuery != null)
			{
				new QueryVisitor().Visit(selectQuery.Select, element =>
				{
					if (element.ElementType == QueryElementType.SqlParameter)
					{
						var p = (SqlParameter) element;
						if (p.SystemType == null || p.SystemType.IsScalar(false))
							p.IsQueryParameter = false;
					}
				});
			}

			return base.Finalize(statement);
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			statement = ReplaceTakeSkipWithRowNum(statement, false);

			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((SqlDeleteStatement) statement); break;
				case QueryType.Update : statement = GetAlternativeUpdate((SqlUpdateStatement) statement); break;
			}

			statement = QueryHelper.OptimizeSubqueries(statement);

			return statement;
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "MOD",    be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2);
					case "|": // (a + b) - BITAND(a, b)
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2),
							be.SystemType);

					case "^": // (a + b) - BITAND(a, b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2), 2),
							be.SystemType);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Coalesce"       : return new SqlFunction(func.SystemType, "Nvl", func.Parameters);
					case "Convert"        :
						{
							var ftype = func.SystemType.ToUnderlying();

							if (ftype == typeof(bool))
							{
								var ex = AlternativeConvertToBoolean(func, 1);
								if (ex != null)
									return ex;
							}

							if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset))
							{
								if (IsTimeDataType(func.Parameters[0]))
								{
									if (func.Parameters[1].SystemType == typeof(string))
										return func.Parameters[1];

									return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("HH24:MI:SS"));
								}

								if (IsDateDataType(func.Parameters[0], "Date"))
								{
									if (func.Parameters[1].SystemType.ToUnderlying() == typeof(DateTime))
									{
										return new SqlFunction(func.SystemType, "Trunc", func.Parameters[1], new SqlValue("DD"));
									}

									return new SqlFunction(func.SystemType, "TO_DATE", func.Parameters[1], new SqlValue("YYYY-MM-DD"));
								}

								return new SqlFunction(func.SystemType, "TO_TIMESTAMP", func.Parameters[1], new SqlValue("YYYY-MM-DD HH24:MI:SS"));
							}

							return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
						}

					case "CharIndex"      :
						return func.Parameters.Length == 2?
							new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0]):
							new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0], func.Parameters[2]);
					case "AddYear"        : return new SqlFunction(func.SystemType, "Add_Months", func.Parameters[0], Mul(func.Parameters[1], 12));
					case "AddQuarter"     : return new SqlFunction(func.SystemType, "Add_Months", func.Parameters[0], Mul(func.Parameters[1],  3));
					case "AddMonth"       : return new SqlFunction(func.SystemType, "Add_Months", func.Parameters[0],     func.Parameters[1]);
					case "AddDayOfYear"   :
					case "AddWeekDay"     :
					case "AddDay"         : return Add<DateTime>(func.Parameters[0],     func.Parameters[1]);
					case "AddWeek"        : return Add<DateTime>(func.Parameters[0], Mul(func.Parameters[1], 7));
					case "AddHour"        : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1],                  24));
					case "AddMinute"      : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1],             60 * 24));
					case "AddSecond"      : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1],        60 * 60 * 24));
					case "AddMillisecond" : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1], 1000 * 60 * 60 * 24));
					case "Avg"            :
						return new SqlFunction(
							func.SystemType,
							"Round",
							new SqlFunction(func.SystemType, "AVG", func.Parameters[0]),
							new SqlValue(27));
				}
			}
			else if (expr is SqlExpression)
			{
				var e = (SqlExpression)expr;

				if (e.Expr.StartsWith("To_Number(To_Char(") && e.Expr.EndsWith(", 'FF'))"))
					return Div(new SqlExpression(e.SystemType, e.Expr.Replace("To_Number(To_Char(", "to_Number(To_Char("), e.Parameters), 1000);
			}

			return expr;
		}

		static ISqlExpression RowNumExpr = new SqlExpression(typeof(long), "ROWNUM", Precedence.Primary, true);

		/// <summary>
		/// Replaces Take/Skip by ROWNUM usage.
		/// See <a href="https://blogs.oracle.com/oraclemagazine/on-rownum-and-limiting-results">'Pagination with ROWNUM'</a> for more information.
		/// </summary>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="onlySubqueries">Indicates when transformation needed only for subqueries.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when optimization has been performed.</returns>
		protected SqlStatement ReplaceTakeSkipWithRowNum(SqlStatement statement, bool onlySubqueries)
		{
			return QueryHelper.WrapQuery(statement,
				query =>
				{
					if (query.Select.TakeValue == null && query.Select.SkipValue == null)
						return 0;
					if (query.Select.SkipValue != null)
						return 2;

					if (query.Select.TakeValue != null && query.Select.OrderBy.IsEmpty)
					{
						query.Select.Where.EnsureConjunction().Expr(RowNumExpr)
							.LessOrEqual.Expr(query.Select.TakeValue);

						query.Select.Take(null, null);
						return 0;
					}
						
					return 1;
				}
				, queries =>
				{
					var query = queries[queries.Length - 1];
					var processingQuery = queries[queries.Length - 2];

					if (query.Select.SkipValue != null)
					{
						var rnColumn = processingQuery.Select.AddNewColumn(RowNumExpr);
						rnColumn.Alias = "RN";

						if (query.Select.TakeValue != null)
						{
							processingQuery.Where.EnsureConjunction().Expr(RowNumExpr)
								.LessOrEqual.Expr(new SqlBinaryExpression(query.Select.SkipValue.SystemType,
									query.Select.SkipValue, "+", query.Select.TakeValue));
						}

						queries[queries.Length - 3].Where.Expr(rnColumn).Greater.Expr(query.Select.SkipValue);
					}
					else
					{
						processingQuery.Where.EnsureConjunction().Expr(RowNumExpr)
							.LessOrEqual.Expr(query.Select.TakeValue);
					}

					query.Select.SkipValue = null;
					query.Select.Take(null, null);

				});
		}
	}
}
