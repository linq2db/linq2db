using System;

namespace LinqToDB.Extensions
{
	/// <summary>
	/// Contains data manipulation helpers (e.g. for use in query parameters).
	/// </summary>
	static class ValueExtensions
	{
		internal static readonly int[] TICKS_DIVIDERS =
		[
			10000000,
			1000000,
			100000,
			10000,
			1000,
			100,
			10,
			1
		];

		public static long GetTicks(this TimeSpan ts, int precision)
		{
			if (precision >= 7)
				return ts.Ticks;

			if (precision < 0)
				throw new InvalidOperationException(FormattableString.Invariant($"Precision must be >= 0: {precision}"));

			return ts.Ticks - (ts.Ticks % TICKS_DIVIDERS[precision]);
		}

		public static DateTimeOffset WithPrecision(this DateTimeOffset dto, int precision)
		{
			if (precision >= 7)
				return dto;

			if (precision < 0)
				throw new InvalidOperationException(FormattableString.Invariant($"Precision must be >= 0: {precision}"));

			var delta = dto.Ticks % TICKS_DIVIDERS[precision];
			return delta == 0 ? dto : dto.AddTicks(-delta);
		}

		public static DateTime WithPrecision(this DateTime dt, int precision)
		{
			if (precision >= 7)
				return dt;

			if (precision < 0)
				throw new InvalidOperationException(FormattableString.Invariant($"Precision must be >= 0: {precision}"));

			var delta = dt.Ticks % TICKS_DIVIDERS[precision];
			return delta == 0 ? dt : dt.AddTicks(-delta);
		}
	}
}
