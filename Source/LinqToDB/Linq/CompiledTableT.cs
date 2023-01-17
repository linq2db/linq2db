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

		Query<T> GetInfo(IDataContext dataContext)
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
				(dataContext, lambda: _lambda, dataOptions),
				static (o, key, ctx) =>
				{
					o.SlidingExpiration = ctx.dataOptions.LinqOptions.CacheSlidingExpirationOrDefault;

					var query = new Query<T>(ctx.dataContext, key.expression);

					var optimizationContext = new ExpressionTreeOptimizationContext(ctx.dataContext);
					var parametersContext = new ParametersContext(key.expression, optimizationContext, ctx.dataContext);

					query = new ExpressionBuilder(query, optimizationContext, parametersContext, ctx.dataContext, key.expression, ctx.lambda.Parameters.ToArray())
						.Build<T>();

					query.ClearMemberQueryableInfo();
					return query;
				})!;


			return result;
		}

		public IQueryable<T> Create(object[] parameters, object[] preambles)
		{
			var db = (IDataContext)parameters[0];
			return new Table<T>(db, _expression) { Info = GetInfo(db), Parameters = parameters };
		}

		public T Execute(object[] parameters, object[] preambles)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db);

			return (T)query.GetElement(db, _expression, parameters, preambles)!;
		}

		public async Task<T> ExecuteAsync(object[] parameters, object[] preambles)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db);

			return (T)(await query.GetElementAsync(db, _expression, parameters, preambles, default).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))!;
		}
	}
}
