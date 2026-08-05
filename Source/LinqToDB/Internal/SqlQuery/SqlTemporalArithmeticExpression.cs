using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// A temporal value shifted by an interval. The result is temporal, not an interval.
	/// </summary>
	/// <remarks>
	/// Kept as its own node rather than a <see cref="SqlBinaryExpression"/> because that one carries its operation
	/// as a plain string and its type as a <see cref="DbDataType"/>, neither of which has room for interval
	/// semantics - it would render as a literal infix and any optimizer would be free to treat it as ordinary
	/// arithmetic.
	/// </remarks>
	public sealed class SqlTemporalArithmeticExpression : SqlExpressionBase
	{
		public SqlTemporalArithmeticExpression(ISqlExpression temporal, ISqlExpression interval, bool isSubtract, DbDataType type)
		{
			Temporal   = temporal ?? throw new ArgumentNullException(nameof(temporal));
			Interval   = interval ?? throw new ArgumentNullException(nameof(interval));
			IsSubtract = isSubtract;
			Type       = type;
		}

		/// <summary>
		/// Value being shifted.
		/// </summary>
		public ISqlExpression Temporal   { get; private set; }

		/// <summary>
		/// Interval to shift by.
		/// </summary>
		public ISqlExpression Interval   { get; private set; }

		/// <summary>
		/// <see langword="true"/> to subtract the interval, <see langword="false"/> to add it.
		/// </summary>
		public bool           IsSubtract { get; }

		/// <summary>
		/// Result type - the temporal type of <see cref="Temporal"/>.
		/// </summary>
		public DbDataType     Type       { get; private set; }

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type?            SystemType  => Type.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlTemporalArithmetic;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("SHIFT(")
				.AppendElement(Temporal)
				.Append(IsSubtract ? " - " : " + ")
				.AppendElement(Interval)
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Type);
			hash.Add(IsSubtract);
			hash.Add(Temporal.GetElementHashCode());
			hash.Add(Interval.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			// IsSubtract must participate, or an addition and a subtraction of the same interval over the same
			// value compare equal while hashing differently.
			return other is SqlTemporalArithmeticExpression otherShift
				&& Type.Equals(otherShift.Type)
				&& IsSubtract == otherShift.IsSubtract
				&& Temporal.Equals(otherShift.Temporal, comparer)
				&& Interval.Equals(otherShift.Interval, comparer);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlTemporalArithmeticExpression(this);

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Temporal.CanBeNullable(nullability) || Interval.CanBeNullable(nullability);
		}

		public void Modify(ISqlExpression temporal, ISqlExpression interval, DbDataType type)
		{
			Temporal = temporal;
			Interval = interval;
			Type     = type;
		}
	}
}
