using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsMethodCall(nameof(DataExtensions.SelectQuery))]
	sealed class SelectQueryBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(DataExtensions.SelectQueryMethodInfo);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = new SelectContext(
				builder.GetTranslationModifier(),
				buildInfo.Parent,
				builder,
				null,
				methodCall.Arguments[1].UnwrapLambda().Body,
				buildInfo.SelectQuery, 
				buildInfo.IsSubQuery);

			var subquery = new SubQueryContext(sequence);

			var translated = builder.BuildSqlExpression(
				subquery, 
				new ContextRefExpression(subquery.ElementType, subquery));

			return BuildSequenceResult.FromContext(subquery);
		}

		public override bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
