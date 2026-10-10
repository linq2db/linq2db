using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Marks one usage of a <see cref="SqlParameter"/> as needing an explicit type in the generated SQL.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The need for a cast belongs to the position, not to the parameter: one instance is shared by every usage,
	/// so a flag on it could only cast everywhere or nowhere. Wrapping the usage keeps a single parameter - a
	/// single <c>DECLARE</c> - and casts only where the provider requires it.
	/// </para>
	/// <para>
	/// This is deliberately not a <see cref="SqlCastExpression"/>. It carries no target type of its own: the
	/// provider decides what to render (<c>BasicSqlBuilder.GetParameterCastType</c>), and several derive it from
	/// the value bound for the current execution - the length of the actual string, the facets of the actual
	/// decimal. Keeping it a separate node also means the code that reasons about real casts, such as the
	/// set-operation type correlation in <c>BasicSqlOptimizer</c>, never mistakes this marker for a cast whose
	/// type was chosen deliberately, and the cast folding in the optimizer cannot remove it.
	/// </para>
	/// </remarks>
	public sealed class SqlParameterCastExpression : SqlExpressionBase
	{
		public SqlParameterCastExpression(SqlParameter parameter)
		{
			Parameter = parameter;
		}

		public SqlParameter Parameter { get; private set; }

		public void Modify(SqlParameter parameter)
		{
			Parameter = parameter;
		}

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type             SystemType  => Parameter.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlParameterCast;

		public override bool CanBeNullable(NullabilityContext nullability) => Parameter.CanBeNullable(nullability);

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			return other is SqlParameterCastExpression otherCast && Parameter.Equals(otherCast.Parameter, comparer);
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(ElementType, Parameter.GetElementHashCode());
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("$PCast$(")
				.AppendElement(Parameter)
				.Append(')');

			return writer;
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlParameterCastExpression(this);
	}
}
