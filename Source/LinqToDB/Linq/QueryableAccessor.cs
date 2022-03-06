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
			// skip first re-evaluation by parameter setter
			SkipForce = true;
		}

		public          bool                        SkipForce;
		public          IQueryable                  Queryable;
		public readonly Func<Expression,IQueryable> Accessor;
	}
}
