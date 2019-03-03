using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class AnyClause : BaseClause
	{
		public AnyClause()
		{
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
