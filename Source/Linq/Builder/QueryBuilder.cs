using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	abstract class QueryBuilder
	{
		#region Init

		protected QueryBuilder(IDataContext dataContext, QueryNew query)
		{
			Query         = query;
			DataContext   = dataContext;
			MappingSchema = dataContext.MappingSchema;

			DataReaderLocalParameter = Configuration.AvoidSpecificDataProviderAPI ?
				DataReaderParameter :
				BuildVariableExpression(Expression.Convert(DataReaderParameter, DataContext.DataReaderType), "localDataReader");
		}

		public readonly QueryNew         Query;
		public readonly IDataContext  DataContext;
		public readonly MappingSchema MappingSchema;

		public readonly Dictionary<ParameterExpression,IExpressionBuilder> Builders = new Dictionary<ParameterExpression,IExpressionBuilder>();

		#endregion

		#region Parameters & Variables

		public static readonly ParameterExpression DataContextParameter = Expression.Parameter(typeof(IDataContext), "dataContext");
		public static readonly ParameterExpression ExpressionParameter  = Expression.Parameter(typeof(Expression),   "expression");
		public static readonly ParameterExpression DataReaderParameter  = Expression.Parameter(typeof(IDataReader),  "dataReader");

		public readonly ParameterExpression       DataReaderLocalParameter;
		public readonly List<ParameterExpression> BlockVariables   = new List<ParameterExpression>();
		public readonly List<Expression>          BlockExpressions = new List<Expression>();

		int _varIndex;

		public ParameterExpression BuildVariableExpression(Expression expr, string name = null)
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

		#endregion

		#region Finalize

		public Expression FinalizeQuery(SqlQuery selectQuery, Expression expression)
		{
			expression = BuildBlock(expression);

			Query.FinalizeQuery(selectQuery);

			return expression;
		}

		#endregion

		#region TransformQuery

		protected abstract Expression CreateQueryExpression(IExpressionBuilder expressionBuilder);

		protected Expression TramsformQuery(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant :
					{
						var c = (ConstantExpression)expression;
						if (c.Value is ITable)
							return CreateQueryExpression(new TableBuilderNew(expression));
						break;
					}

				case ExpressionType.MemberAccess:
					{
						if (typeof(ITable).IsSameOrParentOf(expression.Type))
							return CreateQueryExpression(new TableBuilderNew(expression));
						break;
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;

						if (call.Method.Name == "GetTable")
							if (typeof(ITable).IsSameOrParentOf(expression.Type))
								return CreateQueryExpression(new TableBuilderNew(expression));

						var attr = Query.MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(call.Method, a => a.Configuration);

						if (attr != null)
							return CreateQueryExpression(new TableFunctionBuilder(this, expression));

						if (call.IsQueryable())
						{
							if (call.Object == null && call.Arguments.Count > 0 && call.Arguments[0] != null)
							{
								var qe = call.Arguments[0].Transform(e => TramsformQuery(e)) as QueryExpression;

								if (qe != null)
								{
									switch (call.Method.Name)
									{
										case "Select" : return SelectBuilder1.Translate(qe, call);
										case "Where"  :
										case "Having" : return WhereBuilder1. Translate(qe, call);
									}
								}
							}
						}

						break;
					}
			}

			return expression;
		}

		#endregion
	}

	class QueryBuilder<T> : QueryBuilder
	{
		public QueryBuilder(IDataContext dataContext, QueryNew<T> query)
			: base(dataContext, query)
		{
		}

		public new QueryNew<T> Query { get { return (QueryNew<T>)base.Query; } }

		protected override Expression CreateQueryExpression(IExpressionBuilder expressionBuilder)
		{
			return new QueryExpression<T>(this, expressionBuilder);
		}

		public Func<IDataContext,Expression,IEnumerable<T>> BuildEnumerable()
		{
			var expr = Query.Expression.Transform(e => TramsformQuery(e));

			if (expr is QueryExpression<T>)
			{
				BuildQuery((QueryExpression<T>)expr);
				return Query.GetIEnumerable;
			}

			return BuildQuery<IEnumerable<T>>(expr);
		}

		public Func<IDataContext,Expression,T> BuildElement()
		{
			var expr = Query.Expression.Transform(e => TramsformQuery(e));

			if (expr is QueryExpression<T>)
			{
				BuildQuery((QueryExpression<T>)expr);
				return Query.GetElement;
			}

			return BuildQuery<T>(expr);
		}

		static Func<IDataContext,Expression,TResult> BuildQuery<TResult>(Expression expr)
		{
			if (expr.Type != typeof(TResult))
				expr = Expression.Convert(expr, typeof(TResult));

			var l = Expression.Lambda<Func<IDataContext,Expression,TResult>>(
				expr, DataContextParameter, ExpressionParameter);

			return l.Compile();
		}

		void BuildQuery(QueryExpression<T> expression)
		{
			SqlQuery sql = null;

			for (var builder = expression.First; builder != null; builder = builder.Next)
				sql = builder.BuildSql(this, sql);

			expression.Last.BuildQuery(this);
		}

		public Expression BuildQueryExpression(QueryExpression<T> expression)
		{
			SqlQuery sql = null;

			for (var builder = expression.First; builder != null; builder = builder.Next)
				sql = builder.BuildSql(this, sql);

			return expression.Last.BuildQueryExpression(this);
		}

		#region BuildQuery

		public void BuildQuery(Expression<Func<IDataReader,T>> mapper)
		{
			Query.BuildQuery(mapper);
		}

		public Expression BuildQueryExpression(Expression<Func<IDataReader,T>> mapper)
		{
			var expr = Expression.Call(
				Expression.Constant(Query),
				MemberHelper.MethodOf(() => Query.ExecuteQuery(null, null, null)),
				DataContextParameter,
				ExpressionParameter,
				mapper);

			return expr;
		}

		#endregion
	}
}
