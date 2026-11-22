using System;

using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Logical type of an interval expression: what it means, not how it is stored.
	/// </summary>
	/// <param name="Domain">Whether the interval is a fixed duration or a calendar interval.</param>
	/// <param name="Resolution">
	/// Finest unit the interval can represent exactly. This is what makes precision loss computable: an operation
	/// whose exact result needs a finer unit than this cannot be translated and must be rejected rather than rounded.
	/// </param>
	/// <param name="IsSigned">Whether the interval can represent negative values.</param>
	/// <remarks>
	/// Physical storage - a native <c>INTERVAL</c>, a <c>BIGINT</c> of ticks, a <c>DECIMAL</c> of seconds - is a
	/// separate concern, resolved from the mapping. Keeping the two apart is what lets the AST carry the intent
	/// through optimization and lower it per provider afterwards.
	/// </remarks>
	public readonly record struct SqlIntervalType(
		SqlIntervalDomain Domain,
		SqlIntervalUnit   Resolution,
		bool              IsSigned)
	{
		/// <summary>
		/// The logical type of a CLR <see cref="TimeSpan"/>: a signed fixed-length duration with tick resolution.
		/// </summary>
		public static readonly SqlIntervalType ClrTimeSpan = new(SqlIntervalDomain.Duration, SqlIntervalUnit.Tick, true);

		/// <summary>
		/// Returns the logical type of a duration stored in <paramref name="unit"/>.
		/// </summary>
		/// <param name="unit">Storage unit declared by the mapping.</param>
		/// <param name="isSigned">Whether the storage can hold negative values. Defaults to <see langword="true"/>.</param>
		public static SqlIntervalType ForDuration(DurationUnit unit, bool isSigned = true)
		{
			return new SqlIntervalType(SqlIntervalDomain.Duration, ToIntervalUnit(unit), isSigned);
		}

		/// <summary>
		/// Maps a storage unit to the corresponding interval unit. Total by construction:
		/// <see cref="DurationUnit"/> deliberately has no calendar members.
		/// </summary>
		/// <param name="unit">Storage unit declared by the mapping.</param>
		public static SqlIntervalUnit ToIntervalUnit(DurationUnit unit)
		{
			return unit switch
			{
				DurationUnit.Nanosecond  => SqlIntervalUnit.Nanosecond,
				DurationUnit.Tick        => SqlIntervalUnit.Tick,
				DurationUnit.Microsecond => SqlIntervalUnit.Microsecond,
				DurationUnit.Millisecond => SqlIntervalUnit.Millisecond,
				DurationUnit.Second      => SqlIntervalUnit.Second,
				DurationUnit.Minute      => SqlIntervalUnit.Minute,
				DurationUnit.Hour        => SqlIntervalUnit.Hour,
				DurationUnit.Day         => SqlIntervalUnit.Day,
				_                        => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
			};
		}
	}
}
