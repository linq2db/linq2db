using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Preserves the storage representation and semantic origin of a duration through SQL-tree
	/// optimization. Provider conversion lowers the wrapped value to canonical CLR ticks only after
	/// projections, CTEs and set operations have been finalized.
	/// </summary>
	internal sealed class SqlDurationExpression : SqlExpressionBase
	{
		public SqlDurationExpression(DbDataType type, ISqlExpression expression, DurationUnit unit, SqlDurationKind kind)
		{
			Type       = type;
			Expression = expression;
			Unit       = unit;
			Kind       = kind;
		}

		public DbDataType     Type       { get; }
		public ISqlExpression Expression { get; private set; }
		public DurationUnit   Unit       { get; }
		public SqlDurationKind Kind      { get; }

		public void Modify(ISqlExpression expression)
		{
			Expression = expression;
		}

		public override int              Precedence  => Expression.Precedence;
		public override Type             SystemType  => Type.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlDuration;

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Expression.CanBeNullable(nullability);
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			return other is SqlDurationExpression duration
				&& Type == duration.Type
				&& Unit == duration.Unit
				&& Kind == duration.Kind
				&& Expression.Equals(duration.Expression, comparer);
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(ElementType, Type, Unit, Kind, Expression.GetElementHashCode());
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer
				.DebugAppendUniqueId(this)
				.Append("DURATION(")
				.Append(Kind.ToString())
				.Append(", ")
				.Append(Unit.ToString())
				.Append(", ")
				.AppendElement(Expression)
				.Append(')');
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlDurationExpression(this);
	}
}
