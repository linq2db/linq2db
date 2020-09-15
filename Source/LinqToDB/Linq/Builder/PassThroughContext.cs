﻿using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	abstract class PassThroughContext : IBuildContext
	{
		protected PassThroughContext(IBuildContext context)
		{
			Context = context;

			context.Builder.Contexts.Add(this);
		}

		public IBuildContext Context { get; set; }

#if DEBUG
		string? IBuildContext._sqlQueryText => Context._sqlQueryText;
		public string Path => this.GetPath();
#endif

		public virtual ExpressionBuilder Builder     => Context.Builder;
		public virtual Expression?       Expression  => Context.Expression;
		public virtual SelectQuery       SelectQuery { get => Context.SelectQuery; set => Context.SelectQuery = value; }
		public virtual SqlStatement?     Statement   { get => Context.Statement;   set => Context.Statement   = value; }
		public virtual IBuildContext?    Parent      { get => Context.Parent;      set => Context.Parent      = value; }

		public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			Context.BuildQuery(query, queryParameter);
		}

		public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.BuildExpression(expression, level, enforceServerSide);
		}

		public virtual SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.ConvertToSql(expression, level, flags);
		}

		public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.ConvertToIndex(expression, level, flags);
		}

		public virtual IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.IsExpression(expression, level, requestFlag);
		}

		public virtual IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
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

		public virtual ISqlExpression? GetSubQuery(IBuildContext context)
		{
			return Context.GetSubQuery(context);
		}

		public virtual SqlStatement GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public void CompleteColumns()
		{
			Context.CompleteColumns();
		}
	}
}
