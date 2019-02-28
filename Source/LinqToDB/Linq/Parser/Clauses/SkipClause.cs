using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class SkipClause : BaseClause
	{
		public Expression SkipExpression { get; }

		public SkipClause([NotNull] Expression skipExpression)
		{
			SkipExpression = skipExpression ?? throw new ArgumentNullException(nameof(skipExpression));
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
