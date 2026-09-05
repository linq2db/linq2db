using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq.Builder;

namespace LinqToDB.Internal.Linq
{
	sealed class CompiledTable<T>
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

		Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues)
		{
			for (;;)
			{
				var indexes = _dependentArgumentIndexes;
				var query   = GetInfo(dataContext, parameterValues, indexes, out var unseenSlots);

				if (unseenSlots == null)
					return query;

				lock (_learnLock)
					_dependentArgumentIndexes = Union(_dependentArgumentIndexes, unseenSlots);
			}
		}

		Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues, int[] indexes, out int[]? unseenSlots)
		{
			var configurationID = dataContext.ConfigurationID;
			var dataOptions     = dataContext.Options;
			var reported        = new List<int[]>(1);

			var cacheKey =
				(
					operation: "CT",
					configurationID,
					// Identity of this fold site, not a structural comparison: the cache is global per T,
					// and one compiled query can fold more than one table of the same type.
					table      : this,
					queryFlags : dataContext.GetQueryFlags(),
					dependent  : GetDependentArgumentValues(indexes, parameterValues)
				);

			var result = QueryRunner.Cache<T>.QueryCache.GetOrCreate(
				cacheKey,
				(dataContext, dataOptions, parameterValues, reported),
				static (o, key, ctx) =>
				{
					o.SlidingExpiration = ctx.dataOptions.LinqOptions.CacheSlidingExpirationOrDefault;

					var optimizationContext = new ExpressionTreeOptimizationContext(ctx.dataContext);
					var exposed = ExpressionBuilder.ExposeExpression(key.table._expression, ctx.dataContext,
						optimizationContext, ctx.parameterValues, optimizeConditions : false, compactBinary : true,
						out var materializedSlots);

					ctx.reported.Add(materializedSlots);

					var query             = new Query<T>(ctx.dataContext);
					var expressions       = (IQueryExpressions)new RuntimeExpressionsContainer(exposed);
					var parametersContext = new ParametersContext(expressions, optimizationContext, ctx.dataContext);

					var validateSubqueries = !ExpressionBuilder.NeedsSubqueryValidation(ctx.dataContext);
					query = new ExpressionBuilder(query, validateSubqueries, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.parameterValues)
						.Build<T>(ref expressions);

					if (query.ErrorExpression != null)
					{
						if (!validateSubqueries)
						{
							query = new Query<T>(ctx.dataContext);

							query = new ExpressionBuilder(query, true, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.parameterValues)
								.Build<T>(ref expressions);
						}

						if (query.ErrorExpression != null)
							throw query.ErrorExpression.CreateException();
					}

					query.CompiledExpressions = expressions;

					return query;
				})!;

			if (reported.Count == 0 || IsCovered(reported[0], indexes))
			{
				unseenSlots = null;

				return result;
			}

			// A slot the pre-expose scan could not see took part in this build, so the entry just made is keyed
			// too weakly to tell two argument values apart. Drop it and report the slot so the caller can retry.
			QueryRunner.Cache<T>.QueryCache.Remove(cacheKey);

			unseenSlots = reported[0];

			return result;
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
			var query = GetInfo(db, parameters);

			// The exposed tree, not _expression: Info carries parameter accessors built against it, and a
			// pre-set Info makes ExpressionQuery skip the expose that would otherwise reconcile the two.
			return new Table<T>(db, query.CompiledExpressions!.MainExpression) { Info = query, Parameters = parameters };
		}

		public T Execute(object[] parameters)
		{
			var db          = (IDataContext)parameters[0];
			var query       = GetInfo(db, parameters);
			var expressions = query.CompiledExpressions!;

			using (query.StartLoadTransaction(db))
			{
				var preambles = query.InitPreambles(db, expressions, parameters);

				return (T)query.GetElement(db, expressions, parameters, preambles)!;
			}
		}

		public async Task<T> ExecuteAsync(object[] parameters, CancellationToken cancellationToken)
		{
			var db          = (IDataContext)parameters[0];
			var query       = GetInfo(db, parameters);
			var expressions = query.CompiledExpressions!;

			var transaction = await query.StartLoadTransactionAsync(db, cancellationToken).ConfigureAwait(false);
			await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false);

			var preambles = await query.InitPreamblesAsync(db, expressions, parameters, cancellationToken).ConfigureAwait(false);

			return (T)(await query.GetElementAsync(db, expressions, parameters, preambles, cancellationToken).ConfigureAwait(false))!;
		}
	}
}
