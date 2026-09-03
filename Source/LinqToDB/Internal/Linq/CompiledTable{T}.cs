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
		public CompiledTable(LambdaExpression lambda, Expression expression)
		{
			_lambda     = lambda;
			_expression = expression;
		}

		readonly LambdaExpression _lambda;
		readonly Expression       _expression;

		Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues)
		{
			var configurationID = dataContext.ConfigurationID;
			var dataOptions     = dataContext.Options;

			var result = QueryRunner.Cache<T>.QueryCache.GetOrCreate(
				(
					operation: "CT",
					configurationID,
					expression : _expression,
					queryFlags : dataContext.GetQueryFlags()
				),
				(dataContext, lambda: _lambda, dataOptions, parameterValues),
				static (o, key, ctx) =>
				{
					o.SlidingExpiration = ctx.dataOptions.LinqOptions.CacheSlidingExpirationOrDefault;

					var optimizationContext = new ExpressionTreeOptimizationContext(ctx.dataContext);
					var exposed = ExpressionBuilder.ExposeExpression(key.expression, ctx.dataContext,
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
