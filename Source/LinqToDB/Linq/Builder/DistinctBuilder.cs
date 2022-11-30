using System.Reflection;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Reflection;
	using LinqToDB.Expressions;

	sealed class DistinctBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.Distinct, Methods.Enumerable.Distinct, Methods.LinqToDB.SelectDistinct };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(_supportedMethods);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var sql      = sequence.SelectQuery;
			if (sql.Select.TakeValue != null || sql.Select.SkipValue != null)
			{
				sequence = new SubQueryContext(sequence);
			}

			var subQueryContext = new SubQueryContext(sequence);

			subQueryContext.SelectQuery.Select.IsDistinct = true;

			var outerSubqueryContext = new SubQueryContext(subQueryContext);

			// We do not need all fields for SelectDistinct
			//
			if (methodCall.IsSameGenericMethod(Methods.LinqToDB.SelectDistinct))
			{
				subQueryContext.SelectQuery.Select.OptimizeDistinct = true;
			}
			else
			{
				// create all columns
				var sqlExpr = builder.ConvertToSqlExpr(subQueryContext, new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], sequence), buildInfo.GetFlags());
				builder.UpdateNesting(outerSubqueryContext, sqlExpr);
			}

			return new DistinctContext(outerSubqueryContext);
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		class DistinctContext : PassThroughContext
		{
			public DistinctContext(IBuildContext context) : base(context)
			{
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)))
					return path;

				return base.MakeExpression(path, flags);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DistinctContext(context.CloneContext(Context));
			}
		}
	}
}
