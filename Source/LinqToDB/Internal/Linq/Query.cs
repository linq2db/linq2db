using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public abstract class Query
	{
		internal Func<IDataContext,IQueryExpressions,SqlCommandExecutionContext?,object?>                         GetElement      = null!;
		internal Func<IDataContext,IQueryExpressions,SqlCommandExecutionContext?,CancellationToken,Task<object?>> GetElementAsync = null!;

		// Eager-aware element selection (First/Single/…), set only by FirstSingleBuilder. Unlike GetElement, these route
		// through the combined eager engine (Query<T>.GetEagerEnumerable — one multi-result-set command when eligible,
		// else the sequential harvesters + base enumerable) and take the single element, so First/Single batch their
		// eager children the same way ToList()/foreach do. When set, the element execution paths use these INSTEAD of
		// GetElement and skip the InitHarvesters pre-init (GetEagerEnumerable runs the harvesters itself). Null for
		// scalar / ExecuteElement / non-query queries, which keep the pre-inited-harvesters GetElement path.
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?>?                         GetEagerElement;
		internal Func<IDataContext,IQueryExpressions,object?[]?,CancellationToken,Task<object?>>? GetEagerElementAsync;

		#region Init

		// One logical query per Query. Multi-statement expansion is modelled by SqlCommandScenario at render time
		// (DataConnection.QueryRunner.GetCommand), not by a list here. Set in Init.
		internal QueryInfo QueryInfo = null!;

		public IReadOnlyCollection<QueryInfo> GetQueries()    => new[] { QueryInfo };
		public bool                           IsFinalized     { get; internal set; }
		public SqlErrorExpression?            ErrorExpression { get; internal set; }

		internal abstract void Init(IBuildContext parseContext);

		protected internal Query(IDataContext dataContext)
		{
			MappingSchema    = dataContext.MappingSchema;
			SqlOptimizer     = dataContext.GetSqlOptimizer(dataContext.Options);
			SqlProviderFlags = dataContext.SqlProviderFlags;
			DataOptions      = dataContext.Options;
			ConfigurationID  = dataContext.ConfigurationID;
		}

		#endregion

		#region Compare

		// ConfigurationID, ContextType, InlineParameters and IsEntityServiceProvided are pinned
		// by the QueryCache bucket key (ContextType + ConfigurationID + QueryFlags — the latter
		// encodes InlineParameters and HasEntityServiceInterceptor), so every entry inside a
		// bucket already matches them by construction. Compare only re-checks the expression.
		internal readonly MappingSchema    MappingSchema;
		internal readonly ISqlOptimizer    SqlOptimizer;
		internal readonly SqlProviderFlags SqlProviderFlags;
		internal readonly DataOptions      DataOptions;
		/// <summary>
		/// Configuration ID of the context the query was built for.
		/// </summary>
		internal readonly int              ConfigurationID;

		protected internal bool Compare(IDataContext dataContext, IQueryExpressions expressions, [NotNullWhen(true)] out IQueryExpressions? matchedQueryExpressions)
		{
			var dynamicAccessorResults = new QueryCacheCompareInfo.DynamicAccessorResults(memoize: false);

			return Compare(dataContext, expressions, ref dynamicAccessorResults, out matchedQueryExpressions);
		}

		/// <param name="dynamicAccessorResults">The dynamic accessors already evaluated in the lookup this comparison is part of.</param>
		internal bool Compare(IDataContext dataContext, IQueryExpressions expressions, ref QueryCacheCompareInfo.DynamicAccessorResults dynamicAccessorResults, [NotNullWhen(true)] out IQueryExpressions? matchedQueryExpressions)
		{
			matchedQueryExpressions = null;

			if (CompareInfo == null)
				return false;

			var result = CompareInfo.MainExpression.EqualsTo(expressions.MainExpression);

			if (!result)
				return false;

			return Revalidate(dataContext, expressions.MainExpression, compiledParameters: null, ref dynamicAccessorResults, out matchedQueryExpressions);
		}

		/// <summary>
		/// The part of <see cref="Compare(IDataContext, IQueryExpressions, out IQueryExpressions)"/> that does not follow
		/// from the main expression: re-evaluates the dynamic expression accessors for <paramref name="dataContext"/> and
		/// runs the value comparisons. On success returns the expressions to run the query with.
		/// </summary>
		/// <param name="compiledParameters">Arguments of a compiled query; <see langword="null"/> otherwise.</param>
		/// <param name="dynamicAccessorResults">The dynamic accessors already evaluated in the lookup this check is part of.</param>
		internal bool Revalidate(IDataContext dataContext, Expression mainExpression, object?[]? compiledParameters, ref QueryCacheCompareInfo.DynamicAccessorResults dynamicAccessorResults, [NotNullWhen(true)] out IQueryExpressions? matchedQueryExpressions)
		{
			bool result;

			var runtimeExpressions = new RuntimeExpressionsContainer(mainExpression);
			matchedQueryExpressions = runtimeExpressions;

			if (CompareInfo!.DynamicAccessors != null)
			{
				List<(int, Expression)>? testedExpressions = null;

				foreach (var da in CompareInfo.DynamicAccessors)
				{
					var current = dynamicAccessorResults.Get(dataContext, da);
					result = da.Used.EqualsTo(current);
					if (!result)
						return false;

					testedExpressions ??= new List<(int, Expression)>();
					testedExpressions.Add((da.ExpressionId, current));
				}

				if (testedExpressions != null)
				{
					foreach (var (expressionId, expression) in testedExpressions)
					{
						runtimeExpressions.AddExpression(expressionId, expression);
					}
				}
			}

			if (CompareInfo.ComparisionFunctions != null)
			{
				foreach (var compare in CompareInfo.ComparisionFunctions)
				{
					if (!compare(matchedQueryExpressions, dataContext, compiledParameters))
						return false;
				}
			}

			return DependentValuesMatch(dataContext, matchedQueryExpressions, compiledParameters);
		}

		/// <summary>
		/// Checks that the <see cref="SqlQueryDependentAttribute"/> arguments the main expression does not compare still have
		/// the values the query was built for.
		/// </summary>
		/// <param name="compiledParameters">Arguments of a compiled query; <see langword="null"/> otherwise.</param>
		internal bool DependentValuesMatch(IDataContext dataContext, IQueryExpressions expressions, object?[]? compiledParameters)
		{
			if (CompareInfo?.DependentValueChecks != null)
			{
				foreach (var check in CompareInfo.DependentValueChecks)
				{
					if (!check.Attribute.ObjectsEqual(check.Value, check.Current(expressions, dataContext, compiledParameters)))
						return false;
				}
			}

			return true;
		}

		internal QueryCacheCompareInfo? CompareInfo;

		internal List<ParameterAccessor>? ParameterAccessors { get; set; }

		internal List<SqlParameter>? BuiltParameters;

		internal void AddParameterAccessor(ParameterAccessor accessor)
		{
			(ParameterAccessors ??= []).Add(accessor);
		}

		internal void SetParametersAccessors(List<ParameterAccessor>? parameterAccessors)
		{
			ParameterAccessors = parameterAccessors;
		}

		#endregion

		#region Cache Support

		internal static readonly ConcurrentQueue<Action> CacheCleaners = new ();

		/// <summary>
		/// Clears query caches for all typed queries.
		/// </summary>
		public static void ClearCaches()
		{
			foreach (var cleaner in CacheCleaners)
			{
				cleaner();
			}
		}

		#endregion

		#region Eager Loading

		Harvester[]? _harvesters;

		internal void SetHarvesters(List<Harvester>? harvesters)
		{
			_harvesters = harvesters?.ToArray();
		}

		internal bool IsAnyHarvesters()
		{
			if (_harvesters == null || _harvesters.Length == 0)
				return false;

			for (var i = 0; i < _harvesters.Length; i++)
			{
				if (!_harvesters[i].IsInlined)
					return true;
			}

			return false;
		}

		// Exposes the harvester array to QueryRunner.TryGetCombinedEagerEnumerable so the main query and its combinable
		// eager-load children can be assembled into one multi-result-set command.
		internal Harvester[]? HarvestersArray => _harvesters;

		internal SqlCommandExecutionContext? InitHarvesters(IDataContext dc, IQueryExpressions expressions, object?[]? ps)
		{
			// A query with no harvesters still needs a context when it carries compiled-query parameters (ps != null), so the
			// mapper/parameter accessors can read them from the context. Regular queries pass ps == null here and stay context-free.
			if (_harvesters == null)
				return ps is null ? null : new SqlCommandExecutionContext(0, ps);

			var context = new SqlCommandExecutionContext(_harvesters.Length, ps);
			for (var i = 0; i < _harvesters.Length; i++)
			{
				context.SetResult(i, _harvesters[i].Execute(dc, expressions, context));
			}

			return context;
		}

		internal async Task<SqlCommandExecutionContext?> InitHarvestersAsync(IDataContext dc, IQueryExpressions expressions, object?[]? ps, CancellationToken cancellationToken)
		{
			if (_harvesters == null)
				return ps is null ? null : new SqlCommandExecutionContext(0, ps);

			var context = new SqlCommandExecutionContext(_harvesters.Length, ps);
			for (var i = 0; i < _harvesters.Length; i++)
			{
				context.SetResult(i, await _harvesters[i].ExecuteAsync(dc, expressions, context, cancellationToken).ConfigureAwait(false));
			}

			return context;
		}

		internal LoadTransaction? StartLoadTransaction(IDataContext dataContext)
		{
			var dc = GetTransactionOwner(dataContext);

			if (dc == null)
				return null;

			if (!dataContext.Options.LinqOptions.UseEagerLoadingTransaction)
				return LoadTransaction.WithoutTransaction;

			var isolationLevel = GetLoadIsolationLevel(dataContext, dc);

			if (dataContext is DataContext ctx)
				return new LoadTransaction(ctx.BeginTransaction(isolationLevel));

			return new LoadTransaction(dc.BeginTransaction(isolationLevel));
		}

		internal async Task<LoadTransaction?> StartLoadTransactionAsync(IDataContext dataContext, CancellationToken cancellationToken)
		{
			var dc = GetTransactionOwner(dataContext);

			if (dc == null)
				return null;

			if (!dataContext.Options.LinqOptions.UseEagerLoadingTransaction)
				return LoadTransaction.WithoutTransaction;

			var isolationLevel = GetLoadIsolationLevel(dataContext, dc);

			if (dataContext is DataContext ctx)
				return new LoadTransaction(
					await ctx.BeginTransactionAsync(isolationLevel, cancellationToken)
						.ConfigureAwait(false));

			return new LoadTransaction(
				await dc.BeginTransactionAsync(isolationLevel, cancellationToken)
					.ConfigureAwait(false));
		}

		static IsolationLevel GetLoadIsolationLevel(IDataContext dataContext, DataConnection dc)
		{
			return dataContext.Options.LinqOptions.EagerLoadingIsolationLevel
				?? dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel;
		}

		/// <summary>
		/// The implicit transaction that keeps the reads of an eager-loading query consistent. It covers the reads only: an
		/// enumeration buffers the main rows and commits it with <see cref="Complete"/> before returning the first one, so the
		/// caller's code never runs inside a transaction it did not open. Disposing it uncommitted rolls it back.
		/// With <see cref="LinqOptions.UseEagerLoadingTransaction"/> off, <see cref="WithoutTransaction"/> stands for it: the
		/// main rows are buffered the same way, with no transaction around the reads.
		/// </summary>
		internal sealed class LoadTransaction : IDisposable, IAsyncDisposable
		{
			public static readonly LoadTransaction WithoutTransaction = new();

			readonly object? _transaction;

			// Once committed or disposed, the connection's current transaction may be another one, and disposing ours
			// again would dispose it.
			bool _released;

			LoadTransaction()
			{
			}

			public LoadTransaction(DataContextTransaction transaction)
			{
				_transaction = transaction;
			}

			public LoadTransaction(DataConnectionTransaction transaction)
			{
				_transaction = transaction;
			}

			public void Complete()
			{
				if (_released || _transaction == null)
					return;

				if (_transaction is DataContextTransaction contextTransaction)
					contextTransaction.CommitTransaction();
				else
					((DataConnectionTransaction)_transaction).Commit();

				_released = true;
			}

			public async Task CompleteAsync(CancellationToken cancellationToken)
			{
				if (_released || _transaction == null)
					return;

				if (_transaction is DataContextTransaction contextTransaction)
					await contextTransaction.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
				else
					await ((DataConnectionTransaction)_transaction).CommitAsync(cancellationToken).ConfigureAwait(false);

				_released = true;
			}

			public void Dispose()
			{
				if (_released || _transaction == null)
					return;

				_released = true;
				((IDisposable)_transaction).Dispose();
			}

			public ValueTask DisposeAsync()
			{
				if (_released || _transaction == null)
					return default;

				_released = true;
				return ((IAsyncDisposable)_transaction).DisposeAsync();
			}
		}

		DataConnection? GetTransactionOwner(IDataContext dataContext)
		{
			// Do not start implicit transaction if there is no harvesters
			//
			if (!IsAnyHarvesters())
				return null;

			var dc = dataContext switch
			{
				DataConnection dataConnection => dataConnection,
				DataContext    context        => context.GetDataConnection(),
				_                             => null,
			};

			if (dc == null)
				return null;

			// transaction will be maintained by TransactionScope
			//
			if (TransactionScopeHelper.IsInsideTransactionScope)
				return null;

			if (dc.TransactionAsync != null || dc.CurrentCommand?.Transaction != null)
				return null;

			return dc;
		}

		#endregion
	}
}
