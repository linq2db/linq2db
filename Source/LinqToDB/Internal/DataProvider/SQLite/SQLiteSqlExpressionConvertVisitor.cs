using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SQLiteSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				"+" when element.SystemType == typeof(string) => new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence),
				"+" => element,

				// (a + b) - (a & b) * 2
				"^" => Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlBinaryExpression(element.SystemType, element.Expr1, "&", element.Expr2), 2),
						element.SystemType
					),

				_ => base.ConvertSqlBinaryExpression(element),
			};
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)),
									"Substr",
									predicate.Expr1,
									new SqlValue(1),
									Factory.Length(predicate.Expr2)
								),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)),
									"Substr",
									predicate.Expr1,
									new SqlBinaryExpression(
										typeof(int),
										Factory.Length(predicate.Expr2), "*", new SqlValue(-1),
										Precedence.Multiplicative
									)
								),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "InStr", predicate.Expr1, predicate.Expr2),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(predicate.IsNot, canBeUnknown: null,
						like,
						subStrPredicate.MakeNot(predicate.IsNot));

					return result;
				}
			}

			return like;
		}

		private static bool IsDateTime(DbDataType dbDataType)
		{
			if (dbDataType.DataType
					is DataType.Date
					or DataType.Time
					or DataType.DateTime
					or DataType.DateTime2
					or DataType.DateTimeOffset
					or DataType.SmallDateTime
					or DataType.Timestamp)
				return true;

			if (dbDataType.DataType != DataType.Undefined)
				return false;

			return IsDateTime(dbDataType.SystemType);
		}

		private static bool IsDateTime(Type type)
		{
			return    type    == typeof(DateTime)
			          || type == typeof(DateTimeOffset)
			          || type == typeof(DateTime?)
			          || type == typeof(DateTimeOffset?);
		}

		public override IQueryElement ConvertExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var leftType  = QueryHelper.GetDbDataType(predicate.Expr1, MappingSchema);
			var rightType = QueryHelper.GetDbDataType(predicate.Expr2, MappingSchema);

			if (IsDateTime(leftType) || IsDateTime(rightType))
			{
				var dateType = IsDateTime(leftType) ? leftType : rightType;
				var expr1 = GetActualExpr(predicate.Expr1);
				if (expr1 is not (SqlCastExpression or SqlFunction { DoNotOptimize: true }))
				{
					var left = PseudoFunctions.MakeMandatoryCast(predicate.Expr1, dateType, null);
					predicate = new SqlPredicate.ExprExpr(left, predicate.Operator, predicate.Expr2, predicate.UnknownAsValue);
				}

				var expr2 = GetActualExpr(predicate.Expr2);
				if (expr2 is not (SqlCastExpression or SqlFunction { DoNotOptimize: true }))
				{
					var right = PseudoFunctions.MakeMandatoryCast(predicate.Expr2, dateType, null);
					predicate = new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, right, predicate.UnknownAsValue);
				}
			}

			return base.ConvertExprExprPredicate(predicate);

			static ISqlExpression GetActualExpr(ISqlExpression expr)
			{
				expr = QueryHelper.UnwrapNullablity(expr);

				if (expr is SelectQuery selectQuery && selectQuery.Select.Columns.Count == 1)
				{
					expr = selectQuery.Select.Columns[0].Expression;
				}

				return expr;
			}
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var underlying = cast.SystemType.ToUnderlying();

			if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)
#if SUPPORTS_DATEONLY
			                                   || underlying == typeof(DateOnly)
#endif
			   )
			{
				if (!(cast.Expression.TryEvaluateExpression(EvaluationContext, out var value) && value is null))
				{
					var newExpr = WrapDateTime(cast.Expression, cast.ToType);

					if (!ReferenceEquals(cast.Expression, newExpr))
						return (ISqlExpression)Visit(newExpr);
				}
			}

			return base.ConvertConversion(cast);
		}

		ISqlExpression WrapDateTime(ISqlExpression expression, DbDataType dbDataType)
		{
			if (IsDateTime(dbDataType))
			{
				if (expression is not (SqlCastExpression or SqlFunction { DoNotOptimize: true }))
				{
					if (IsDateDataType(dbDataType, "Date"))
						return new SqlFunction(dbDataType, "Date", expression) { DoNotOptimize = true };

					if (expression is SqlFunction { Parameters: [SqlValue { Value: "%Y-%m-%d %H:%M:%f" }, var expr] })
						expression = expr;

					return new SqlFunction(dbDataType, "strftime", ParametersNullabilityType.SameAsSecondParameter, new SqlValue("%Y-%m-%d %H:%M:%f"), expression) { DoNotOptimize = true };
				}
			}

			return expression;
		}
	}
}
