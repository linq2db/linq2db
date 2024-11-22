using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Async;
	using Data;
	using Extensions;
	using Tools;

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

		internal Query<T>? Info;
		internal object?[]? Parameters;
		internal object?[]? Preambles;

		#endregion

		#region Public Members

#if DEBUG
		// This property is helpful in Debug Mode.
		//
		[JetBrains.Annotations.UsedImplicitly]
		// ReSharper disable once InconsistentNaming
		public string _sqlText => SqlText;
#endif

		public string SqlText
		{
			get
			{
				var expression = Expression;
				var info       = GetQuery(ref expression, true, out var dependsOnParameters);

				if (!dependsOnParameters)
					Expression = expression;

				var sqlText    = QueryRunner.GetSqlText(info, DataContext, expression, Parameters, Preambles);

				return sqlText;
			}
		}

		#endregion

		#region Execute

		Query<T> GetQuery(ref Expression expression, bool cache, out bool dependsOnParameters)
		{
			dependsOnParameters = false;

			if (cache && Info != null)
				return Info;

			var info = Query<T>.GetQuery(DataContext, ref expression, out dependsOnParameters);

			if (cache && info.IsFastCacheable && !dependsOnParameters)
				Info = info;

			return info;
		}

		async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var query = GetQuery(ref expression, false, out _);

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken)
				.ConfigureAwait(false);

			var value = await query.GetElementAsync(DataContext, expression, Parameters, Preambles, cancellationToken)
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
				return await ctx!.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken)!
					.ConfigureAwait(false);

			return await dc!.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken)!
				.ConfigureAwait(false);
		}

		async Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var query = GetQuery(ref expression, false, out _);

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken)
				.ConfigureAwait(false);

			return Query<TResult>.GetQuery(DataContext, ref expression, out _)
				.GetResultEnumerable(DataContext, expression, Parameters, Preambles);
		}

		public async Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var _ = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken)
				.ConfigureAwait(false);

			var enumerable = (IAsyncEnumerable<T>)query.GetResultEnumerable(DataContext, expression, Parameters, Preambles);

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
			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			var enumerable = (IAsyncEnumerable<T>)query.GetResultEnumerable(DataContext, expression, Parameters, Preambles);
			var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);

			while (await enumerator.MoveNextAsync().ConfigureAwait(false))
			{
				if (func(enumerator.Current))
					break;
			}
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			return new AsyncEnumeratorAsyncWrapper<T>(async () =>
			{
				var tr = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
				try
				{
					Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken)
						.ConfigureAwait(false);
					return Tuple.Create(
						query.GetResultEnumerable(DataContext, expression, Parameters, Preambles)
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

		Type           IQueryable.ElementType => typeof(T);
		Expression     IQueryable.Expression  => Expression;
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

			try
			{
				return (IQueryable)Activator.CreateInstance(
					typeof(ExpressionQueryImpl<>).MakeGenericType(elementType),
					DataContext, expression)!;
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException ?? ex;
			}
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			using var m = ActivityService.Start(ActivityID.QueryProviderExecuteT);

			var query = GetQuery(ref expression, false, out _);

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expression, Parameters);

				var getElement = query.GetElement;
				if (getElement == null)
					throw new LinqToDBException("GetElement is not assigned by the context.");
				return (TResult)getElement(DataContext, expression, Parameters, Preambles)!;
			}
		}

		object? IQueryProvider.Execute(Expression expression)
		{
			using var m = ActivityService.Start(ActivityID.QueryProviderExecute);

			var query = GetQuery(ref expression, false, out _);

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expression, Parameters);

				var getElement = query.GetElement;
				if (getElement == null)
					throw new LinqToDBException("GetElement is not assigned by the context.");
				return getElement(DataContext, expression, Parameters, Preambles);
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumeratorT);

			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expression, Parameters);

				return query.GetResultEnumerable(DataContext, expression, Parameters, Preambles).GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumerator);

			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expression, Parameters);

				return query.GetResultEnumerable(DataContext, expression, Parameters, Preambles).GetEnumerator();
			}
		}

		#endregion
	}
}
