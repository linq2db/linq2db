using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Builder;
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public abstract class Query
	{
		protected Query(IDataContext dataContext, Expression expression)
		{
			ContextID       = dataContext.ContextID;
			Expression      = expression;
			MappingSchema   = dataContext.MappingSchema;
			ConfigurationID = dataContext.MappingSchema.ConfigurationID;
			SqlOptimizer    = dataContext.GetSqlOptimizer();
		}

		public readonly string        ContextID;
		public readonly Expression    Expression;
		public readonly MappingSchema MappingSchema;
		public readonly string        ConfigurationID;
		public readonly ISqlOptimizer SqlOptimizer;

		public SqlQuery SqlQuery;

		readonly Dictionary<Expression,QueryableAccessor> _queryableAccessorDic  = new Dictionary<Expression,QueryableAccessor>();

		public bool Compare(string contextID, MappingSchema mappingSchema, Expression expr)
		{
			return
				ContextID.Length == contextID.Length              &&
				ContextID        == contextID                     &&
				ConfigurationID  == mappingSchema.ConfigurationID &&
				Expression.EqualsTo(expr, _queryableAccessorDic);
		}

		public void FinalizeQuery(SqlQuery sqlQuery)
		{
			SqlQuery = SqlOptimizer.Finalize(sqlQuery);

			if (!SqlQuery.IsParameterDependent)
			{
				
			}
		}

		public class CommandInfo
		{
			public string          CommandText;
			public DataParameter[] Parameters;
		}

		public CommandInfo GetCommandInfo(IDataContext dataContext, Expression expression)
		{
			var sqlProvider   = dataContext.CreateSqlProvider();
			var stringBuilder = new StringBuilder();

			sqlProvider.BuildSql(0, SqlQuery, stringBuilder);

			return new CommandInfo
			{
				CommandText = stringBuilder.ToString(),
			};
		}
	}

	class Query<T> : Query
	{
		Query(IDataContext dataContext, Expression expression)
			: base(dataContext, expression)
		{
		}

		public Func<IDataContext,Expression,T>                                GetElement;
		public Func<IDataContext,Expression,IEnumerable<T>>                   GetIEnumerable;
		public Func<IDataContext,Expression,Action<T>,CancellationToken,Task> GetForEachAsync;

		Query<T> _next;

		#region GetQuery

		static          Query<T> _first;
		static readonly object   _sync = new object();

		const int CacheSize = 100;

		public static Query<T> GetQuery(IDataContext dataContext, Expression expr, bool isEnumerable)
		{
			var query = FindQuery(dataContext, expr);

			if (query == null)
			{
				lock (_sync)
				{
					query = FindQuery(dataContext, expr);

					if (query == null)
					{
						if (Configuration.Linq.GenerateExpressionTest)
						{
							var testFile = new ExpressionTestGenerator().GenerateSource(expr);
#if !SILVERLIGHT && !NETFX_CORE
							DataConnection.WriteTraceLine(
								"Expression test code generated: '" + testFile + "'.", 
								DataConnection.TraceSwitch.DisplayName);
#endif
						}

						query = new Query<T>(dataContext, expr);

						try
						{
							if (isEnumerable) query.GetIEnumerable = new QueryBuilder<T>(dataContext, query).BuildEnumerable();
							else              query.GetElement     = new QueryBuilder<T>(dataContext, query).BuildElement   ();
						}
						catch (Exception)
						{
							if (!Configuration.Linq.GenerateExpressionTest)
							{
#if !SILVERLIGHT && !NETFX_CORE
								DataConnection.WriteTraceLine(
									"To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.",
									DataConnection.TraceSwitch.DisplayName);
#endif
							}

							throw;
						}
					}
				}
			}

			return query;
		}

		static Query<T> FindQuery(IDataContext dataContext, Expression expr)
		{
			Query<T> prev = null;
			var      n    = 0;

			for (var query = _first; query != null; query = query._next)
			{
				if (query.Compare(dataContext.ContextID, dataContext.MappingSchema, expr))
				{
					if (prev != null)
					{
						lock (_sync)
						{
							prev._next  = query._next;
							query._next = _first;
							_first      = query;
						}
					}

					return query;
				}

				if (n++ >= CacheSize)
				{
					query._next = null;
					return null;
				}

				prev = query;
			}

			return null;
		}

		#endregion

		#region Execute

		internal IEnumerable<T> ExecuteQuery(IDataContext dataContext, Expression expression, Func<IDataReader,T> mapper)
		{
			using (var ctx = dataContext.GetQueryContext(this, expression))
			using (var dr = ctx.ExecuteReader())
				while (dr.Read())
					yield return mapper(dr);
		}

		internal async Task ExecuteQueryAsync(
			IDataContext dataContext, Expression expression, Func<IDataReader,T> mapper, Action<T> action, CancellationToken cancellationToken)
		{
			using (var ctx = dataContext.GetQueryContext(this, expression))
			using (var dr = await ctx.ExecuteReaderAsync(cancellationToken))
				await dr.QueryForEachAsync(mapper, action, cancellationToken);
		}

		public void BuildQuery(Expression<Func<IDataReader,T>> mapper)
		{
			var l = mapper.Compile();

			GetIEnumerable  = (ctx, expr)                => ExecuteQuery     (ctx, expr, l);
			GetForEachAsync = (ctx, expr, action, token) => ExecuteQueryAsync(ctx, expr, l, action, token);
		}

		#endregion
	}
}
