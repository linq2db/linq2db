using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Async;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider
{
	public abstract class DatabaseSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : IExpressionQuery<TSource>
	{
		public IEnumerator<TSource> GetEnumerator() 
			=> query.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() 
			=> ((IEnumerable)query).GetEnumerator();

		public Expression     Expression  => query.Expression;
		public IDataContext   DataContext => query.DataContext;
		public QueryDebugView DebugView   => query.DebugView;

		public IReadOnlyList<QuerySql> GetSqlQueries(SqlGenerationOptions? options) 
			=> query.GetSqlQueries(options);

		public Type                    ElementType => query.ElementType;
		public IQueryProvider          Provider    => query.Provider;

		Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken) 
			=> query.ExecuteAsyncEnumerable<TResult>(expression, cancellationToken);

		Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) 
			=> query.ExecuteAsync<TResult>(expression, cancellationToken);

		IQueryable IQueryProvider.CreateQuery(Expression expression) 
			=> Provider.CreateQuery(expression);

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) 
			=> Provider.CreateQuery<TElement>(expression);

		object? IQueryProvider.Execute(Expression expression) 
			=> Provider.Execute(expression);

		TResult IQueryProvider.Execute<TResult>(Expression expression) 
			=> Provider.Execute<TResult>(expression);
	}
}
