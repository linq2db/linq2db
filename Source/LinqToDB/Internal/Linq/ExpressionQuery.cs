using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Tools;

namespace LinqToDB.Internal.Linq
{
	abstract class ExpressionQuery<T> : IExpressionQuery<T>, IAsyncEnumerable<T>
	{
		#region Init

		protected void Init(IDataContext dataContext, Expression? expression)
		{
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
			Expression  = expression  ?? Expression.Constant(this);
		}

		public Expression   Expression  { get; set; } = null!;
		public IDataContext DataContext { get; set; } = null!;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal Query<T>?  Info;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal object?[]? Parameters;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal object?[]? Preambles;

		#endregion

		#region Public Members

		IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options)
		{
			var oldInline = DataContext.InlineParameters;

			if (options?.InlineParameters != null)
				DataContext.InlineParameters = options.InlineParameters.Value;

			var expression  = Expression;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var info        = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (options?.MultiInsertMode != null && info.Queries[0].Statement is SqlMultiInsertStatement multiInsert)
				multiInsert.InsertType = options.MultiInsertMode.Value;

			if (!dependsOnParameters)
			{
				Expression = expressions.MainExpression;
			}

			var sqlText    = QueryRunner.GetSqlText(info, DataContext, expressions, Parameters, Preambles);

			DataContext.InlineParameters = oldInline;

			return sqlText;
		}

		public virtual QueryDebugView DebugView
			=> new(
				() => new ExpressionPrinter().PrintExpression(Expression),
				() => ((IExpressionQuery)this).GetSqlQueries(null)[0].Sql,
				() => ((IExpressionQuery)this).GetSqlQueries(new () { InlineParameters = true })[0].Sql
				);

		#endregion

		#region Execute

		Query<T> GetQuery(ref IQueryExpressions expression, bool cache, out bool dependsOnParameters)
		{
			dependsOnParameters = false;

			if (cache && Info != null)
			{
				return Info;
			}

			var info = Query<T>.GetQuery(DataContext, ref expression, out dependsOnParameters);

			if (cache && info.CompareInfo?.IsFastComparable == true && !dependsOnParameters)
			{
				Info = info;
			}

			return info;
		}

		async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, false, out _);

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			Preambles = await query.InitPreamblesAsync(DataContext, expressions, Parameters, cancellationToken)
				.ConfigureAwait(false);

			var value = await query.GetElementAsync(DataContext, expressions, Parameters, Preambles, cancellationToken)
				.ConfigureAwait(false);

			return (TResult)value!;
		}

		IDisposable? StartLoadTransaction(Query query)
		{
			// Do not start implicit transaction if there is no preambles
			//
			if (!query.IsAnyPreambles())
				return null;

			var dc = DataContext switch
			{
				DataConnection dataConnection => dataConnection,
				DataContext    dataContext    => dataContext.GetDataConnection(),
				_                             => null
			};

			if (dc == null)
				return null;

			// transaction will be maintained by TransactionScope
			//
			if (TransactionScopeHelper.IsInsideTransactionScope)
				return null;

			dc.EnsureConnection();

			if (dc.TransactionAsync != null || dc.CurrentCommand?.Transaction != null)
				return null;

			if (DataContext is DataContext ctx)
				return ctx!.BeginTransaction(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel);

			return dc!.BeginTransaction(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		async Task<IAsyncDisposable?> StartLoadTransactionAsync(Query query, CancellationToken cancellationToken)
		{
			// Do not start implicit transaction if there is no preambles
			//
			if (!query.IsAnyPreambles())
				return null;

			var dc = DataContext switch
			{
				DataConnection dataConnection => dataConnection,
				DataContext    dataContext    => dataContext.GetDataConnection(),
				_                             => null
			};

			if (dc == null)
				return null;

			// transaction will be maintained by TransactionScope
			//
			if (TransactionScopeHelper.IsInsideTransactionScope)
				return null;

			await dc.EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

			if (dc.TransactionAsync != null || dc.CurrentCommand?.Transaction != null)
				return null;

			if (DataContext is DataContext ctx)
				return await ctx.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken)!
					.ConfigureAwait(false);

			return await dc.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken)!
				.ConfigureAwait(false);
		}

		async Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, false, out _);

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			Preambles = await query.InitPreamblesAsync(DataContext, expressions, Parameters, cancellationToken)
				.ConfigureAwait(false);

			return Query<TResult>.GetQuery(DataContext, ref expressions, out _)
				.GetResultEnumerable(DataContext, expressions, Parameters, Preambles);
		}

		public async Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			var expression  = Expression;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var _ = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			Preambles = await query.InitPreamblesAsync(DataContext, expressions, Parameters, cancellationToken)
				.ConfigureAwait(false);

			var enumerable = (IAsyncEnumerable<T>)query.GetResultEnumerable(DataContext, expressions, Parameters, Preambles);

#pragma warning disable CA2007
			await using var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
#pragma warning restore CA2007

			while (await enumerator.MoveNextAsync().ConfigureAwait(false))
			{
				action(enumerator.Current);
			}
		}

		public async Task GetForEachUntilAsync(Func<T,bool> func, CancellationToken cancellationToken)
		{
			var expression  = Expression;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			var enumerable = (IAsyncEnumerable<T>)query.GetResultEnumerable(DataContext, expressions, Parameters, Preambles);
			var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);

			while (await enumerator.MoveNextAsync().ConfigureAwait(false))
			{
				if (func(enumerator.Current))
					break;
			}
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			var expression  = Expression;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			return new AsyncEnumeratorAsyncWrapper<T>(async () =>
			{
				var tr = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
				try
				{
					Preambles = await query.InitPreamblesAsync(DataContext, expressions, Parameters, cancellationToken)
						.ConfigureAwait(false);
					return Tuple.Create(
						query.GetResultEnumerable(DataContext, expressions, Parameters, Preambles)
						.GetAsyncEnumerator(cancellationToken), tr);
				}
				catch
				{
					if (tr != null)
						await tr.DisposeAsync().ConfigureAwait(false);
					throw;
				}
			});
		}

		#endregion

		#region IQueryable Members

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Type           IQueryable.ElementType => typeof(T);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Expression     IQueryable.Expression  => Expression;
		
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IQueryProvider IQueryable.Provider    => this;

		#endregion

		#region IQueryProvider Members

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			return new ExpressionQueryImpl<TElement>(DataContext, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			return ActivatorExt.CreateInstance<IQueryable>(
				typeof(ExpressionQueryImpl<>).MakeGenericType(elementType),
				DataContext,
				expression
			);
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			using var m = ActivityService.Start(ActivityID.QueryProviderExecuteT);

			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, false, out _);

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expressions, Parameters);

				var getElement = query.GetElement;
				if (getElement == null)
					throw new LinqToDBException("GetElement is not assigned by the context.");
				return (TResult)getElement(DataContext, expressions, Parameters, Preambles)!;
			}
		}

		object? IQueryProvider.Execute(Expression expression)
		{
			using var m = ActivityService.Start(ActivityID.QueryProviderExecute);

			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, false, out _);

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expressions, Parameters);

				var getElement = query.GetElement;
				if (getElement == null)
					throw new LinqToDBException("GetElement is not assigned by the context.");
				return getElement(DataContext, expressions, Parameters, Preambles);
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumeratorT);

			var expression  = Expression;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expressions, Parameters);

				return query.GetResultEnumerable(DataContext, expressions, Parameters, Preambles).GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumerator);

			var expression  = Expression;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expressions, Parameters);

				return query.GetResultEnumerable(DataContext, expressions, Parameters, Preambles).GetEnumerator();
			}
		}

		#endregion
	}
}
