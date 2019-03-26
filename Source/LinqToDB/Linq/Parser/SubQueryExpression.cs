using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser
{
	public class SubQueryExpression2 : BaseCustomExpression
	{
		public Sequence Sequence { get; }
		public Type ItemType { get; }
		public const ExpressionType ExpressionType = (ExpressionType) 200002;

		public SubQueryExpression2(Sequence sequence, Type itemType)
		{
			Sequence = sequence;
			ItemType = itemType;
		}

		public override ExpressionType NodeType => ExpressionType;
		public override Type Type => ItemType;

		public override string ToString ()
		{
			return $"Subquery({ItemType.Name})";
		}

		public override void CustomVisit(Action<Expression> func)
		{
			func(this);
		}

		public override bool CustomVisit(Func<Expression, bool> func)
		{
			return func(this);
		}

		public override Expression CustomFind(Func<Expression, bool> func)
		{
			if (func(this))
				return this;
			return null;
		}

		public override Expression CustomTransform(Func<Expression, Expression> func)
		{
			return func(this);
		}

		public override bool CustomEquals(Expression other)
		{
			if (other.GetType() != GetType())
				return false;

			var otherExpr = (SubQueryExpression2)other;
			return (otherExpr.Sequence == Sequence) && (otherExpr.ItemType == ItemType);
		}
	}
}
