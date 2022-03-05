using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class QueryableAccessor
	{
		public QueryableAccessor(Func<Expression, IQueryable> accessor, Expression expr)
		{
			Accessor  = accessor;
			Queryable = accessor(expr);
		}

		public readonly IQueryable                  Queryable;
		public readonly Func<Expression,IQueryable> Accessor;
	}
}
