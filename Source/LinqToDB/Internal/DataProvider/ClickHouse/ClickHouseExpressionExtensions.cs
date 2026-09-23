using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	static class ClickHouseExpressionExtensions
	{
		/// <summary>
		/// Coerces a date/time operand to <c>DateTime64</c>, which is what the <c>toUnixTimestamp64*</c> family and
		/// the sub-second interval operators accept - a <c>DateTime</c>, <c>Date</c> or <c>Date32</c> operand is
		/// refused by name.
		/// </summary>
		/// <remarks>
		/// The operand's reported type has to be the one the server sees, which is why the
		/// <see cref="DateTime"/>-producing translations name the type their SQL actually produces rather
		/// than the one the mapping schema gives a CLR <see cref="DateTime"/>.
		/// </remarks>
		public static ISqlExpression AsDateTime64(this ISqlExpressionFactory factory, ISqlExpression expression)
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

		/// <summary>
		/// Truncates a date/time operand to its date, as <c>toDate32(…)</c>.
		/// </summary>
		public static ISqlExpression AsDate32(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return factory.Cast(expression, new DbDataType(typeof(DateTime), DataType.Date32), true);
		}

		/// <summary>
		/// Nanoseconds since the epoch, <c>toUnixTimestamp64Nano(…)</c>, with the operand coerced by <see cref="AsDateTime64"/>.
		/// </summary>
		public static ISqlExpression ToUnixTimestamp64Nano(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return factory.Function(factory.GetDbDataType(typeof(long)), "toUnixTimestamp64Nano", factory.AsDateTime64(expression));
		}

		/// <summary>
		/// Milliseconds since the epoch, <c>toUnixTimestamp64Milli(…)</c>, with the operand coerced by <see cref="AsDateTime64"/>.
		/// </summary>
		public static ISqlExpression ToUnixTimestamp64Milli(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return factory.Function(factory.GetDbDataType(typeof(long)), "toUnixTimestamp64Milli", factory.AsDateTime64(expression));
		}

		/// <summary>
		/// <c>now()</c>, or <c>now('UTC')</c> when <paramref name="utc"/> is set.
		/// </summary>
		public static ISqlExpression Now(this ISqlExpressionFactory factory, DbDataType type, bool utc)
		{
			var nowType = DateTimeType(type);

			return utc
				? factory.Function(nowType, "now", factory.Value("UTC"))
				: factory.Function(nowType, "now", ParametersNullabilityType.NotNullable);
		}

		/// <summary>
		/// Type of a <c>now(…)</c> or <c>makeDateTime(…)</c> result. The server answers a <c>DateTime</c> whatever
		/// the mapping schema makes of the CLR type, and an operand that claims <c>DateTime64</c> is one the epoch
		/// functions then refuse.
		/// </summary>
		public static DbDataType DateTimeType(DbDataType type)
		{
			return type.WithDataType(DataType.DateTime).WithPrecision(null);
		}
	}
}
