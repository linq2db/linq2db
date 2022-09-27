using System;
using System.Data.SqlTypes;

namespace Tests
{
	public static class TestDataExtensions
	{
		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="DateTime"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTime? TrimPrecision(this DateTime? value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			if (value == null)
				return null;

			return value.Value.TrimPrecision(precision);
		}

		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="DateTime"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTime TrimPrecision(this DateTime value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			return value.AddTicks(- ((value.Ticks % 10_000_000) % (long)Math.Pow(10, 7 - precision)));
		}

		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="SqlDateTime"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static SqlDateTime? TrimPrecision(this SqlDateTime? value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			if (value == null)
				return null;

			return value.Value.TrimPrecision(precision);
		}

		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="SqlDateTime"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static SqlDateTime TrimPrecision(this SqlDateTime value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			var dateTime = (DateTime)value;
			return dateTime.AddTicks(-((dateTime.Ticks % 10_000_000) % (long)Math.Pow(10, 7 - precision)));
		}

		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="TimeSpan"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static TimeSpan? TrimPrecision(this TimeSpan? value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			if (value == null)
				return null;

			return value.Value.TrimPrecision(precision);
		}

		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="TimeSpan"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static TimeSpan TrimPrecision(this TimeSpan value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			return value - TimeSpan.FromTicks(((value.Ticks % 10_000_000) % (long)Math.Pow(10, 7 - precision)));
		}

		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="DateTimeOffset"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTimeOffset? TrimPrecision(this DateTimeOffset? value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			if (value == null)
				return null;

			return value.Value.TrimPrecision(precision);
		}

		/// <summary>
		/// Remove ticks, according to specified precision (0-7 range).
		/// </summary>
		/// <param name="value"><see cref="DateTimeOffset"/> instance to trim.</param>
		/// <param name="precision">Returning value precision (0-7) range. Specify number of fractional second digits.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTimeOffset TrimPrecision(this DateTimeOffset value, int precision)
		{
			if (precision is < 0 or > 7)
				throw new InvalidOperationException();

			return value.AddTicks(-((value.Ticks % 10_000_000) % (long)Math.Pow(10, 7 - precision)));
		}

		/// <summary>
		/// Remove seconds/fractional seconds and add <paramref name="addMinutes"/> minutes to resulting value.
		/// </summary>
		/// <param name="value"><see cref="DateTime"/> instance to trim.</param>
		/// <param name="addMinutes">Number of minutes to add to resulting value.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTime? TrimSeconds(this DateTime? value, int addMinutes)
		{
			if (value == null)
				return null;

			return value.Value.TrimSeconds(addMinutes);
		}

		/// <summary>
		/// Remove seconds/fractional seconds and add <paramref name="addMinutes"/> minutes to resulting value.
		/// </summary>
		/// <param name="value"><see cref="DateTime"/> instance to trim.</param>
		/// <param name="addMinutes">Number of minutes to add to resulting value.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTime TrimSeconds(this DateTime value, int addMinutes)
		{
			value = value.TrimPrecision(0);
			return value.AddSeconds(-value.Second).AddMinutes(addMinutes);
		}

		/// <summary>
		/// Remove seconds/fractional seconds and add <paramref name="addMinutes"/> minutes to resulting value.
		/// </summary>
		/// <param name="value"><see cref="SqlDateTime"/> instance to trim.</param>
		/// <param name="addMinutes">Number of minutes to add to resulting value.</param>
		/// <returns>Trimmed value.</returns>
		public static SqlDateTime? TrimSeconds(this SqlDateTime? value, int addMinutes)
		{
			if (value == null)
				return null;

			return value.Value.TrimSeconds(addMinutes);
		}

		/// <summary>
		/// Remove seconds/fractional seconds and add <paramref name="addMinutes"/> minutes to resulting value.
		/// </summary>
		/// <param name="value"><see cref="SqlDateTime"/> instance to trim.</param>
		/// <param name="addMinutes">Number of minutes to add to resulting value.</param>
		/// <returns>Trimmed value.</returns>
		public static SqlDateTime TrimSeconds(this SqlDateTime value, int addMinutes)
		{
			var dateTime = (DateTime)value;
			dateTime = dateTime.TrimPrecision(0);
			return dateTime.AddSeconds(-dateTime.Second).AddMinutes(addMinutes);
		}

		/// <summary>
		/// Remove seconds/fractional seconds and add <paramref name="addMinutes"/> minutes to resulting value.
		/// </summary>
		/// <param name="value"><see cref="DateTimeOffset"/> instance to trim.</param>
		/// <param name="addMinutes">Number of minutes to add to resulting value.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTimeOffset? TrimSeconds(this DateTimeOffset? value, int addMinutes)
		{
			if (value == null)
				return null;

			return value.Value.TrimSeconds(addMinutes);
		}

		/// <summary>
		/// Remove seconds/fractional seconds and add <paramref name="addMinutes"/> minutes to resulting value.
		/// </summary>
		/// <param name="value"><see cref="DateTimeOffset"/> instance to trim.</param>
		/// <param name="addMinutes">Number of minutes to add to resulting value.</param>
		/// <returns>Trimmed value.</returns>
		public static DateTimeOffset TrimSeconds(this DateTimeOffset value, int addMinutes)
		{
			value = value.TrimPrecision(0);
			return value.AddSeconds(-value.Second).AddMinutes(addMinutes);
		}
	}
}
