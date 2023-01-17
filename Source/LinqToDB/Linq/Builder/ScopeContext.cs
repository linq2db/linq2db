using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class ScopeContext : BuildContextBase
	{
		public IBuildContext Context    { get; }
		public IBuildContext UpTo       { get; }
		public bool          OnlyForSql { get; }

		public ScopeContext(IBuildContext context, IBuildContext upTo) : base(context.Builder, upTo.SelectQuery)
		{
			Context = context;
			UpTo    = upTo;
		}

		public ScopeContext(IBuildContext context, IBuildContext upTo, bool onlyForSql) : this(context, upTo)
		{
			OnlyForSql = onlyForSql;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var correctedPath = SequenceHelper.CorrectExpression(path, this, Context);
			var newExpr       = Builder.MakeExpression(Context, correctedPath, flags);

			// nothing changed, return as is
			if (ExpressionEqualityComparer.Instance.Equals(newExpr, correctedPath))
				return path;

			if (!flags.IsTest())
			{
				if (!OnlyForSql)
				{
					newExpr = SequenceHelper.MoveAllToScopedContext(newExpr, UpTo);
				}
				newExpr = Builder.UpdateNesting(UpTo, newExpr);
			}

			return newExpr;
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new ScopeContext(context.CloneContext(Context), context.CloneContext(UpTo));
		}

		public override SqlStatement GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.GetContext(expression, buildInfo);
		}
	}
}
