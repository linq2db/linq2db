using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	using System.Diagnostics.CodeAnalysis;
	using Async;
	using Extensions;
	using Data;

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
		[UsedImplicitly]
		// ReSharper disable once InconsistentNaming
		public string _sqlText => SqlText;
#endif

		public string SqlText
		{
			get
			{
				var expression = Expression;
				var info       = GetQuery(ref expression, true);
				Expression     = expression;

				var sqlText    = QueryRunner.GetSqlText(info, DataContext, expression, Parameters, Preambles);

				return sqlText;
			}
		}

		#endregion

		#region Execute

		Query<T> GetQuery(ref Expression expression, bool cache)
		{
			if (cache && Info != null)
				return Info;

			var info = Query<T>.GetQuery(DataContext, ref expression);

			if (cache && info.IsFastCacheable)
				Info = info;

			return info;
		}

		async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var query = GetQuery(ref expression, false);

			using (await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				var value = await query.GetElementAsync(DataContext, expression, Parameters, Preambles, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return (TResult)value!;
			}
		}

		DataConnectionTransaction? StartLoadTransaction(Query query)
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
		
			return dc!.BeginTransaction(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		async Task<DataConnectionTransaction?> StartLoadTransactionAsync(Query query, CancellationToken cancellationToken)
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
		
			await dc.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (dc.TransactionAsync != null || dc.CurrentCommand?.Transaction != null)
				return null;
		
			return await dc!.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken)!
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		async Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var query = GetQuery(ref expression, false);

			using (await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return Query<TResult>.GetQuery(DataContext, ref expression)
					.GetIAsyncEnumerable(DataContext, expression, Parameters, Preambles);
			}
		}

		public async Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			using (await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				await query
					.GetForEachAsync(DataContext, Expression, Parameters, Preambles, r =>
					{
						action(r);
						return true;
					}, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}

		public Task GetForEachUntilAsync(Func<T,bool> func, CancellationToken cancellationToken)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			return query.GetForEachAsync(DataContext, expression, Parameters, Preambles, func, cancellationToken);
		}

		public IAsyncEnumerable<T> GetAsyncEnumerable()
		{
			return this;
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			return new AsyncEnumeratorAsyncWrapper<T>(async () =>
			{
				var tr = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				try
				{
					Preambles = await query.InitPreamblesAsync(DataContext, expression, Parameters, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#if NETFRAMEWORK
					return Tuple.Create<IAsyncEnumerator<T>, IDisposable?>(
#else
					return Tuple.Create<IAsyncEnumerator<T>, IAsyncDisposable?>(
#endif
						query.GetIAsyncEnumerable(DataContext, expression, Parameters, Preambles)
						.GetAsyncEnumerator(cancellationToken), tr);
				}
				catch
				{
					tr?.Dispose();
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

		[return: MaybeNull]
		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			var query = GetQuery(ref expression, false);

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
			var query = GetQuery(ref expression, false);
			
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
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expression, Parameters);

				return query.GetIEnumerable(DataContext, Expression, Parameters, Preambles).GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext, expression, Parameters);

				return query.GetIEnumerable(DataContext, Expression, Parameters, Preambles).GetEnumerator();
			}
		}

		#endregion

	}
}
