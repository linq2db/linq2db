using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Async;

namespace LinqToDB.DataProvider
{
	abstract class DatabaseSpecificQueryable<TSource> : IQueryable<TSource>, IQueryProviderAsync
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

		Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return ((IQueryProviderAsync)_queryable).ExecuteAsyncEnumerable<TResult>(expression, cancellationToken);
		}

		Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return ((IQueryProviderAsync)_queryable).ExecuteAsync<TResult>(expression, cancellationToken);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			return Provider.CreateQuery(expression);
		}

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			return Provider.CreateQuery<TElement>(expression);
		}

		object? IQueryProvider.Execute(Expression expression)
		{
			return Provider.Execute(expression);
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			return Provider.Execute<TResult>(expression);
		}
	}
}
