﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Common.Internal.Cache;
	using Mapping;
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class InsertOrReplace<T>
		{
			static Query<int> CreateQuery(
				IDataContext                   dataContext,
				EntityDescriptor               descriptor,
				T                              obj,
				InsertOrUpdateColumnFilter<T>? columnFilter,
				string?                        tableName,
				string?                        serverName,
				string?                        databaseName,
				string?                        schemaName,
				TableOptions                   tableOptions,
				Type                           type)
			{
				var fieldDic = new Dictionary<SqlField, ParameterAccessor>();
				var sqlTable = new SqlTable(descriptor);

				if (tableName != null || schemaName != null || databaseName != null || serverName != null)
				{
					sqlTable.TableName = new(
						          tableName    ?? sqlTable.TableName.Name,
						Server  : serverName   ?? sqlTable.TableName.Server,
						Database: databaseName ?? sqlTable.TableName.Database,
						Schema  : schemaName   ?? sqlTable.TableName.Schema);
				}

				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				var sqlQuery = new SelectQuery();

				ParameterAccessor? param;

				var insertOrUpdateStatement = new SqlInsertOrUpdateStatement(sqlQuery)
				{
					Insert = { Into  = sqlTable },
					Update = { Table = sqlTable }
				};

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = insertOrUpdateStatement, } }
				};

				var supported = ei.SqlProviderFlags.IsInsertOrUpdateSupported && ei.SqlProviderFlags.CanCombineParameters;

				// Insert.
				//
				foreach (var field in sqlTable.Fields)
				{
					if (field.IsInsertable && !field.ColumnDescriptor.ShouldSkip(obj!, descriptor, SkipModification.Insert))
					{
						if (columnFilter == null || columnFilter(obj, field.ColumnDescriptor, true))
						{
							if (!supported || !fieldDic.TryGetValue(field, out param))
							{
								param = GetParameter(type, dataContext, field);
								ei.Queries[0].AddParameterAccessor(param);

								if (supported)
									fieldDic.Add(field, param);
							}

							insertOrUpdateStatement.Insert.Items.Add(new SqlSetExpression(field, param.SqlParameter));
						}
					}
					else if (field.IsIdentity)
					{
						throw new LinqException("InsertOrReplace method does not support identity field '{0}.{1}'.", sqlTable.NameForLogging, field.Name);
					}
				}

				// Update.
				//
				var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
				var fields = sqlTable.Fields
					.Where(f => f.IsUpdatable && !f.ColumnDescriptor.ShouldSkip(obj!, descriptor, SkipModification.Update))
					.Except(keys);

				if (keys.Count == 0)
					throw new LinqException("InsertOrReplace method requires the '{0}' table to have a primary key.", sqlTable.NameForLogging);

				var q =
				(
					from k in keys
					join i in insertOrUpdateStatement.Insert.Items on k equals i.Column
					select new { k, i }
				).ToList();

				var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

				if (missedKey != null)
					throw new LinqException("InsertOrReplace method requires the '{0}.{1}' field to be included in the insert setter.",
						sqlTable.NameForLogging,
						missedKey.Name);

				var fieldCount = 0;

				foreach (var field in fields)
				{
					if (columnFilter != null && !columnFilter(obj, field.ColumnDescriptor, false))
						continue;

					if (!supported || !fieldDic.TryGetValue(field, out param))
					{
						param = GetParameter(type, dataContext, field);
						ei.Queries[0].AddParameterAccessor(param);

						if (supported)
							fieldDic.Add(field, param);
					}

					insertOrUpdateStatement.Update.Items.Add(new SqlSetExpression(field, param.SqlParameter));

					fieldCount++;
				}

				if (fieldCount == 0)
					throw new LinqException("There are no fields to update in the type '{0}'.", sqlTable.NameForLogging);

				insertOrUpdateStatement.Update.Keys.AddRange(q.Select(i => i.i));

				// Set the query.
				//
				if (ei.SqlProviderFlags.IsInsertOrUpdateSupported)
					SetNonQueryQuery(ei);
				else
					MakeAlternativeInsertOrUpdate(ei);

				return ei;
			}

			public static int Query(
				IDataContext                   dataContext,
				T                              obj,
				InsertOrUpdateColumnFilter<T>? columnFilter,
				string?                        tableName,
				string?                        serverName,
				string?                        databaseName,
				string?                        schema,
				TableOptions                   tableOptions)
			{
				if (Equals(default(T), obj))
					return 0;

				var type             = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type, dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

				var cacheDisabled =
					dataContext.Options.LinqOptions.DisableQueryCache                       ||
					columnFilter != null                                                    ||
					entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) ||
					entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update);

				var ei = cacheDisabled
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schema, tableOptions, type)
					: Cache<T,int>.QueryCache.GetOrCreate(
					(
						operation: "IR",
						dataContext.ConfigurationID,
						tableName,
						schema,
						databaseName,
						serverName,
						tableOptions,
						type,
						queryFlags: dataContext.GetQueryFlags()
					),
					(dataContext, entityDescriptor, obj),
					static (entry, key, context) =>
					{
						entry.SlidingExpiration = context.dataContext.Options.LinqOptions.CacheSlidingExpirationOrDefault;
						return CreateQuery(context.dataContext, context.entityDescriptor, context.obj, null, key.tableName, key.serverName, key.databaseName, key.schema, key.tableOptions, key.type);
					});

				return (int)ei.GetElement(dataContext, Expression.Constant(obj), null, null)!;
			}

			public static async Task<int> QueryAsync(
				IDataContext                   dataContext,
				T                              obj,
				InsertOrUpdateColumnFilter<T>? columnFilter,
				string?                        tableName,
				string?                        serverName,
				string?                        databaseName,
				string?                        schema,
				TableOptions                   tableOptions,
				CancellationToken              token)
			{
				if (Equals(default(T), obj))
					return 0;

				var type             = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type, dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

				var cacheDisabled =
					dataContext.Options.LinqOptions.DisableQueryCache                       ||
					columnFilter != null                                                    ||
					entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) ||
					entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update);

				var ei = cacheDisabled
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schema, tableOptions, type)
					: Cache<T,int>.QueryCache.GetOrCreate(
					(
						operation: "IR",
						dataContext.ConfigurationID,
						tableName,
						schema,
						databaseName,
						serverName,
						tableOptions,
						type,
						queryFlags: dataContext.GetQueryFlags()
					),
					(dataContext, entityDescriptor, obj),
					static (entry, key, context) =>
					{
						entry.SlidingExpiration = context.dataContext.Options.LinqOptions.CacheSlidingExpirationOrDefault;
						return CreateQuery(context.dataContext, context.entityDescriptor, context.obj, null, key.tableName, key.serverName, key.databaseName, key.schema, key.tableOptions, key.type);
					});

				var result = await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, null, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return (int)result!;
			}
		}

		public static void MakeAlternativeInsertOrUpdate(Query query)
		{
			var firstStatement  = (SqlInsertOrUpdateStatement)query.Queries[0].Statement;
			var cloned          = firstStatement.Clone();
			var insertStatement = new SqlInsertStatement(cloned.SelectQuery)
			{
				Insert             = cloned.Insert,
				Tag                = cloned.Tag,
				SqlQueryExtensions = cloned.SqlQueryExtensions
			};

			insertStatement.SelectQuery.From.Tables.Clear();

			query.Queries.Add(new QueryInfo
			{
				Statement          = insertStatement,
				ParameterAccessors = query.Queries[0].ParameterAccessors
			});

			var keys = firstStatement.Update.Keys;

			foreach (var key in keys)
				firstStatement.SelectQuery.Where.Expr(key.Column).Equal.Expr(key.Expression!);

			// TODO! looks not working solution
			if (firstStatement.Update.Items.Count > 0)
			{
				query.Queries[0].Statement = new SqlUpdateStatement(firstStatement.SelectQuery)
				{
					Update             = firstStatement.Update,
					Tag                = firstStatement.Tag,
					SqlQueryExtensions = firstStatement.SqlQueryExtensions
				};
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
				ParameterAccessors = query.Queries[0].ParameterAccessors.ToList(),
			});
		}
	}
}
