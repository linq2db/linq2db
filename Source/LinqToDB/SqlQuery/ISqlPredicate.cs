using System;

namespace LinqToDB.SqlQuery
{
	public interface ISqlPredicate : IQueryElement
	{
		int  Precedence { get; }

		bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer);
	}
}
