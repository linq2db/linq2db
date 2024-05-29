using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class AsValueInsertableBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("AsValueInsertable");
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var insertContext = new InsertBuilder.InsertContext(sequence,
				InsertBuilder.InsertContext.InsertTypeEnum.Insert, new SqlInsertStatement(sequence.SelectQuery), null)
			{
				RequiresSetters = true
			};

			return BuildSequenceResult.FromContext(insertContext);
		}
	}
}
