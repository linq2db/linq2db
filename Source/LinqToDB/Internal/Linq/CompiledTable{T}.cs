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
		readonly int[]      _dependentArgumentIndexes;

		// Expose materialises a [SqlQueryDependent] argument read from the argument array, so the cached query
		// belongs to those values and not just to this fold site.
		DependentArgumentValues GetDependentArgumentValues(object?[] parameterValues)
		{
			if (_dependentArgumentIndexes.Length == 0)
				return DependentArgumentValues.None;

			var values = new object?[_dependentArgumentIndexes.Length];

			for (var i = 0; i < values.Length; i++)
				values[i] = parameterValues[_dependentArgumentIndexes[i]];

			return new DependentArgumentValues(values);
		}

		Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues)
		{
			var configurationID = dataContext.ConfigurationID;
			var dataOptions     = dataContext.Options;

			var result = QueryRunner.Cache<T>.QueryCache.GetOrCreate(
				(
					operation: "CT",
					configurationID,
					// Identity of this fold site, not a structural comparison: the cache is global per T,
					// and one compiled query can fold more than one table of the same type.
					table      : this,
					queryFlags : dataContext.GetQueryFlags(),
					dependent  : GetDependentArgumentValues(parameterValues)
				),
				(dataContext, dataOptions, parameterValues),
				static (o, key, ctx) =>
				{
					o.SlidingExpiration = ctx.dataOptions.LinqOptions.CacheSlidingExpirationOrDefault;

					var optimizationContext = new ExpressionTreeOptimizationContext(ctx.dataContext);
					var exposed = ExpressionBuilder.ExposeExpression(key.table._expression, ctx.dataContext,
						optimizationContext, ctx.parameterValues, optimizeConditions : false, compactBinary : true);

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

			return result;
		}

		public IQueryable<T> Create(object[] parameters)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters);

			return new Table<T>(db, _expression) { Info = query, Parameters = parameters };
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
