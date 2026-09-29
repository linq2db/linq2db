using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// The interval elapsed between two temporal values.
	/// </summary>
	/// <remarks>
	/// The contract is exactly <c>End - Start</c>: elapsed time, signed, exact within the resolution of
	/// <see cref="IntervalType"/>. It is deliberately not a boundary count - <c>Sql.DateDiff</c> counts how many
	/// unit boundaries were crossed, which for 10:59 to 11:01 gives one hour where the elapsed duration is two
	/// minutes. Keeping the two apart in the AST is what stops that confusion from being reintroduced downstream.
	/// <para>
	/// There is no unit on this node. It produces a whole interval, not "a number of hours" - a unit only becomes
	/// meaningful when a part or a total is requested, which is <see cref="SqlIntervalPartExpression"/>.
	/// </para>
	/// </remarks>
	public sealed class SqlIntervalDifferenceExpression : SqlExpressionBase
	{
		public SqlIntervalDifferenceExpression(ISqlExpression start, ISqlExpression end, DbDataType type, SqlIntervalType intervalType)
		{
			Start        = start ?? throw new ArgumentNullException(nameof(start));
			End          = end   ?? throw new ArgumentNullException(nameof(end));
			Type         = type;
			IntervalType = intervalType;
		}

		/// <summary>
		/// Subtrahend - the earlier value in a positive result.
		/// </summary>
		public ISqlExpression  Start        { get; private set; }

		/// <summary>
		/// Minuend - the later value in a positive result.
		/// </summary>
		public ISqlExpression  End          { get; private set; }

		public DbDataType      Type         { get; private set; }

		/// <summary>
		/// Logical type of the produced interval.
		/// </summary>
		public SqlIntervalType IntervalType { get; private set; }

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type?            SystemType  => Type.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlIntervalDifference;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("ELAPSED(")
				.AppendElement(End)
				.Append(" - ")
				.AppendElement(Start)
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Type);
			hash.Add(IntervalType);
			hash.Add(Start.GetElementHashCode());
			hash.Add(End.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			// Operand order carries the sign, so Start and End must be compared in their own positions - a
			// position-insensitive comparison would make a difference equal to its own negation.
			return other is SqlIntervalDifferenceExpression otherDifference
				&& Type.Equals(otherDifference.Type)
				&& IntervalType.Equals(otherDifference.IntervalType)
				&& Start.Equals(otherDifference.Start, comparer)
				&& End.Equals(otherDifference.End, comparer);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlIntervalDifferenceExpression(this);

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Start.CanBeNullable(nullability) || End.CanBeNullable(nullability);
		}

		public void Modify(ISqlExpression start, ISqlExpression end, DbDataType type, SqlIntervalType intervalType)
		{
			Start        = start;
			End          = end;
			Type         = type;
			IntervalType = intervalType;
		}
	}
}
