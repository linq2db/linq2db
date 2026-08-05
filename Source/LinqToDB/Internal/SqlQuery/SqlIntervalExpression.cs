using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// An interval built from a scalar amount expressed in a known unit.
	/// </summary>
	/// <remarks>
	/// Covers a duration column whose unit the mapping declares, a <see cref="TimeSpan"/> parameter, and a literal
	/// such as <c>TimeSpan.FromHours(5)</c>. The unit is <see cref="SqlIntervalType"/>.<c>Resolution</c> - a single
	/// source of truth, so the value and its unit cannot drift apart.
	/// </remarks>
	public sealed class SqlIntervalExpression : SqlExpressionBase
	{
		public SqlIntervalExpression(ISqlExpression value, DbDataType type, SqlIntervalType intervalType)
		{
			Value        = value ?? throw new ArgumentNullException(nameof(value));
			Type         = type;
			IntervalType = intervalType;
		}

		/// <summary>
		/// Scalar amount, counted in <see cref="SqlIntervalType"/>.<c>Resolution</c> units.
		/// </summary>
		public ISqlExpression  Value        { get; private set; }

		public DbDataType      Type         { get; private set; }

		/// <summary>
		/// Logical type of the produced interval.
		/// </summary>
		public SqlIntervalType IntervalType { get; private set; }

		/// <summary>
		/// Unit <see cref="Value"/> is counted in.
		/// </summary>
		public SqlIntervalUnit Unit => IntervalType.Resolution;

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type?            SystemType  => Type.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlInterval;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("INTERVAL(")
				.AppendElement(Value)
				.Append(' ')
				.Append(IntervalType.Resolution.ToString())
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Type);
			hash.Add(IntervalType);
			hash.Add(Value.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			// Every component GetElementHashCode() hashes has to be compared here too, or two intervals differing
			// only in unit are Equals-equal with different hash codes and any hash-keyed dedup treats them as
			// distinct. A seconds interval and a ticks interval over the same column are exactly that pair.
			return other is SqlIntervalExpression otherInterval
				&& Type.Equals(otherInterval.Type)
				&& IntervalType.Equals(otherInterval.IntervalType)
				&& Value.Equals(otherInterval.Value, comparer);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlIntervalExpression(this);

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Value.CanBeNullable(nullability);
		}

		public SqlIntervalExpression WithValue(ISqlExpression value)
		{
			if (ReferenceEquals(value, Value))
				return this;
			return new SqlIntervalExpression(value, Type, IntervalType);
		}

		public void Modify(ISqlExpression value, DbDataType type, SqlIntervalType intervalType)
		{
			Value        = value;
			Type         = type;
			IntervalType = intervalType;
		}
	}
}
