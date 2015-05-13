﻿using System;
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
		QueryOld<T>      _lastQuery;

		readonly Dictionary<object,QueryOld<T>> _infos = new Dictionary<object, QueryOld<T>>();

		QueryOld<T> GetInfo(IDataContext dataContext)
		{
			var dataContextInfo = DataContextInfo.Create(dataContext);

			string        lastContextID;
			MappingSchema lastMappingSchema;
			QueryOld<T>      query;

			lock (_sync)
			{
				lastContextID     = _lastContextID;
				lastMappingSchema = _lastMappingSchema;
				query             = _lastQuery;
			}

			var contextID     = dataContextInfo.ContextID;
			var mappingSchema = dataContextInfo.MappingSchema;

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
							query = new ExpressionBuilder(new QueryOld<T>(), dataContextInfo, _expression, _lambda.Parameters.ToArray())
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
			return new TableOld<T>(db, _expression) { Info = GetInfo(db), Parameters = parameters };
		}

		public T Execute(object[] parameters)
		{
			var db    = (IDataContext)parameters[0];
			var ctx   = DataContextInfo.Create(db);
			var query = GetInfo(db);

			return (T)query.GetElement(null, ctx, _expression, parameters);
		}
	}
}
