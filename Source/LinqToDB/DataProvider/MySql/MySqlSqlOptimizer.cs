﻿using System.Collections.Generic;

namespace LinqToDB.DataProvider.MySql
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class MySqlSqlOptimizer : BasicSqlOptimizer
	{
		public MySqlSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override bool CanCompareSearchConditions => true;
		
		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Update => CorrectMySqlUpdate((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		private SqlUpdateStatement CorrectMySqlUpdate(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.SkipValue != null)
				throw new LinqToDBException("MySql does not support Skip in update query");

			statement = CorrectUpdateTable(statement);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "+":
						if (be.SystemType == typeof(string))
						{
							if (be.Expr1 is SqlFunction func)
							{
								if (func.Name == "Concat")
								{
									var list = new List<ISqlExpression>(func.Parameters) { be.Expr2 };
									return new SqlFunction(be.SystemType, "Concat", list.ToArray());
								}
							}
							else if (be.Expr1 is SqlBinaryExpression && be.Expr1.SystemType == typeof(string) && ((SqlBinaryExpression)be.Expr1).Operation == "+")
							{
								var list = new List<ISqlExpression> { be.Expr2 };
								var ex   = be.Expr1;

								while (ex is SqlBinaryExpression && ex.SystemType == typeof(string) && ((SqlBinaryExpression)be.Expr1).Operation == "+")
								{
									var bex = (SqlBinaryExpression)ex;

									list.Insert(0, bex.Expr2);
									ex = bex.Expr1;
								}

								list.Insert(0, ex);

								return new SqlFunction(be.SystemType, "Concat", list.ToArray());
							}

							return new SqlFunction(be.SystemType, "Concat", be.Expr1, be.Expr2);
						}

						break;
				}
			}
			else if (expression is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Convert" :
						var ftype = func.SystemType.ToUnderlying();

						if (ftype == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						if ((ftype == typeof(double) || ftype == typeof(float)) && func.Parameters[1].SystemType!.ToUnderlying() == typeof(decimal))
							return func.Parameters[1];

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}

			return expression;
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context);

			if (caseSensitive == null || caseSensitive == false)
			{
				var searchExpr = predicate.Expr2;
				var dataExpr = predicate.Expr1;

				if (caseSensitive == false)
				{
					searchExpr = new SqlFunction(typeof(string), "$ToLower$", searchExpr);
					dataExpr   = new SqlFunction(typeof(string), "$ToLower$", dataExpr);
				}

				ISqlPredicate? newPredicate = null;
				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						newPredicate = new SqlPredicate.ExprExpr(
							new SqlFunction(typeof(int), "LOCATE", searchExpr, dataExpr), SqlPredicate.Operator.Greater,
							new SqlValue(0), null);
						break;
					}
				}

				if (newPredicate != null)
				{
					if (predicate.IsNot)
					{
						newPredicate = new SqlSearchCondition(new SqlCondition(true, newPredicate));
					}

					return newPredicate;
				}

				if (caseSensitive == false)
				{
					predicate = new SqlPredicate.SearchString(
						dataExpr,
						predicate.IsNot,
						searchExpr,
						predicate.Kind,
						new SqlValue(false));
				}
			}

			if (caseSensitive == true)
			{
				predicate = new SqlPredicate.SearchString(
					new SqlExpression(typeof(string), $"{{0}} COLLATE utf8_bin", Precedence.Primary, predicate.Expr1),
					predicate.IsNot,
					predicate.Expr2,
					predicate.Kind,
					new SqlValue(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate, visitor);
		}
	}
}
