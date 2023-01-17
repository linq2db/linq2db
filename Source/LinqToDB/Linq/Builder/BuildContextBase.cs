using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	abstract class BuildContextBase : IBuildContext
	{

#if DEBUG
		public string SqlQueryText => SelectQuery?.SqlText ?? "";
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif

		protected BuildContextBase(ExpressionBuilder builder, SelectQuery selectQuery)
		{
			Builder     = builder;
			SelectQuery = selectQuery;
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
		}

		public         ExpressionBuilder Builder     { get; }
		public virtual Expression?       Expression  => null;
		public         SelectQuery       SelectQuery { get; protected set; }
		public virtual SqlStatement?     Statement   { get; set; }
		public         IBuildContext?    Parent      { get; set; }

		public abstract Expression MakeExpression(Expression path, ProjectFlags flags);

		public abstract void SetRunQuery<T>(Query<T> query, Expression expr);

		public abstract IBuildContext Clone(CloningContext context);

		public abstract SqlStatement GetResultStatement();

		public virtual void SetAlias(string? alias)
		{
		}

		public virtual ISqlExpression? GetSubQuery(IBuildContext context)
		{
			return null;
		}
		
		#region Obsolete

		public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			throw new NotImplementedException();
		}

		public virtual SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public virtual IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public virtual IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		public virtual int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public virtual void CompleteColumns()
		{
		}

		#endregion Obsolete

	}
}
