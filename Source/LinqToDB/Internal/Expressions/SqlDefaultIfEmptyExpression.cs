using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public sealed class SqlDefaultIfEmptyExpression : Expression
	{
		public Expression                     InnerExpression    { get; }
		public ReadOnlyCollection<Expression> NotNullExpressions { get; }

		public SqlDefaultIfEmptyExpression(Expression innerExpression, ReadOnlyCollection<Expression> notNullExpressions)
		{
			InnerExpression    = innerExpression;
			NotNullExpressions = notNullExpressions;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;

		public override Expression Reduce() => InnerExpression;

		public SqlDefaultIfEmptyExpression Update(Expression innerExpression, ReadOnlyCollection<Expression> notNullExpressions)
		{
			if (ReferenceEquals(InnerExpression, innerExpression) &&
			    (ReferenceEquals(NotNullExpressions, notNullExpressions) || NotNullExpressions.SequenceEqual(notNullExpressions, ExpressionEqualityComparer.Instance)))
			{
				return this;
			}

			return new SqlDefaultIfEmptyExpression(innerExpression, notNullExpressions);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlDefaultIfEmptyExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			return $"DFT({InnerExpression})";
		}
	}
}
