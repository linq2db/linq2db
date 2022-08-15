using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class ScopeContext : PassThroughContext
	{
		readonly IBuildContext _upTo;

		public ScopeContext(IBuildContext context, IBuildContext upTo) : base(context)
		{
			_upTo = upTo;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.Root))
				return path;

			var newExpr = base.MakeExpression(path, flags);

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				newExpr = Builder.UpdateNesting(_upTo, newExpr);
			}

			return newExpr;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new ScopeContext(context.CloneContext(Context), context.CloneContext(_upTo));
		}
	}
}
