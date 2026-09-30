using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	internal static class SQLiteDateTimeHelper
	{
		public static ISqlExpression ShiftDate(ISqlExpressionFactory factory, DbDataType type, ISqlExpression date, ISqlExpression modifier)
		{
			var format = factory.Value("%Y-%m-%d %H:%M:%f");
			SqlFunction Shift(DbDataType resultType, ISqlExpression value) => new SqlFunction(resultType, "strftime",
				ParametersNullabilityType.IfAnyParameterNullable, format, value, modifier) { DoNotOptimize = true };

			if (type.SystemType.ToUnderlying() != typeof(DateTimeOffset))
				return Shift(type, date);

			var stringType = factory.GetDbDataType(typeof(string));
			var suffix     = factory.Function(stringType, "Substr", date, factory.Value(-6));
			var hasOffset  = new SqlPredicate.Expr(factory.Expression(factory.GetDbDataType(typeof(bool)),
				"{0} GLOB {1}", suffix, factory.Value("[+-][0-9][0-9]:[0-9][0-9]")));
			var localDate = factory.Function(stringType, "Substr", date, factory.Value(1), factory.Decrement(factory.Length(date), 6));

			// Shift the local date directly to preserve both calendar semantics and the original offset.
			// Values ending in Z or without an offset use SQLite's UTC interpretation.
			var result = factory.Condition(hasOffset,
				factory.Concat(Shift(stringType, localDate), suffix),
				factory.Concat(Shift(stringType, date), factory.Value("+00:00")));

			// Keep offset-bearing results eligible for UTC normalization in comparisons.
			return factory.Expression(type, "{0}", result);
		}
	}
}
