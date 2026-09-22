using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	static class ClickHouseDateTime
	{
		/// <summary>
		/// Coerces a date/time operand to <c>DateTime64</c>, which is what the <c>toUnixTimestamp64*</c> family and
		/// the sub-second interval operators accept - a <c>DateTime</c>, <c>Date</c> or <c>Date32</c> operand is
		/// refused by name.
		/// </summary>
		/// <remarks>
		/// The operand's reported type has to be the one the server sees, which is why the
		/// <see cref="DateTime"/>-producing translations here name the type their SQL actually produces rather
		/// than the one the mapping schema gives a CLR <see cref="DateTime"/>.
		/// </remarks>
		public static ISqlExpression AsDateTime64(ISqlExpressionFactory factory, ISqlExpression expression)
		{
			var type = factory.GetDbDataType(expression);

			// Any DateTime64 precision is accepted, so an operand that already is one needs no conversion.
			if (type.DataType == DataType.DateTime64)
				return expression;

			// Built from the system type alone: a DbType carried over from the operand names the operand's own
			// ClickHouse type and would be rendered in preference to the DataType set here.
			var resultType = new DbDataType(type.SystemType, DataType.DateTime64)
				.WithPrecision(ClickHouseMappingSchema.DEFAULT_DATETIME64_PRECISION);

			return factory.Cast(expression, resultType);
		}
	}
}
