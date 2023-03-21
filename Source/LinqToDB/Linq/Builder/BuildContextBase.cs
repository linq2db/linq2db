using System;
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

		protected BuildContextBase(ExpressionBuilder builder, Type elementType, SelectQuery selectQuery)
		{
			Builder     = builder;
			ElementType = elementType;
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

		public virtual  Type       ElementType { get; }
		public abstract Expression MakeExpression(Expression path, ProjectFlags flags);

		public abstract void SetRunQuery<T>(Query<T> query, Expression expr);

		public abstract IBuildContext Clone(CloningContext context);

		public abstract SqlStatement GetResultStatement();

		public virtual void SetAlias(string? alias)
		{
		}

		#region Obsolete

		public virtual IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return null;
		}

		public virtual void CompleteColumns()
		{
		}

		#endregion Obsolete

	}
}
