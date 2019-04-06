﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;
	using Mapping;
	using Common.Internal.Cache;

	static partial class QueryRunner
	{
		public static class InsertOrReplace<T>
		{
			static Query<int> CreateQuery(IDataContext dataContext, EntityDescriptor descriptor, T obj, string tableName, string databaseName, string schemaName, Type type)
			{
				var fieldDic = new Dictionary<SqlField, ParameterAccessor>();
				var sqlTable = new SqlTable(dataContext.MappingSchema, type);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				var sqlQuery = new SelectQuery();

				ParameterAccessor param = null;

				var insertOrUpdateStatement = new SqlInsertOrUpdateStatement(sqlQuery);
				insertOrUpdateStatement.Insert.Into  = sqlTable;
				insertOrUpdateStatement.Update.Table = sqlTable;

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = insertOrUpdateStatement, } }
				};

				var supported = ei.SqlProviderFlags.IsInsertOrUpdateSupported && ei.SqlProviderFlags.CanCombineParameters;

				// Insert.
				//
				foreach (var field in sqlTable.Fields.Select(f => f.Value))
				{
					if (field.IsInsertable && !field.ColumnDescriptor.ShouldSkip(obj, descriptor, SkipModification.Insert))
					{
						if (!supported || !fieldDic.TryGetValue(field, out param))
						{
							param = GetParameter(type, dataContext, field);
							ei.Queries[0].Parameters.Add(param);

							if (supported)
								fieldDic.Add(field, param);
						}

						insertOrUpdateStatement.Insert.Items.Add(new SqlSetExpression(field, param.SqlParameter));
					}
					else if (field.IsIdentity)
					{
						throw new LinqException("InsertOrReplace method does not support identity field '{0}.{1}'.", sqlTable.Name, field.Name);
					}
				}

				// Update.
				//
				var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
				var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable && !f.ColumnDescriptor.ShouldSkip(obj, descriptor, SkipModification.Update))
				                     .Except(keys).ToList();

				if (keys.Count == 0)
					throw new LinqException("InsertOrReplace method requires the '{0}' table to have a primary key.", sqlTable.Name);

				var q =
				(
					from k in keys
					join i in insertOrUpdateStatement.Insert.Items on k equals i.Column
					select new { k, i }
				).ToList();

				var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

				if (missedKey != null)
					throw new LinqException("InsertOrReplace method requires the '{0}.{1}' field to be included in the insert setter.",
						sqlTable.Name,
						missedKey.Name);

				if (fields.Count == 0)
					throw new LinqException("There are no fields to update in the type '{0}'.", sqlTable.Name);

				foreach (var field in fields)
				{
					if (!supported || !fieldDic.TryGetValue(field, out param))
					{
						param = GetParameter(type, dataContext, field);
						ei.Queries[0].Parameters.Add(param);

						if (supported)
							fieldDic.Add(field, param = GetParameter(typeof(T), dataContext, field));
					}

					insertOrUpdateStatement.Update.Items.Add(new SqlSetExpression(field, param.SqlParameter));
				}

				insertOrUpdateStatement.Update.Keys.AddRange(q.Select(i => i.i));

				// Set the query.
				//
				if (ei.SqlProviderFlags.IsInsertOrUpdateSupported)
					SetNonQueryQuery(ei);
				else
					MakeAlternativeInsertOrUpdate(ei);

				return ei;
			}

			public static int Query(IDataContext dataContext, T obj, string tableName, string databaseName, string schema)
			{
				if (Equals(default(T), obj))
					return 0;

				var type             = GetType<T>(obj, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei               = Common.Configuration.Linq.DisableQueryCache
						|| entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert)
						|| entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update)
					? CreateQuery(dataContext, entityDescriptor, obj, tableName, databaseName, schema, type)
					: Cache<T>.QueryCache.GetOrCreate(
					new { Operation = "IR", dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schema, databaseName, type },
					o =>
					{
						o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
						return CreateQuery(dataContext, entityDescriptor, obj, tableName, databaseName, schema, type);
					});

				return ei == null ? 0 : (int)ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<int> QueryAsync(IDataContext dataContext, T obj, string tableName, string databaseName, string schema, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var type             = GetType<T>(obj, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei               = Common.Configuration.Linq.DisableQueryCache
						|| entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert)
						|| entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update)
					? CreateQuery(dataContext, entityDescriptor, obj, tableName, schema, databaseName, type)
					: Cache<T>.QueryCache.GetOrCreate(
					new { Operation = "IR", dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, databaseName, schema, type },
					o =>
					{
						o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
						return CreateQuery(dataContext, entityDescriptor, obj, tableName, schema, databaseName, type);
					});

				var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}
		}

		public static void MakeAlternativeInsertOrUpdate(Query query)
		{
			var dic = new Dictionary<ICloneableElement, ICloneableElement>();

			var firstStatement = (SqlInsertOrUpdateStatement)query.Queries[0].Statement;
			var cloned         = (SqlInsertOrUpdateStatement)firstStatement.Clone(dic, _ => true);

			var insertStatement = new SqlInsertStatement(cloned.SelectQuery) {Insert = cloned.Insert};
			insertStatement.SelectQuery.From.Tables.Clear();

			query.Queries.Add(new QueryInfo
			{
				Statement   = insertStatement,
				Parameters  = query.Queries[0].Parameters
					.Select(p => new ParameterAccessor
						(
							p.Expression,
							p.Accessor,
							p.DataTypeAccessor,
							p.DbTypeAccessor,
							p.SizeAccessor,
							dic.ContainsKey(p.SqlParameter) ? (SqlParameter)dic[p.SqlParameter] : null
						))
					.Where(p => p.SqlParameter != null)
					.ToList(),
			});

			var keys = firstStatement.Update.Keys;

			foreach (var key in keys)
				firstStatement.SelectQuery.Where.Expr(key.Column).Equal.Expr(key.Expression);

			//TODO! looks not working solution
			if (firstStatement.Update.Items.Count > 0)
			{
				query.Queries[0].Statement = new SqlUpdateStatement(firstStatement.SelectQuery) {Update = firstStatement.Update};
				SetNonQueryQuery2(query);
			}
			else
			{
				firstStatement.SelectQuery.Select.Columns.Clear();
				firstStatement.SelectQuery.Select.Columns.Add(new SqlColumn(firstStatement.SelectQuery, new SqlExpression("1")));
				query.Queries[0].Statement = new SqlSelectStatement(firstStatement.SelectQuery);
				SetQueryQuery2(query);
			}

			query.Queries.Add(new QueryInfo
			{
				Statement  = new SqlSelectStatement(firstStatement.SelectQuery),
				Parameters = query.Queries[0].Parameters.ToList(),
			});
		}
	}
}
