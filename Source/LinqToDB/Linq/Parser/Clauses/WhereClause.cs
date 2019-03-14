using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class WhereClause : BaseClause
	{
		public WhereClause(Expression searchExpression)
		{
			SearchExpression = searchExpression;
		}

		public Expression SearchExpression { get; internal set; }

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}

		public override void TransformExpression(Func<Expression, Expression> func)
		{
			SearchExpression = SearchExpression.Transform(func);
		}
	}
}
