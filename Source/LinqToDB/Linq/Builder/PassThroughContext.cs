using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	abstract class PassThroughContext : IBuildContext
	{
		protected PassThroughContext(IBuildContext context)
		{
			Context = context;

			context.Builder.Contexts.Add(this);
		}

		public IBuildContext Context { get; set; }

#if DEBUG
		string IBuildContext._sqlQueryText { get { return Context._sqlQueryText; } }
#endif

		public virtual ExpressionBuilder Builder     { get { return Context.Builder;     } }
		public virtual Expression        Expression  { get { return Context.Expression;  } }
		public virtual SelectQuery       SelectQuery { get { return Context.SelectQuery; } set { Context.SelectQuery = value; } }
		public virtual IBuildContext     Parent      { get { return Context.Parent;      } set { Context.Parent      = value; } }

		public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			Context.BuildQuery(query, queryParameter);
		}

		public virtual Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
		{
			return Context.BuildExpression(expression, level, enforceServerSide);
		}

		public virtual SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			return Context.ConvertToSql(expression, level, flags);
		}

		public virtual SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			return Context.ConvertToIndex(expression, level, flags);
		}

		public virtual IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
		{
			return Context.IsExpression(expression, level, requestFlag);
		}

		public virtual IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
		{
			return Context.GetContext(expression, level, buildInfo);
		}

		public virtual int ConvertToParentIndex(int index, IBuildContext context)
		{
			return Context.ConvertToParentIndex(index, context);
		}

		public virtual void SetAlias(string alias)
		{
			Context.SetAlias(alias);
		}

		public virtual ISqlExpression GetSubQuery(IBuildContext context)
		{
			return Context.GetSubQuery(context);
		}
	}
}
