using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metrics;

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

		// What Info was found for (see GetInfoKey): Info is reused only while the context still matches it.
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		(int configurationID, QueryFlags queryFlags, int interceptorsGeneration)? _infoKey;

		// Set by a compiled table on the IQueryable it returns: its Info is re-resolved through the compiled query's
		// cache on every execution, instead of being kept.
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal Func<IDataContext, object?[], (Query<T> query, IQueryExpressions expressions)>? CompiledResolver;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal object?[]? Parameters;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal SqlCommandExecutionContext? Preambles;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal IQueryExpressions? CompiledExpressions;

		// A compiled table hands over the container its Info's accessors were built against. Rebuilding one from
		// the main expression alone carries no dynamic entries, and an accessor rooted at one - which a query
		// filter closing over a value produces - then has nothing to resolve. Only for enumerating this query as
		// it is: a composed expression is a different tree and gets its own container.
		IQueryExpressions GetOwnExpressions(Expression expression)
		{
			return CompiledExpressions ?? new RuntimeExpressionsContainer(expression);
		}

		#endregion

		#region Public Members

		IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options)
		{
			var oldInline = DataContext.InlineParameters;

			if (options?.InlineParameters != null)
				DataContext.InlineParameters = options.InlineParameters.Value;

			var expression  = Expression;
			var expressions = GetOwnExpressions(expression);
			var info        = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (options?.MultiInsertMode != null && info.QueryInfo.Statement is SqlMultiInsertStatement multiInsert)
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
				() => new Expressions.ExpressionPrinter().PrintExpression(Expression),
				() => ((IExpressionQuery)this).GetSqlQueries(null)[0].Sql,
				() => ((IExpressionQuery)this).GetSqlQueries(new () { InlineParameters = true })[0].Sql
				);

		#endregion

		#region Execute

		Query<T> GetQuery(ref IQueryExpressions expression, bool cache, out bool dependsOnParameters)
		{
			dependsOnParameters = false;

			if (cache && CompiledResolver != null)
			{
				var (query, compiled) = CompiledResolver(DataContext, Parameters!);

				Info                = query;
				CompiledExpressions = compiled;
				expression          = compiled;

				return query;
			}

			if (cache && Info != null)
			{
				if (_infoKey != null && _infoKey.Value == GetInfoKey(DataContext))
					return Info;

				Info     = null;
				_infoKey = null;
			}

			var info = Query<T>.GetQuery(DataContext, ref expression, out dependsOnParameters);

			// Only a query that the cache would accept without re-evaluating anything is kept (no dynamic accessors or
			// value comparisons, no part of the tree rewritten by expose or by an interceptor).
			if (cache && info.CompareInfo?.IsFastComparable == true && !dependsOnParameters && GetInfoKey(DataContext) is { } key)
			{
				Info     = info;
				_infoKey = key;
			}

			return info;
		}

		// A kept query stays valid while the context's configuration id, query flags and registered interceptor instances are
		// unchanged: the id covers mapping schema, options and interceptor types, the generation counts every
		// AddInterceptor / RemoveInterceptor (so another instance of the same type is noticed). A context that cannot report
		// such a generation keeps nothing once it has a query interceptor.
		static (int configurationID, QueryFlags queryFlags, int interceptorsGeneration)? GetInfoKey(IDataContext dataContext)
		{
			int generation;

			if (dataContext is IInterceptorsGeneration interceptors)
				generation = interceptors.InterceptorsGeneration;
			else if (dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: not null })
				return null;
			else
				generation = 0;

			return (dataContext.ConfigurationID, dataContext.GetQueryFlags(), generation);
		}

		async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, false, out _);

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			if (query.GetEagerElementAsync != null)
			{
				var eagerValue = await query.GetEagerElementAsync(DataContext, expressions, Parameters, cancellationToken)
					.ConfigureAwait(false);

				if (transaction != null)
					await transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);

				return (TResult)eagerValue!;
			}

			Preambles = await query.InitHarvestersAsync(DataContext, expressions, Parameters, cancellationToken)
				.ConfigureAwait(false);

			var value = await query.GetElementAsync(DataContext, expressions, Preambles, cancellationToken)
				.ConfigureAwait(false);

			if (transaction != null)
				await transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);

			return (TResult)value!;
		}

		Query.LoadTransaction? StartLoadTransaction(Query query)
		{
			return query.StartLoadTransaction(DataContext);
		}

		Task<Query.LoadTransaction?> StartLoadTransactionAsync(Query query, CancellationToken cancellationToken)
		{
			return query.StartLoadTransactionAsync(DataContext, cancellationToken);
		}

		async Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, false, out _);

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			Preambles = await query.InitHarvestersAsync(DataContext, expressions, Parameters, cancellationToken)
				.ConfigureAwait(false);

			var enumerable = Query<TResult>.GetQuery(DataContext, ref expressions, out _)
				.GetResultEnumerable(DataContext, expressions, Preambles);

			if (transaction == null)
				return enumerable;

			var rows = await BufferedAsyncEnumerable<TResult>.CreateAsync(enumerable, cancellationToken).ConfigureAwait(false);

			await transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);

			return rows;
		}

		public Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			return GetForEachCoreAsync(x => { action(x); return true; }, cancellationToken);
		}

		public Task GetForEachUntilAsync(Func<T,bool> func, CancellationToken cancellationToken)
		{
			return GetForEachCoreAsync(func, cancellationToken);
		}

		async Task GetForEachCoreAsync(Func<T,bool> func, CancellationToken cancellationToken)
		{
			var expression  = Expression;
			var expressions = GetOwnExpressions(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
			await using var _ = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			var (enumerable, harvesters, combined) = await query.GetEagerEnumerableAsync(DataContext, expressions, Parameters, cancellationToken).ConfigureAwait(false);

			if (!combined)
				Preambles = harvesters;

			IAsyncEnumerable<T> source = enumerable;

			if (transaction != null)
			{
				source = await BufferedAsyncEnumerable<T>.CreateAsync(enumerable, cancellationToken).ConfigureAwait(false);

				await transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);
			}

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(false))
			{
				while (await enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					if (!func(enumerator.Current))
						break;
				}
			}
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			var expression  = Expression;
			var expressions = GetOwnExpressions(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			return new AsyncEnumeratorAsyncWrapper<T>(async () =>
			{
				var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(false);
				await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

				var (enumerable, harvesters, combined) = await query.GetEagerEnumerableAsync(DataContext, expressions, Parameters, cancellationToken).ConfigureAwait(false);

				if (!combined)
					Preambles = harvesters;

				if (transaction == null)
					return enumerable.GetAsyncEnumerator(cancellationToken);

				var rows = await BufferedAsyncEnumerable<T>.CreateAsync(enumerable, cancellationToken).ConfigureAwait(false);

				await transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);

				return rows.GetAsyncEnumerator(cancellationToken);
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
			ArgumentNullException.ThrowIfNull(expression);

			return new ExpressionQueryImpl<TElement>(DataContext, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			ArgumentNullException.ThrowIfNull(expression);

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

			using var transaction = StartLoadTransaction(query);

			if (query.GetEagerElement != null)
			{
				var eagerValue = query.GetEagerElement(DataContext, expressions, Parameters);

				transaction?.Complete();

				return (TResult)eagerValue!;
			}

			Preambles = query.InitHarvesters(DataContext, expressions, Parameters);

			var getElement = query.GetElement;
			if (getElement == null)
				throw new LinqToDBException("GetElement is not assigned by the context.");

			var value = getElement(DataContext, expressions, Preambles);

			transaction?.Complete();

			return (TResult)value!;
		}

		object? IQueryProvider.Execute(Expression expression)
		{
			using var m = ActivityService.Start(ActivityID.QueryProviderExecute);

			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(expression);
			var query       = GetQuery(ref expressions, false, out _);

			using var transaction = StartLoadTransaction(query);

			if (query.GetEagerElement != null)
			{
				var eagerValue = query.GetEagerElement(DataContext, expressions, Parameters);

				transaction?.Complete();

				return eagerValue;
			}

			Preambles = query.InitHarvesters(DataContext, expressions, Parameters);

			var getElement = query.GetElement;
			if (getElement == null)
				throw new LinqToDBException("GetElement is not assigned by the context.");

			var value = getElement(DataContext, expressions, Preambles);

			transaction?.Complete();

			return value;
		}

		#endregion

		#region IEnumerable Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumeratorT);

			return GetEagerEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumerator);

			return GetEagerEnumerator();
		}

		IEnumerator<T> GetEagerEnumerator()
		{
			var expression  = Expression;
			var expressions = GetOwnExpressions(expression);
			var query       = GetQuery(ref expressions, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expressions.MainExpression;

			using var transaction = StartLoadTransaction(query);

			var (enumerable, harvesters, combined) = query.GetEagerEnumerable(DataContext, expressions, Parameters);

			if (!combined)
				Preambles = harvesters;

			if (transaction == null)
				return enumerable.GetEnumerator();

			var rows = BufferedRows<T>.Read(enumerable);

			transaction.Complete();

			return rows;
		}

		#endregion
	}
}
