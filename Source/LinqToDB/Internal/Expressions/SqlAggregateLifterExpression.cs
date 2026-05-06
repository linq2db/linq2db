using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	/// <summary>
	/// Wraps a <see cref="SqlPlaceholderExpression"/> produced by an aggregate-function
	/// translator and carries up to two delegates that customize how the placeholder is
	/// processed at later phases of query building:
	/// <list type="bullet">
	///   <item><description><see cref="MaterializationCheck"/> — invoked at C# materialization
	///   time. Used by non-nullable Min/Max/Avg to wrap the column read with a runtime
	///   <c>CheckNullValue</c> call that throws on NULL.</description></item>
	///   <item><description><see cref="SqlRewriter"/> — invoked at OUTER APPLY lift time
	///   (in <c>AggregateExecuteContext.CreateWeakOuterJoin</c>) after <c>UpdateNesting</c>
	///   has promoted the inner aggregate to a parent-side column reference, or eagerly
	///   for grouped aggregates. Used by non-nullable Sum and StringJoin-family aggregates
	///   to wrap the lifted column reference with <c>COALESCE(&lt;ref&gt;, default)</c>.
	///   The bare aggregate stays in the inner SQL tree during provider validation/
	///   optimization.</description></item>
	/// </list>
	/// At least one of the two delegates is set. Both can be set when an aggregate needs
	/// runtime null-check AND late SQL coalesce wrapping; in that case, the conceptual order
	/// is: <c>SqlRewriter</c> runs at lift time, then <c>MaterializationCheck</c> at
	/// materialization. <see cref="CanReduce"/> returns <see langword="false"/> — neither delegate is
	/// auto-invoked by visitor reduction; consumers explicitly invoke them at the right
	/// hook point.
	/// </summary>
	public sealed class SqlAggregateLifterExpression : Expression
	{
		public Expression                                                InnerExpression      { get; }
		public Func<Expression, Expression>?                             MaterializationCheck { get; }
		public Func<SqlPlaceholderExpression, SqlPlaceholderExpression>? SqlRewriter          { get; }

		public SqlAggregateLifterExpression(
			Expression                                                inner,
			Func<Expression, Expression>?                             materializationCheck = null,
			Func<SqlPlaceholderExpression, SqlPlaceholderExpression>? sqlRewriter          = null)
		{
			if (materializationCheck == null && sqlRewriter == null)
				throw new ArgumentException($"At least one of {nameof(materializationCheck)} or {nameof(sqlRewriter)} must be provided.");

			InnerExpression      = inner;
			MaterializationCheck = materializationCheck;
			SqlRewriter          = sqlRewriter;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => false;
		public override Type           Type      => InnerExpression.Type;

		public SqlAggregateLifterExpression Update(Expression innerExpression)
		{
			if (ReferenceEquals(InnerExpression, innerExpression))
				return this;

			return new SqlAggregateLifterExpression(innerExpression, MaterializationCheck, SqlRewriter);
		}

		bool Equals(SqlAggregateLifterExpression other)
		{
			return InnerExpression.Equals(other.InnerExpression)
				&& Equals(MaterializationCheck, other.MaterializationCheck)
				&& Equals(SqlRewriter,          other.SqlRewriter);
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || (obj is SqlAggregateLifterExpression other && Equals(other));
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(InnerExpression, MaterializationCheck, SqlRewriter);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlAggregateLifterExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			var marker = MaterializationCheck != null && SqlRewriter != null ? "VS"
				: MaterializationCheck != null ? "V"
				: "S";
			return $"{marker}({InnerExpression})";
		}
	}
}
