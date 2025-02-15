using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Linq;

namespace LinqToDB.DataProvider
{
	abstract class DatabaseSpecificTable<TSource> : ITable<TSource>
		where TSource : notnull
	{
		protected DatabaseSpecificTable(ITable<TSource> table)
		{
			_table = table;
		}

		readonly ITable<TSource> _table;

		public IEnumerator<TSource> GetEnumerator()
		{
			return _table.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_table).GetEnumerator();
		}

		public Expression Expression => _table.Expression;

		IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options) => _table.GetSqlQueries(options);
		public IDataContext   DataContext                                                     => _table.DataContext;
		public Type           ElementType                                                     => _table.ElementType;
		public IQueryProvider Provider                                                        => _table.Provider;

		public IQueryable CreateQuery(Expression expression)
		{
			return _table.CreateQuery(expression);
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return _table.CreateQuery<TElement>(expression);
		}

		public object Execute(Expression expression)
		{
			return _table.Execute(expression)!;
		}

		public TResult Execute<TResult>(Expression expression)
		{
			return _table.Execute<TResult>(expression);
		}

		public Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return _table.ExecuteAsyncEnumerable<TResult>(expression, cancellationToken);
		}

		public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return _table.ExecuteAsync<TResult>(expression, cancellationToken);
		}

		public string?      ServerName   => _table.ServerName;
		public string?      DatabaseName => _table.DatabaseName;
		public string?      SchemaName   => _table.SchemaName;
		public string       TableName    => _table.TableName;
		public TableOptions TableOptions => _table.TableOptions;
		public string?      TableID      => _table.TableID;
	}
}
