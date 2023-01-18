using System;
using System.Collections;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	abstract class DatabaseSpecificQueryable<TSource> : IQueryable<TSource>
	{
		protected DatabaseSpecificQueryable(IQueryable<TSource> queryable)
		{
			_queryable = queryable;
		}

		readonly IQueryable<TSource> _queryable;

		public IEnumerator<TSource> GetEnumerator()
		{
			return _queryable.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_queryable).GetEnumerator();
		}

		public Expression     Expression  => _queryable.Expression;
		public Type           ElementType => _queryable.ElementType;
		public IQueryProvider Provider    => _queryable.Provider;
	}
}
