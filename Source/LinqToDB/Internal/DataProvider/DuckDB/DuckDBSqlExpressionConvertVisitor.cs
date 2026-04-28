using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSqlExpressionConvertVisitor(bool allowModify) : SqlExpressionConvertVisitor(allowModify)
	{
		protected override bool SupportsNullInColumn => false;

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == false
				&& searchPredicate is SqlPredicate.Like likePredicate)
			{
				return new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				"^" => new SqlExpression(element.Type, "xor({0}, {1})", Precedence.Primary, element.Expr1, element.Expr2),

				"+" when element.SystemType == typeof(string) =>
					new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence),

				// DuckDB performs float division by default (5/2 = 2.5), use integer division operator // for integer types
				"/" when element.SystemType.IsIntegerType =>
					new SqlBinaryExpression(element.SystemType, element.Expr1, "//", element.Expr2, element.Precedence),

				// DuckDB: DateTime +/- TimeSpan requires explicit CAST to INTERVAL
				// https://duckdb.org/docs/current/sql/functions/interval
				"+" or "-" when IsIntervalOperand(element.Expr1) && IsTimeOperand(element.Expr2) =>
					new SqlBinaryExpression(
						element.SystemType,
						element.Expr1,
						element.Operation,
						new SqlCastExpression(element.Expr2, QueryHelper.GetDbDataType(element.Expr2, MappingSchema).WithDataType(DataType.Interval), null, true),
						element.Precedence),

				"+" when IsIntervalOperand(element.Expr2) && IsTimeOperand(element.Expr1) =>
					new SqlBinaryExpression(
						element.SystemType,
						element.Expr2,
						element.Operation,
						new SqlCastExpression(element.Expr1, QueryHelper.GetDbDataType(element.Expr1, MappingSchema).WithDataType(DataType.Interval), null, true),
						element.Precedence),

				_ => base.ConvertSqlBinaryExpression(element),
			};
		}

		bool IsIntervalOperand(ISqlExpression expr)
		{
			var dbType = QueryHelper.GetDbDataType(expr, MappingSchema);
			var type   = dbType.SystemType.UnwrapNullableType();

			return (dbType.DataType is DataType.Timestamp or DataType.Time or DataType.DateTimeOffset or DataType.Interval or DataType.Date)
				|| (dbType.DataType is DataType.Undefined && (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
#if NET6_0_OR_GREATER
				 || type == typeof(DateOnly) || type == typeof(TimeOnly)
#endif
				));
		}

		bool IsTimeOperand(ISqlExpression expr)
		{
			var dbType = QueryHelper.GetDbDataType(expr, MappingSchema);

			return dbType.DataType is DataType.Time;
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{
					Name      : "CharIndex",
					Parameters: [var p0, var p1],
					Type      : var type,
				} => new SqlExpression(type, "Position({0} in {1})", Precedence.Primary, p0, p1),

				{
					Name      : "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type      : var type,
				} => Add<int>(
					new SqlExpression(
						type,
						"Position({0} in {1})",
						Precedence.Primary,
						p0,
						(ISqlExpression)Visit(
							new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "Substring",
								p1,
								p2,
								Sub<int>(
									(ISqlExpression)Visit(
										Factory.Length(p1)),
									p2))
						)
					),
					Sub(p2, 1)
				),

				// DuckDB lpad/rpad require VARCHAR first argument
				{
					Name      : "Lpad" or "Rpad",
					Parameters: [var str, ..],
				} when str.SystemType != typeof(string) =>
					base.ConvertSqlFunction(new SqlFunction(func.Type, func.Name, false, true,
						new SqlCastExpression(str, new DbDataType(typeof(string), DataType.VarChar), null, false), func.Parameters[1], func.Parameters[2])),

				_ => base.ConvertSqlFunction(func),
			};
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (cast.SystemType.ToUnderlying() == typeof(bool))
			{
				if (cast.IsMandatory && cast.Expression.SystemType?.UnwrapNullableType() == typeof(bool))
				{
					// do nothing
				}
				else if (cast.Expression is not SqlSearchCondition and not SqlCaseExpression)
				{
					return ConvertBooleanToCase(cast.Expression, cast.ToType);
				}
			}

			cast = FloorBeforeConvert(cast);
			return base.ConvertConversion(cast);
		}
	}
}
