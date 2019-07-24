using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	class HasUniqueKeyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("HasUniqueKey");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			
			var keySelector = (LambdaExpression) methodCall.Arguments[1].Unwrap();
			var keyContext  = new SelectContext(sequence, keySelector, sequence);
			var keySql      = builder.ConvertExpressions(keyContext, keySelector.Body.Unwrap(), ConvertFlags.All);

			var uniqueKeys  = keySql
				.Select(info => sequence.SelectQuery.Select.Columns[sequence.SelectQuery.Select.Add(info.Sql)])
				.ToArray();

			sequence.SelectQuery.UniqueKeys.Add(uniqueKeys);

			return new SubQueryContext(sequence);
		}

		protected override SequenceConvertInfo Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression param)
		{
			return null;
		}
	}
}
