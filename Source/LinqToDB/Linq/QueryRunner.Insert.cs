using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Insert<T>
		{
			static readonly ConcurrentDictionary<object,Query<int>> _queryChache = new ConcurrentDictionary<object,Query<int>>();

			static Query<int> CreateQuery(IDataContext dataContext, string tableName, string databaseName, string schemaName)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery();
				sqlQuery.QueryType = QueryType.Insert;

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Owner        = schemaName;

				sqlQuery.Insert.Into = sqlTable;

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = new SqlInsertStatement(sqlQuery) } }
				};

				foreach (var field in sqlTable.Fields)
				{
					if (field.Value.IsInsertable)
					{
						var param = GetParameter(typeof(T), dataContext, field.Value);

						ei.Queries[0].Parameters.Add(param);

						sqlQuery.Insert.Items.Add(new SqlSetExpression(field.Value, param.SqlParameter));
					}
					else if (field.Value.IsIdentity)
					{
						var sqlb = dataContext.CreateSqlProvider();
						var expr = sqlb.GetIdentityExpression(sqlTable);

						if (expr != null)
							sqlQuery.Insert.Items.Add(new SqlSetExpression(field.Value, expr));
					}
				}

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(IDataContext dataContext, T obj, string tableName, string databaseName, string schemaName)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, databaseName, schemaName };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext, tableName, databaseName, schemaName));

				return (int)ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<int> QueryAsync(
				IDataContext dataContext, T obj, string tableName, string databaseName, string schemaName, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, databaseName, schemaName };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext, tableName, databaseName, schemaName));

				var result = await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}
		}
	}
}
