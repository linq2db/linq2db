using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class CountClause : BaseClause, IResultClause
	{
		public Expression FilterExpression { get; }

		public CountClause(Expression filterExpression, Type resultType)
		{
			FilterExpression = filterExpression;
			ResultType = resultType;
		}

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}

		public Type ResultType { get; }
	}
}
