using System;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class SqlCoalesceExpression : SqlExpressionBase
	{
		public SqlCoalesceExpression(params ISqlExpression[] expressions)
		{
			Expressions = expressions;
		}

		public ISqlExpression[] Expressions { get; private set; }

		public override int              Precedence  => SqlQuery.Precedence.LogicalDisjunction;
		public override Type?            SystemType  => Expressions[0].SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlCoalesce;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append("$COALESCE$(");

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
			{
				hash.Add(expression.GetElementHashCode());
			}

			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlCoalesceExpression otherCoalesceExpression)
				return false;

			if (Expressions.Length != otherCoalesceExpression.Expressions.Length)
				return false;

			for (var index = 0; index < Expressions.Length; index++)
			{
				if (!Expressions[index].Equals(otherCoalesceExpression.Expressions[index], comparer))
					return false;
			}

			return true;
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Expressions.All(e => e.CanBeNullable(nullability));
		}

		public void Modify(params ISqlExpression[] expressions)
		{
			Expressions = expressions;
		}
	}
}
