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
		/// Unconditional, because an operand's declared type does not say what the server will make of it:
		/// <c>now()</c>, <c>makeDateTime</c> and the date truncation are all typed from the mapping schema, which
		/// maps a <see cref="DateTime"/> to <c>DateTime64(7)</c>. Never below a tick, never below what the operand
		/// already carries, so an operand that is already <c>DateTime64</c> keeps every digit it had.
		/// </remarks>
		public static ISqlExpression AsDateTime64(ISqlExpressionFactory factory, ISqlExpression expression)
		{
			var type      = factory.GetDbDataType(expression);
			var precision = Math.Max(type.Precision ?? 0, ClickHouseMappingSchema.DEFAULT_DATETIME64_PRECISION);

			// Built from the system type alone: a DbType carried over from the operand names the operand's own
			// ClickHouse type and would be rendered in preference to the DataType set here.
			var resultType = new DbDataType(type.SystemType, DataType.DateTime64).WithPrecision(precision);

			return factory.Function(resultType, "toDateTime64", expression, factory.Value(factory.GetDbDataType(typeof(int)), precision));
		}
	}
}
