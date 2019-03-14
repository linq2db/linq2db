using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class FromClause : BaseClause
	{
		public Expression FromExpression { get; }

		public FromClause([NotNull] Expression fromExpression)
		{
			FromExpression = fromExpression ?? throw new ArgumentNullException(nameof(fromExpression));
		}

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			throw new NotImplementedException();
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			throw new NotImplementedException();
		}
	}
}
