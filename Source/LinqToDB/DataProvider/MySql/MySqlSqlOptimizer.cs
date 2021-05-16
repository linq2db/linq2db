﻿using System.Collections.Generic;

namespace LinqToDB.DataProvider.MySql
{
	using Extensions;
	using LinqToDB.Mapping;
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

		public override ISqlExpression ConvertExpressionImpl<TContext>(ISqlExpression expression, ConvertVisitor<TContext> visitor,
			EvaluationContext context)
		{
			expression = base.ConvertExpressionImpl(expression, visitor, context);

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

		public override ISqlPredicate ConvertSearchStringPredicate<TContext>(MappingSchema mappingSchema, SqlPredicate.SearchString predicate,
			ConvertVisitor<RunOptimizationContext<TContext>> visitor,
			OptimizationContext optimizationContext)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(optimizationContext.Context);

			if (caseSensitive == false)
			{
				predicate = new SqlPredicate.SearchString(
					new SqlFunction(typeof(string), "$ToLower$", predicate.Expr1),
					predicate.IsNot,
					new SqlFunction(typeof(string), "$ToLower$", predicate.Expr2),
					predicate.Kind,
					new SqlValue(false));
			}
			else if (caseSensitive == true)
			{
				predicate = new SqlPredicate.SearchString(
					new SqlExpression(typeof(string), $"{{0}} COLLATE utf8_bin", Precedence.Primary, predicate.Expr1),
					predicate.IsNot,
					predicate.Expr2,
					predicate.Kind,
					new SqlValue(false));
			}

			return ConvertSearchStringPredicateViaLike(mappingSchema, predicate, visitor, optimizationContext);
		}
	}
}
