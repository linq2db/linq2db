using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public class SqlDefaultIfEmptyExpression : Expression
	{
		public Expression InnerExpression    { get; }
		public Expression NotNullCondition { get; }

		public SqlDefaultIfEmptyExpression(Expression innerExpression, Expression notNullCondition)
		{
			InnerExpression    = innerExpression;
			NotNullCondition = notNullCondition;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;

		public override Expression Reduce() => InnerExpression;

		public SqlDefaultIfEmptyExpression Update(Expression innerExpression, Expression notNullCondition)
		{
			if (ReferenceEquals(InnerExpression, innerExpression) && 
			    ReferenceEquals(NotNullCondition, notNullCondition))
			{
				return this;
			}

			return new SqlDefaultIfEmptyExpression(innerExpression, notNullCondition);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlDefaultIfEmptyExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			return $"DFT({InnerExpression})[{NotNullCondition}]";
		}
	}
}
