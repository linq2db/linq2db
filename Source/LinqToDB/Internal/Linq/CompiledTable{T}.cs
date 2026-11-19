using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.Linq
{
	sealed class CompiledTable<T> : ICompiledQuerySource<T>
		where T : notnull
	{
		public CompiledTable(Expression expression, int[] dependentArgumentIndexes)
		{
			_expression               = expression;
			_dependentArgumentIndexes = dependentArgumentIndexes;
		}

		readonly Expression _expression;

		// Grows when a build reports a slot the pre-expose scan could not see - expansion is recursive, so a
		// dependent position can appear only after several rewrites. Written under _learnLock, read unlocked.
		volatile int[] _dependentArgumentIndexes;

		readonly Lock _learnLock = new();

		// Expose materialises a [SqlQueryDependent] argument read from the argument array, so the cached query
		// belongs to those values and not just to this fold site.
		static DependentArgumentValues GetDependentArgumentValues(int[] indexes, object?[] parameterValues)
		{
			if (indexes.Length == 0)
				return DependentArgumentValues.None;

			var values = new object?[indexes.Length];

			for (var i = 0; i < values.Length; i++)
				values[i] = parameterValues[indexes[i]];

			return new DependentArgumentValues(values);
		}

		public bool HasDependentArguments => _dependentArgumentIndexes.Length != 0;

		public Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues, out IQueryExpressions expressions)
		{
			for (;;)
			{
				var indexes = _dependentArgumentIndexes;
				var query   = GetInfo(dataContext, parameterValues, indexes, out var unseenSlots, out expressions);

				if (unseenSlots == null)
					return query;

				lock (_learnLock)
					_dependentArgumentIndexes = Union(_dependentArgumentIndexes, unseenSlots);
			}
		}

		Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues, int[] indexes, out int[]? unseenSlots, out IQueryExpressions expressions)
		{
			var queryFlags = dataContext.GetQueryFlags();

			IStructuralEquatable cacheKey =
				(
					operation: "CT",
					configurationID: dataContext.ConfigurationID,
					// Identity of this fold site, not a structural comparison: the cache is global per T,
					// and one compiled query can fold more than one table of the same type.
					table      : this,
					queryFlags : queryFlags,
					dependent  : GetDependentArgumentValues(indexes, parameterValues)
				);

			QueryRunner.Cache<T>.QueryCache.TryGetValue(cacheKey, out var cached);

			if (cached != null)
			{
				if (TryReuse(cached, dataContext, parameterValues, out expressions))
				{
					unseenSlots = null;

					return cached;
				}

				// The executing context's query interceptor needs another query for this fold site. Such variants go to the
				// query cache, which keeps several for one key and compares them with the context.
				if (QueryCache.Default.TryFind(typeof(T), dataContext, cached.CompiledExpressions!, queryFlags, this, parameterValues, cacheKey, out var variant, out var matched))
				{
					expressions = matched;
					unseenSlots = null;

					return (Query<T>)variant;
				}
			}

			// Built outside the cache on purpose. Routing this through GetOrCreate publishes the entry before
			// its key can be checked against what the build materialised, and a thread hitting it in that window
			// would run this query for its own argument values.
			var query = BuildQuery(dataContext, parameterValues, out var materializedSlots);

			expressions = query.CompiledExpressions!;

			if (!IsCovered(materializedSlots, indexes))
			{
				// A slot the pre-expose scan could not see took part in the build, so this key cannot tell two
				// argument values apart. Nothing is published - the caller widens the key and comes back.
				unseenSlots = materializedSlots;

				return query;
			}

			if (cached != null)
			{
				query.CompiledCacheKey = cacheKey;

				QueryCache.Default.TryAdd(typeof(T), dataContext, query, expressions, queryFlags, this, parameterValues);
			}
			else
			{
				using var entry = QueryRunner.Cache<T>.QueryCache.CreateEntry(cacheKey);

				entry.SlidingExpiration = dataContext.Options.LinqOptions.CacheSlidingExpirationOrDefault;
				entry.Size              = 1;
				entry.Value             = query;
			}

			unseenSlots = null;

			return query;
		}

		// The key does not tell query interceptor instances apart: with a query interceptor, the dynamic accessors (a query
		// filter it rewrote) are re-evaluated for the executing context first, as the query cache does, and their results
		// supply the parameter values. Without one, the query is reused as it is.
		static bool TryReuse(Query<T> query, IDataContext dataContext, object?[] parameterValues, out IQueryExpressions expressions)
		{
			expressions = query.CompiledExpressions!;

			if (query.CompareInfo?.DynamicAccessors == null || dataContext is not IInterceptable<IQueryExpressionInterceptor> { Interceptor: not null })
				return true;

			if (!query.Revalidate(dataContext, expressions.MainExpression, parameterValues, out var matched))
				return false;

			expressions = matched;

			return true;
		}

		Query<T> BuildQuery(IDataContext dataContext, object?[] parameterValues, out int[] materializedSlots)
		{
			var optimizationContext = new ExpressionTreeOptimizationContext(dataContext);
			var exposed = ExpressionBuilder.ExposeExpression(_expression, dataContext,
				optimizationContext, parameterValues, optimizeConditions : false, compactBinary : true,
				out materializedSlots);

			var query             = new Query<T>(dataContext);
			var expressions       = (IQueryExpressions)new RuntimeExpressionsContainer(exposed);
			var parametersContext = new ParametersContext(expressions, optimizationContext, dataContext);

			var validateSubqueries = !ExpressionBuilder.NeedsSubqueryValidation(dataContext);
			query = new ExpressionBuilder(query, validateSubqueries, optimizationContext, parametersContext, dataContext, exposed, parameterValues)
				.Build<T>(ref expressions);

			if (query.ErrorExpression != null)
			{
				if (!validateSubqueries)
				{
					query = new Query<T>(dataContext);

					query = new ExpressionBuilder(query, true, optimizationContext, parametersContext, dataContext, exposed, parameterValues)
						.Build<T>(ref expressions);
				}

				if (query.ErrorExpression != null)
					throw query.ErrorExpression.CreateException();
			}

			query.CompiledExpressions = expressions;

			return query;
		}

		static bool IsCovered(int[] materializedSlots, int[] indexes)
		{
			foreach (var slot in materializedSlots)
				if (Array.IndexOf(indexes, slot) < 0)
					return false;

			return true;
		}

		static int[] Union(int[] indexes, int[] materializedSlots)
		{
			var union = new List<int>(indexes);

			foreach (var slot in materializedSlots)
				if (!union.Contains(slot))
					union.Add(slot);

			union.Sort();

			return union.ToArray();
		}

		public IQueryable<T> Create(object[] parameters)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters, out var expressions);

			// The exposed tree, not _expression: Info carries parameter accessors built against it, and a
			// pre-set Info makes ExpressionQuery skip the expose that would otherwise reconcile the two.
			return new Table<T>(db, expressions.MainExpression)
			{
				Info                = query,
				Parameters          = parameters,
				CompiledExpressions = expressions,
				CompiledSource      = this,
			};
		}

		public T Execute(object[] parameters)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters, out var expressions);

			using var transaction = query.StartLoadTransaction(db);

			// A compiled query always carries its argument array, so InitHarvesters returns a context even when the
			// query has no eager-load harvesters - the mapper and parameter accessors read the compiled args from it.
			var context = query.InitHarvesters(db, expressions, parameters);

			var value = (T)query.GetElement(db, expressions, context)!;

			transaction?.Complete();

			return value;
		}

		public async Task<T> ExecuteAsync(object[] parameters, CancellationToken cancellationToken)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters, out var expressions);

			var transaction = await query.StartLoadTransactionAsync(db, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			var context = await query.InitHarvestersAsync(db, expressions, parameters, cancellationToken).ConfigureAwait(false);

			var value = (T)(await query.GetElementAsync(db, expressions, context, cancellationToken).ConfigureAwait(false))!;

			if (transaction != null)
				await transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);

			return value;
		}
	}
}
