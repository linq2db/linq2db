using System;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq.Builder
{
	class PassThroughBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("AsQueryable");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}
	}
}
