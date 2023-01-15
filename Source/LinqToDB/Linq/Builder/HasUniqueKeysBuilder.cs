using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsMethodCall("HasUniqueKey")]
	
	sealed class HasUniqueKeyBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo buildInfo, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var keySelector = methodCall.Arguments[1].UnwrapLambda();
			var keyContext  = new SelectContext(buildInfo.Parent, keySelector, sequence, false);

			var keySql = builder.ConvertToSqlExpr(keyContext, new ContextRefExpression(keySelector.Parameters[0].Type, keyContext));

			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(keySql);

			sequence.SelectQuery.UniqueKeys.Add(placeholders.Select(p => p.Sql).ToArray());

			return new SubQueryContext(sequence);
		}
	}
}
