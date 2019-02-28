using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class TakeClause : BaseClause
	{
		public Expression TakeExpression { get; }

		public TakeClause([NotNull] Expression takeExpression)
		{
			TakeExpression = takeExpression ?? throw new ArgumentNullException(nameof(takeExpression));
		}

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
