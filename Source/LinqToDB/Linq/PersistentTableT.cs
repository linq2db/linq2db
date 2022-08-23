using System.Collections;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
#if !NATIVE_ASYNC
	using Async;
#endif

	class PersistentTable<T> : ITable<T>
		where T : notnull
	{
		private readonly IQueryable<T> _query;

		public PersistentTable(IQueryable<T> query)
		{
			_query = query ?? ThrowHelper.ThrowArgumentNullException<IQueryable<T>>(nameof(query));
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _query.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Expression Expression => _query.Expression;
		Expression IExpressionQuery<T>.Expression
		{
			get => _query.Expression;
			set => ThrowHelper.ThrowNotImplementedException();
		}

		public string         SqlText     { get; } = null!;
		public IDataContext   DataContext => null!;
		public Type           ElementType => _query.ElementType;
		public IQueryProvider Provider    => _query.Provider;

		public IQueryable CreateQuery(Expression expression)
		{
			return _query.Provider.CreateQuery(expression);
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return _query.Provider.CreateQuery<TElement>(expression);
		}

		public object? Execute(Expression expression)
		{
			return _query.Provider.Execute(expression);
		}

		public TResult Execute<TResult>(Expression expression)
		{
			return _query.Provider.Execute<TResult>(expression);
		}

		public Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return ThrowHelper.ThrowNotImplementedException<Task<IAsyncEnumerable<TResult>>>();
		}

		public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return ThrowHelper.ThrowNotImplementedException<Task<TResult>>();
		}

		Expression IExpressionQuery.Expression => Expression;

		public string?      DatabaseName { get; }
		public string?      SchemaName   { get; }
		public string       TableName    { get; } = null!;
		public string?      ServerName   { get; }
		public TableOptions TableOptions { get; }
		public string?      TableID      { get; }
	}
}
