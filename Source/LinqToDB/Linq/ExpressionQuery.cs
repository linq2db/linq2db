#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	using Async;
	using Extensions;
	using Data;

	abstract class ExpressionQuery<T> : IExpressionQuery<T>
	{
		#region Init

		protected void Init([NotNull] IDataContext dataContext, Expression expression)
		{
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
			Expression  = expression  ?? Expression.Constant(this);
		}

		[NotNull] public Expression   Expression  { get; set; }
		[NotNull] public IDataContext DataContext { get; set; }

		internal Query<T> Info;
		internal object[] Parameters;
		internal object[] Preambles;

		#endregion

		#region Public Members

		// This property is helpful in Debug Mode.
		//
		[UsedImplicitly]
		// ReSharper disable once InconsistentNaming
		string _sqlText => SqlText;

		public string SqlText
		{
			get
			{
				var expression = Expression;
				var info       = GetQuery(ref expression, true);
				var sqlText    = QueryRunner.GetSqlText(info, DataContext, expression, Parameters, Preambles, 0);

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

			if (cache)
				Info = info;

			return info;
		}

		async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var query = GetQuery(ref expression, false);

			using (await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				Preambles = await query.InitPreamblesAsync(DataContext).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				var value = await query.GetElementAsync(DataContext, expression, Parameters, Preambles, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return (TResult)value;
			}
		}

		static readonly Task<DataConnectionTransaction> CompletedTransactionTask =
			Task.FromResult<DataConnectionTransaction>(null);

		DataConnectionTransaction StartLoadTransaction(Query query)
		{
			if (!query.IsAnyPreambles())
				return null;

			DataConnection dc = null;
			if (DataContext is DataConnection dataConnection)
				dc = dataConnection;
			else if (DataContext is DataContext dataContext)
				dc = dataContext.GetDataConnection();

			if (dc == null || dc.TransactionAsync != null)
				return null;

			return dc.BeginTransaction(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel);
		}

		Task<DataConnectionTransaction> StartLoadTransactionAsync(Query query, CancellationToken cancellationToken)
		{
			if (!query.IsAnyPreambles())
				return CompletedTransactionTask;

			DataConnection dc = null;
			if (DataContext is DataConnection dataConnection)
				dc = dataConnection;
			else if (DataContext is DataContext dataContext)
				dc = dataContext.GetDataConnection();

			if (dc == null || dc.TransactionAsync != null)
				return CompletedTransactionTask;

			return dc.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken);
		}

		IAsyncEnumerable<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression)
		{
			var query = GetQuery(ref expression, false);

			//TODO: need async call

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext);

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
				Preambles = await query.InitPreamblesAsync(DataContext).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
			return GetQuery(ref expression, true)
				.GetForEachAsync(DataContext, expression, Parameters, Preambles, func, cancellationToken);
		}

		public IAsyncEnumerable<T> GetAsyncEnumerable()
		{
			var expression = Expression;
			return GetQuery(ref expression, true).GetIAsyncEnumerable(DataContext, expression, Parameters, Preambles);
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
					DataContext, expression);
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			var query = GetQuery(ref expression, false);

			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext);

				return (TResult)query.GetElement(DataContext, expression, Parameters, Preambles);
			}
		}

		object IQueryProvider.Execute(Expression expression)
		{
			var query = GetQuery(ref expression, false);
			
			using (StartLoadTransaction(query))
			{
				Preambles = query.InitPreambles(DataContext);

				return query.GetElement(DataContext, expression, Parameters, Preambles);
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
				Preambles = query.InitPreambles(DataContext);

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
				Preambles = query.InitPreambles(DataContext);

				return query.GetIEnumerable(DataContext, Expression, Parameters, Preambles).GetEnumerator();
			}
		}

		#endregion

	}
}
