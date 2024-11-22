using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Builder;
	using Common.Internal.Cache;

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

					var query             = new Query<T>(ctx.dataContext, exposed);
					var parametersContext = new ParametersContext(exposed, optimizationContext, ctx.dataContext);

					query = new ExpressionBuilder(query, false, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.parameterValues)
						.Build<T>();

					if (query.ErrorExpression != null)
					{
						query = new Query<T>(ctx.dataContext, exposed);

						query = new ExpressionBuilder(query, true, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.parameterValues)
							.Build<T>();

						if (query.ErrorExpression != null)
							throw query.ErrorExpression.CreateException();
					}


					query.ClearDynamicQueryableInfo();
					return query;
				})!;

			return result;
		}

		public IQueryable<T> Create(object[] parameters, object[] preambles)
		{
			var db = (IDataContext)parameters[0];
			return new Table<T>(db, _expression) { Info = GetInfo(db, parameters), Parameters = parameters };
		}

		public T Execute(object[] parameters, object[] preambles)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters);

			return (T)query.GetElement(db, _expression, parameters, preambles)!;
		}

		public async Task<T> ExecuteAsync(object[] parameters, object[] preambles)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters);

			return (T)(await query.GetElementAsync(db, _expression, parameters, preambles, default).ConfigureAwait(false))!;
		}
	}
}
