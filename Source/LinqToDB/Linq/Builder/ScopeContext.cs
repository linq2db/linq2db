using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class ScopeContext : PassThroughContext
	{
		public ScopeContext(IBuildContext context) : base(context)
		{
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.Root))
				return path;

			var newExpr = base.MakeExpression(path, flags);

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				newExpr = Builder.UpdateNesting(Context, newExpr);
			}

			return newExpr;
		}
	}
}
