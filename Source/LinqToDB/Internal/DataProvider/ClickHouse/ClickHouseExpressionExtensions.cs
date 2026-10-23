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
		/// The coercion is unconditional: a reported <c>DateTime64</c> is as often the mapping schema's default for a
		/// CLR <see cref="DateTime"/> - a column that declares no type, or a translation that copies its operand's type -
		/// as what the server sees (#6115). A <c>DateTime64</c> finer than the default keeps its precision.
		/// </remarks>
		public static ISqlExpression AsDateTime64(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			var type = QueryHelper.GetDbDataTypeWithoutSchema(expression);

			var precision = type is { DataType: DataType.DateTime64, Precision: > ClickHouseMappingSchema.DEFAULT_DATETIME64_PRECISION }
				? type.Precision.Value
				: ClickHouseMappingSchema.DEFAULT_DATETIME64_PRECISION;

			// Built from the system type alone: a DbType carried over from the operand names the operand's own
			// ClickHouse type and would be rendered in preference to the DataType set here.
			var resultType = new DbDataType(type.SystemType, DataType.DateTime64).WithPrecision(precision);

			return factory.Cast(expression, resultType, true);
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
		/// the mapping schema makes of the CLR type, and a value written beside it takes its type from this one.
		/// </summary>
		public static DbDataType DateTimeType(DbDataType type)
		{
			return type.WithDataType(DataType.DateTime).WithPrecision(null);
		}
	}
}
