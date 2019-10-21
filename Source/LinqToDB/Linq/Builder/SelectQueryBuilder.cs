#nullable disable
using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	class SelectQueryBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(DataExtensions.SelectQueryMethodInfo);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = new SelectContext(buildInfo.Parent, builder,
				(LambdaExpression)methodCall.Arguments[1].Unwrap(),
				buildInfo.SelectQuery);

			return sequence;
		}

		protected override SequenceConvertInfo Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression param)
		{
			return null;
		}

	}
}
