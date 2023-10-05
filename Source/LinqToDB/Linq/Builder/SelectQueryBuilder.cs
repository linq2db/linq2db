using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class SelectQueryBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(DataExtensions.SelectQueryMethodInfo);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = new SelectContext(buildInfo.Parent,
				builder,
				null,
				methodCall.Arguments[1].UnwrapLambda().Body,
				buildInfo.SelectQuery, buildInfo.IsSubQuery);

			var subquery = new SubQueryContext(sequence);

			var translated = builder.BuildSqlExpression(subquery, new ContextRefExpression(subquery.ElementType, subquery),
				ProjectFlags.SQL, buildFlags : ExpressionBuilder.BuildFlags.ForceAssignments);

			return subquery;
		}

		public override bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
