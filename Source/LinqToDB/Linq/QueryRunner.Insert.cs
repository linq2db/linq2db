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
			static readonly ConcurrentDictionary<object,Query<int>> _queryCache = new ConcurrentDictionary<object,Query<int>>();

			static Query<int> CreateQuery(IDataContext dataContext, string tableName, string databaseName, string schemaName, Type type)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				var insertStatement = new SqlInsertStatement { Insert = { Into = sqlTable } };

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = insertStatement } }
				};

				foreach (var field in sqlTable.Fields)
				{
					if (field.Value.IsInsertable)
					{
						var param = GetParameter(type, dataContext, field.Value);

						ei.Queries[0].Parameters.Add(param);

						insertStatement.Insert.Items.Add(new SqlSetExpression(field.Value, param.SqlParameter));
					}
					else if (field.Value.IsIdentity)
					{
						var sqlb = dataContext.CreateSqlProvider();
						var expr = sqlb.GetIdentityExpression(sqlTable);

						if (expr != null)
							insertStatement.Insert.Items.Add(new SqlSetExpression(field.Value, expr));
					}
				}

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(IDataContext dataContext, T obj, string tableName, string databaseName, string schemaName)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = obj.GetType();
				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, databaseName, schemaName, type };
				var ei  = _queryCache.GetOrAdd(key, o => CreateQuery(dataContext, tableName, databaseName, schemaName, type));

				return (int)ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<int> QueryAsync(
				IDataContext dataContext, T obj, string tableName, string databaseName, string schemaName, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = obj.GetType();
				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, databaseName, schemaName, type };
				var ei  = _queryCache.GetOrAdd(key, o => CreateQuery(dataContext, tableName, databaseName, schemaName, type));

				var result = await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}
		}
	}
}
