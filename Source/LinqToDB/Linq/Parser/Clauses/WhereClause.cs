using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class WhereClause : BaseClause
	{
		public WhereClause(Expression searchExpression)
		{
			SearchExpression = searchExpression;
		}

		public Expression SearchExpression { get; }

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}
	}
}
