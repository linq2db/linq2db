using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlConcatExpression : SqlExpressionBase
	{
		public SqlConcatExpression(bool preserveNull, params ISqlExpression[] expressions)
		{
			PreserveNull = preserveNull;
			Expressions  = expressions;
		}

		/// <summary>
		/// When false, null values replaced with empty string.
		/// </summary>
		public bool PreserveNull { get; }

		public ISqlExpression[] Expressions { get; private set; }

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Concatenate;
		public override Type?            SystemType  => Expressions[0].SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlConcat;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append("$CONCAT$(");

			for (var index = 0; index < Expressions.Length; index++)
			{
				if (index > 0)
					writer.Append(", ");
				writer.AppendElement(Expressions[index]);
			}

			writer.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			foreach (var expression in Expressions)
				hash.Add(expression.GetElementHashCode());

			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlConcatExpression otherConcatExpression)
				return false;

			if (Expressions.Length != otherConcatExpression.Expressions.Length)
				return false;

			for (var index = 0; index < Expressions.Length; index++)
			{
				if (!Expressions[index].Equals(otherConcatExpression.Expressions[index], comparer))
					return false;
			}

			return true;
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlConcatExpression(this);

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return PreserveNull && Expressions.Any(e => e.CanBeNullable(nullability));
		}

		public void Modify(params ISqlExpression[] expressions)
		{
			Expressions = expressions;
		}
	}
}
