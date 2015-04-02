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
			ContextID        = dataContext.ContextID;
			Expression       = expression;
			MappingSchema    = dataContext.MappingSchema;
			ConfigurationID  = dataContext.MappingSchema.ConfigurationID;
			SqlProviderFlags = dataContext.SqlProviderFlags;
			SqlOptimizer     = dataContext.GetSqlOptimizer();

			_variables = new BuildVariables(dataContext, this);
		}

		public readonly string           ContextID;
		public readonly Expression       Expression;
		public readonly MappingSchema    MappingSchema;
		public readonly string           ConfigurationID;
		public readonly SqlProviderFlags SqlProviderFlags;
		public readonly ISqlOptimizer    SqlOptimizer;

		public SelectQuery SelectQuery;

		public static readonly ParameterExpression DataContextParameter = Expression.Parameter(typeof(IDataContext), "dataContext");
		public static readonly ParameterExpression ExpressionParameter  = Expression.Parameter(typeof(Expression),   "expression");
		public static readonly ParameterExpression DataReaderParameter  = Expression.Parameter(typeof(IDataReader),  "dataReader");

		readonly Dictionary<Expression,QueryableAccessor> _queryableAccessorDic  = new Dictionary<Expression,QueryableAccessor>();

		public bool Compare(string contextID, MappingSchema mappingSchema, Expression expr)
		{
			return
				ContextID.Length == contextID.Length              &&
				ContextID        == contextID                     &&
				ConfigurationID  == mappingSchema.ConfigurationID &&
				Expression.EqualsTo(expr, _queryableAccessorDic);
		}

		#region Build Variables

		BuildVariables _variables;

		public IDataContext               DataContext              { get { return _variables.DataContext; } }
		public ParameterExpression        DataReaderLocalParameter { get { return _variables.DataReaderLocalParameter; } }
		public List<ParameterExpression>  BlockVariables           { get { return _variables.BlockVariables;           } }
		public List<Expression>           BlockExpressions         { get { return _variables.BlockExpressions;         } }

		class BuildVariables
		{
			public BuildVariables(IDataContext dataContext, Query query)
			{
				DataContext = dataContext;

				DataReaderLocalParameter = Configuration.AvoidSpecificDataProviderAPI ?
					DataReaderParameter :
					BuildVariableExpression(Expression.Convert(DataReaderParameter, dataContext.DataReaderType), "localDataReader");
			}

			public readonly ParameterExpression       DataReaderLocalParameter;
			public readonly IDataContext              DataContext;
			public readonly List<ParameterExpression> BlockVariables   = new List<ParameterExpression>();
			public readonly List<Expression>          BlockExpressions = new List<Expression>();

			int _varIndex;

			public ParameterExpression BuildVariableExpression(Expression expr, string name)
			{
				if (name == null)
					name = expr.Type.Name + Interlocked.Increment(ref _varIndex);

				var variable = Expression.Variable(
					expr.Type,
					name.IndexOf('<') >= 0 ? null : name);

				BlockVariables.  Add(variable);
				BlockExpressions.Add(Expression.Assign(variable, expr));

				return variable;
			}

			public Expression BuildBlock(Expression expression)
			{
				if (BlockExpressions.Count == 0)
					return expression;

				BlockExpressions.Add(expression);

				expression = Expression.Block(BlockVariables, BlockExpressions);

				while (BlockVariables.  Count > 1) BlockVariables.  RemoveAt(BlockVariables.  Count - 1);
				while (BlockExpressions.Count > 1) BlockExpressions.RemoveAt(BlockExpressions.Count - 1);

				return expression;
			}
		}

		#endregion

		public ParameterExpression BuildVariableExpression(Expression expr, string name = null)
		{
			return _variables.BuildVariableExpression(expr, name);
		}

		public Expression FinalizeQuery(SelectQuery selectQuery, Expression expression)
		{
			expression  = _variables.BuildBlock(expression);
			SelectQuery = SqlOptimizer.Finalize(selectQuery);

			if (!SelectQuery.IsParameterDependent)
			{
				
			}

			// Clean building context.
			//
			_variables  = null;

			return expression;
		}

		public class CommandInfo
		{
			public string          CommandText;
			public DataParameter[] Parameters;
		}

		// IT : # GetCommandInfo
		public CommandInfo GetCommandInfo(IDataContext dataContext, Expression expression)
		{
			var sqlProvider   = dataContext.CreateSqlProvider();
			var stringBuilder = new StringBuilder();

			sqlProvider.BuildSql(0, SelectQuery, stringBuilder);

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
							var builder = new QueryBuilder(query);

							if (isEnumerable) query.GetIEnumerable = builder.BuildEnumerable(query);
							else              query.GetElement     = builder.BuildElement   (query);
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
	}
}
