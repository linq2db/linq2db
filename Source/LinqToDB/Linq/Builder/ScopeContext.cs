using System.Linq.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class ScopeContext : PassThroughContext
	{
		public   IBuildContext UpTo { get; }

		public override SelectQuery SelectQuery { get => UpTo.SelectQuery; set => UpTo.SelectQuery = value; }

		public ScopeContext(IBuildContext context, IBuildContext upTo) : base(context)
		{
			UpTo = upTo;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.Expand))
				return path;

			var newExpr = base.MakeExpression(path, flags);

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				newExpr = SequenceHelper.MoveAllToScopedContext(newExpr, UpTo);
				newExpr = Builder.UpdateNesting(UpTo, newExpr);
			}

			return newExpr;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new ScopeContext(context.CloneContext(Context), context.CloneContext(UpTo));
		}
	}
}
