using System;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlCompareToExpression : SqlExpressionBase
	{
		public SqlCompareToExpression(ISqlExpression expression1, ISqlExpression expression2)
		{
			Expression1    = expression1;
			Expression2    = expression2;
		}

		public ISqlExpression Expression1 { get; private set; }
		public ISqlExpression Expression2 { get; private set; }

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Unknown;
		public override Type?            SystemType  => typeof(int);
		public override QueryElementType ElementType => QueryElementType.CompareTo;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("$CompareTo$(")
				.AppendElement(Expression1)
				.Append(", ")
				.AppendElement(Expression2)
				.Append(')');

			return writer;
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlCompareToExpression compareTo)
				return false;

			return Expression1.Equals(compareTo.Expression1, comparer) && Expression2.Equals(compareTo.Expression2, comparer);
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return false;
		}

		public void Modify(ISqlExpression expression1, ISqlExpression expression2)
		{
			Expression1 = expression1;
			Expression2 = expression2;
		}
	}
}
