using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	sealed class PersistentTable<T> : ITable<T>
		where T : notnull
	{
		private readonly IQueryable<T> _query;

		public PersistentTable(IQueryable<T> query)
		{
			_query = query ?? throw new ArgumentNullException(nameof(query));
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

		IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options) => Array.Empty<QuerySql>();

		public IDataContext   DataContext                                                     => null!;
		public Type           ElementType                                                     => _query.ElementType;
		public IQueryProvider Provider                                                        => _query.Provider;

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
			throw new NotImplementedException();
		}

		public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
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
