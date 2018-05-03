using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class QueryableAccessor
	{
		public IQueryable                  Queryable;
		public Func<Expression,IQueryable> Accessor;
	}
}
