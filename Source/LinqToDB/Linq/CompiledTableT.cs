using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Builder;
	using Common.Internal.Cache;

	class CompiledTable<T>
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
			var contextID       = dataContext.ContextID;
			var contextType     = dataContext.GetType();
			var mappingSchemaID = dataContext.MappingSchema.ConfigurationID;

			var key = new { Operation = "CT", contextID, contextType, mappingSchemaID, Expression = _expression };

			var result = QueryRunner.Cache<T>.QueryCache.GetOrCreate(key, o =>
			{
				o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;

				var query = new Query<T>(dataContext, _expression);

				query = new ExpressionBuilder(query, dataContext, _expression, _lambda.Parameters.ToArray())
					.Build<T>();

				query.ClearMemberQueryableInfo();
				return query;
			});


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
