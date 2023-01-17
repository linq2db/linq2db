using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	abstract class PassThroughContext : BuildContextBase
	{
		protected PassThroughContext(IBuildContext context, SelectQuery selectQuery) : base(context.Builder, selectQuery)
		{
			Context = context;
			Parent  = context.Parent;
		}

		protected PassThroughContext(IBuildContext context) : this(context, context.SelectQuery)
		{
		}

		public IBuildContext Context { get; protected set; }

		public override Expression?   Expression => Context.Expression;
		public override SqlStatement? Statement  { get => Context.Statement; set => Context.Statement = value; }

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			path = SequenceHelper.CorrectExpression(path, this, Context);
			return Builder.MakeExpression(Context, path, flags);
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override void SetAlias(string? alias)
		{
			Context.SetAlias(alias);
		}

		public override ISqlExpression? GetSubQuery(IBuildContext context)
		{
			return Context.GetSubQuery(context);
		}

		public override SqlStatement GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override void CompleteColumns()
		{
			Context.CompleteColumns();
		}
	}
}
