using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using Builder;
	using Mapping;

	class CompiledTable<T>
	{
		public CompiledTable(LambdaExpression lambda, Expression expression)
		{
			_lambda     = lambda;
			_expression = expression;
		}

		readonly LambdaExpression _lambda;
		readonly Expression       _expression;
		readonly object           _sync = new object();

		string        _lastContextID;
		MappingSchema _lastMappingSchema;
		Query<T>      _lastQuery;

		readonly Dictionary<object,Query<T>> _infos = new Dictionary<object, Query<T>>();

		Query<T> GetInfo(IDataContext dataContext)
		{
			string        lastContextID;
			MappingSchema lastMappingSchema;
			Query<T>      query;

			lock (_sync)
			{
				lastContextID     = _lastContextID;
				lastMappingSchema = _lastMappingSchema;
				query             = _lastQuery;
			}

			var contextID     = dataContext.ContextID;
			var mappingSchema = dataContext.MappingSchema;

			if (lastContextID != contextID || lastMappingSchema != mappingSchema)
				query = null;

			if (query == null)
			{
				var key = new { contextID, mappingSchema };

				lock (_sync)
					_infos.TryGetValue(key, out query);

				if (query == null)
				{
					lock (_sync)
					{
						_infos.TryGetValue(key, out query);

						if (query == null)
						{
							query = new Query<T>(dataContext, _expression);

							query = new ExpressionBuilder(query, dataContext, _expression, _lambda.Parameters.ToArray())
								.Build<T>();

							_infos.Add(key, query);

							_lastContextID     = contextID;
							_lastMappingSchema = mappingSchema;
							_lastQuery         = query;
						}
					}
				}
			}

			return query;
		}

		public IQueryable<T> Create(object[] parameters)
		{
			var db = (IDataContext)parameters[0];
			return new Table<T>(db, _expression) { Info = GetInfo(db), Parameters = parameters };
		}

		public T Execute(object[] parameters)
		{
			var db    = (IDataContextEx)parameters[0];
			var query = GetInfo(db);

			return (T)query.GetElement(db, _expression, parameters);
		}
	}
}
