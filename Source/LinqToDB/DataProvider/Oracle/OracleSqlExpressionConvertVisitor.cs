﻿using System;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Oracle
{
	public class OracleSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public OracleSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		protected static string[] OracleLikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => OracleLikeCharactersToEscape;

		#endregion

		public override IQueryElement ConvertExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var (a, op, b, withNull) = predicate;
			
			// We want to modify comparisons involving "" as Oracle treats "" as null

			// Comparisons to a literal constant "" are always converted to IS [NOT] NULL (same as == null or == default)
			if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				if (QueryHelper.UnwrapNullablity(a) is SqlValue { Value: string { Length: 0 } })
					return new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(b, true), isNot: op == SqlPredicate.Operator.NotEqual);
				if (QueryHelper.UnwrapNullablity(b) is SqlValue { Value: string { Length: 0 } })
					return new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(a, true), isNot: op == SqlPredicate.Operator.NotEqual);
			}

			// CompareNulls.LikeSql compiles as-is, no change
			// CompareNulls.LikeSqlExceptParameters sniffs parameters to == and != and replaces by IS [NOT] NULL
			// CompareNulls.LikeClr (withNull) always handles nulls.
			// Note: LikeClr sometimes generates `withNull: null` expressions, in which case it works the
			//       same way as LikeSqlExceptParameters (for backward compatibility).

			if (withNull != null
				|| (DataOptions.LinqOptions.CompareNulls != CompareNulls.LikeSql
					&& op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual))
			{
				if (Oracle11SqlOptimizer.IsTextType(b, MappingSchema)                   &&
				    b.TryEvaluateExpressionForServer(EvaluationContext, out var bValue) &&
					bValue is string { Length: 0 })
				{
					return CompareToEmptyString(a, op);
				}
				
				if (Oracle11SqlOptimizer.IsTextType(a, MappingSchema)                   &&
				    a.TryEvaluateExpressionForServer(EvaluationContext, out var aValue) &&
					aValue is string { Length: 0 })
				{
					return CompareToEmptyString(b, InvertDirection(op));
				}
			}

			return base.ConvertExprExprPredicate(predicate);

			static ISqlPredicate CompareToEmptyString(ISqlExpression x, SqlPredicate.Operator op)
			{
				return op switch
				{
					SqlPredicate.Operator.NotGreater     or
					SqlPredicate.Operator.LessOrEqual    or
					SqlPredicate.Operator.Equal          => new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(x, true), isNot: false),
					SqlPredicate.Operator.NotLess        or
					SqlPredicate.Operator.Greater        or
					SqlPredicate.Operator.NotEqual       => new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(x, true), isNot: true),
					SqlPredicate.Operator.GreaterOrEqual => new SqlPredicate.ExprExpr(
						// Always true
						new SqlValue(1), SqlPredicate.Operator.Equal, new SqlValue(1), withNull: null),
					SqlPredicate.Operator.Less           => new SqlPredicate.ExprExpr(
						// Always false
						new SqlValue(1), SqlPredicate.Operator.Equal, new SqlValue(0), withNull: null),
					// Overlaps doesn't operate on strings
					_ => throw new InvalidOperationException(),
				};
			}

			static SqlPredicate.Operator InvertDirection(SqlPredicate.Operator op)
			{
				return op switch 
				{
					SqlPredicate.Operator.NotEqual       or 
					SqlPredicate.Operator.Equal          => op,
					SqlPredicate.Operator.Greater        => SqlPredicate.Operator.Less,
					SqlPredicate.Operator.GreaterOrEqual => SqlPredicate.Operator.LessOrEqual,
					SqlPredicate.Operator.Less           => SqlPredicate.Operator.Greater,
					SqlPredicate.Operator.LessOrEqual    => SqlPredicate.Operator.GreaterOrEqual,
					SqlPredicate.Operator.NotGreater     => SqlPredicate.Operator.NotLess,
					SqlPredicate.Operator.NotLess        => SqlPredicate.Operator.NotGreater,
					// Overlaps doesn't operate on strings
					_ => throw new InvalidOperationException(),
				};
			}
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%": return new SqlFunction(element.SystemType, "MOD", element.Expr1, element.Expr2);
				case "&": return new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2);
				case "|": // (a + b) - BITAND(a, b)
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2),
						element.SystemType);

				case "^": // (a + b) - BITAND(a, b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2), 2),
						element.SystemType);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlExpression(SqlExpression element)
		{
			if (element.Expr.StartsWith("To_Number(To_Char(") && element.Expr.EndsWith(", 'FF'))"))
				return Div(new SqlExpression(element.SystemType, element.Expr.Replace("To_Number(To_Char(", "to_Number(To_Char("), element.Parameters), 1000);

			return base.ConvertSqlExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func)
			{
				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1],
					SystemType: var type,
				}:
					return new SqlFunction(type, "InStr", p1, p0);

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					SystemType: var type,
				}:
					return new SqlFunction(type, "InStr", p1, p0, p2);

				default:
					return base.ConvertSqlFunction(func);
			};
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var ftype = cast.SystemType.ToUnderlying();

			var toType   = cast.ToType;
			var argument = cast.Expression;

			if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
				|| ftype == typeof(DateOnly)
#endif
			   )
			{
				if (IsTimeDataType(toType))
				{
					if (argument.SystemType == typeof(string))
						return argument;

					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("HH24:MI:SS"));
				}

				if (IsDateDataType(toType, "Date"))
				{
					if (argument.SystemType!.ToUnderlying() == typeof(DateTime)
						|| argument.SystemType!.ToUnderlying() == typeof(DateTimeOffset))
					{
						return new SqlFunction(cast.SystemType, "Trunc", argument, new SqlValue("DD"));
					}

					return new SqlFunction(cast.SystemType, "TO_DATE", argument, new SqlValue("YYYY-MM-DD"));
				}
				else if (IsDateDataOffsetType(toType))
				{
					if (ftype == typeof(DateTimeOffset))
						return argument;

					return new SqlFunction(cast.SystemType, "TO_TIMESTAMP_TZ", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
				}

				return new SqlFunction(cast.SystemType, "TO_TIMESTAMP", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
			}
			else if (ftype == typeof(string))
			{
				var stype = argument.SystemType!.ToUnderlying();

				if (stype == typeof(DateTimeOffset))
				{
					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS TZH:TZM"));
				}
				else if (stype == typeof(DateTime))
				{
					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
				}
#if NET6_0_OR_GREATER
				else if (stype == typeof(DateOnly))
				{
					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD"));
				}
#endif
			}

			return FloorBeforeConvert(cast);
		}
	}
}
