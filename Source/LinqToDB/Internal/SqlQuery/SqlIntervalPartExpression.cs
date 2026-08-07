using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// A component or a total of an interval, expressed in a requested unit.
	/// </summary>
	/// <remarks>
	/// <see cref="SqlIntervalPartKind.Component"/> reproduces <c>TimeSpan.Hours</c>, truncating toward zero as CLR
	/// integer division does; <see cref="SqlIntervalPartKind.Total"/> reproduces <c>TimeSpan.TotalHours</c> and keeps
	/// the fraction. Provider division and modulo do not agree on negative values, so lowering must express the
	/// truncation explicitly rather than lean on the database's own rules.
	/// </remarks>
	public sealed class SqlIntervalPartExpression : SqlExpressionBase
	{
		public SqlIntervalPartExpression(ISqlExpression interval, SqlIntervalUnit unit, SqlIntervalPartKind kind, DbDataType type, SqlIntervalUnit? within = null)
		{
			Interval = interval ?? throw new ArgumentNullException(nameof(interval));
			Unit     = unit;
			Kind     = kind;
			Type     = type;
			Within   = within;
		}

		/// <summary>
		/// Interval the part is taken from.
		/// </summary>
		public ISqlExpression      Interval { get; private set; }

		/// <summary>
		/// Unit the result is expressed in.
		/// </summary>
		public SqlIntervalUnit     Unit     { get; private set; }

		/// <summary>
		/// Whether a component within an enclosing unit or the whole interval is requested.
		/// </summary>
		public SqlIntervalPartKind Kind     { get; private set; }

		/// <summary>
		/// The unit a <see cref="SqlIntervalPartKind.Component"/> is counted within, or <see langword="null"/> when
		/// it is not counted within anything - <c>TimeSpan.Days</c> is the whole day count and does not wrap.
		/// Meaningless for <see cref="SqlIntervalPartKind.Total"/>.
		/// </summary>
		/// <remarks>
		/// Stated rather than implied. "The next coarser unit" cannot express the two readings the same unit has -
		/// <c>TimeSpan.Microseconds</c> counts within the millisecond while <c>DATEPART(microsecond, ...)</c>
		/// counts within the second - and it is the enclosing unit, not this one, that decides whether a component
		/// survives a coarse measurement: nanoseconds within a microsecond stay meaningful at tick resolution,
		/// because a tick is a hundred of them.
		/// </remarks>
		public SqlIntervalUnit?    Within   { get; private set; }

		/// <summary>
		/// Result type - integral for <see cref="SqlIntervalPartKind.Component"/>, floating point for
		/// <see cref="SqlIntervalPartKind.Total"/>.
		/// </summary>
		public DbDataType          Type     { get; private set; }

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type?            SystemType  => Type.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlIntervalPart;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append(Kind == SqlIntervalPartKind.Total ? "TOTAL_" : "PART_")
				.Append(Unit.ToString().ToUpperInvariant())
				.Append('(')
				.AppendElement(Interval)
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Type);
			hash.Add(Unit);
			hash.Add(Kind);
			hash.Add(Within);
			hash.Add(Interval.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			// Kind must participate: Minutes and TotalMinutes over the same interval differ only by it, and for
			// 90 minutes they are 30 and 90 respectively.
			return other is SqlIntervalPartExpression otherPart
				&& Type.Equals(otherPart.Type)
				&& Unit == otherPart.Unit
				&& Kind == otherPart.Kind
				&& Within == otherPart.Within
				&& Interval.Equals(otherPart.Interval, comparer);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlIntervalPartExpression(this);

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Interval.CanBeNullable(nullability);
		}

		public SqlIntervalPartExpression WithInterval(ISqlExpression interval)
		{
			if (ReferenceEquals(interval, Interval))
				return this;
			return new SqlIntervalPartExpression(interval, Unit, Kind, Type, Within);
		}

		public void Modify(ISqlExpression interval, SqlIntervalUnit unit, SqlIntervalPartKind kind, DbDataType type, SqlIntervalUnit? within)
		{
			Interval = interval;
			Unit     = unit;
			Kind     = kind;
			Type     = type;
			Within   = within;
		}
	}
}
