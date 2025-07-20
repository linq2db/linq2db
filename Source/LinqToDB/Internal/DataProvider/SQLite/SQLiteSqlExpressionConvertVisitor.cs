using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	sealed class SQLiteSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SQLiteSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "+": return element.SystemType == typeof(string)? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
				case "^": // (a + b) - (a & b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlBinaryExpression(element.SystemType, element.Expr1, "&", element.Expr2), 2), element.SystemType);
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func)
			{
				case {
					Name: "Space",
					Parameters: [var p0],
					Type: var type,
				}:
					return new SqlFunction(type, "PadR", new SqlValue(" "), p0);

				default:
					return base.ConvertSqlFunction(func);
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
			if (dbDataType.DataType == DataType.Date           ||
			    dbDataType.DataType == DataType.Time           ||
			    dbDataType.DataType == DataType.DateTime       ||
			    dbDataType.DataType == DataType.DateTime2      ||
			    dbDataType.DataType == DataType.DateTimeOffset ||
			    dbDataType.DataType == DataType.SmallDateTime  ||
			    dbDataType.DataType == DataType.Timestamp)
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

				var expr1 = QueryHelper.UnwrapNullablity(predicate.Expr1);
				if (!(expr1 is SqlCastExpression || expr1 is SqlFunction { DoNotOptimize: true }))
				{
					var left = PseudoFunctions.MakeMandatoryCast(predicate.Expr1, dateType, null);
					predicate = new SqlPredicate.ExprExpr(left, predicate.Operator, predicate.Expr2, predicate.UnknownAsValue);
				}

				var expr2 = QueryHelper.UnwrapNullablity(predicate.Expr2);
				if (!(expr2 is SqlCastExpression || expr2 is SqlFunction { DoNotOptimize: true }))
				{
					var right = PseudoFunctions.MakeMandatoryCast(predicate.Expr2, dateType, null);
					predicate = new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, right, predicate.UnknownAsValue);
				}
			}

			return base.ConvertExprExprPredicate(predicate);
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var underlying = cast.SystemType.ToUnderlying();

			if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)
#if NET8_0_OR_GREATER
			                                   || underlying == typeof(DateOnly)
#endif
			   )
			{
				if (!(cast.Expression.TryEvaluateExpression(EvaluationContext, out var value) && value is null))
				{
					return (ISqlExpression)Visit(WrapDateTime(cast.Expression, cast.ToType));
				}
			}

			return base.ConvertConversion(cast);
		}

		ISqlExpression WrapDateTime(ISqlExpression expression, DbDataType dbDataType)
		{
			if (IsDateTime(dbDataType))
			{
				if (!(expression is SqlCastExpression || expression is SqlFunction { DoNotOptimize: true }))
				{
					if (IsDateDataType(dbDataType, "Date"))
						return new SqlFunction(dbDataType, "Date", expression) { DoNotOptimize = true };

					return new SqlFunction(dbDataType, "strftime", ParametersNullabilityType.SameAsSecondParameter, new SqlValue("%Y-%m-%d %H:%M:%f"), expression) { DoNotOptimize = true };
				}
			}

			return expression;
		}
	}
}
