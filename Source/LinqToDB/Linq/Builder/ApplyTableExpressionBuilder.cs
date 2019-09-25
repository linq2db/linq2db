using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	class ApplyTableExpressionBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(
				LinqExtensions.ApplyTableExpressionMethodInfo1,
				LinqExtensions.ApplyTableExpressionMethodInfo2, 
				LinqExtensions.ApplyTableExpressionExceptMethodInfo);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var context = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var groups = Enumerable.Empty<string>();
			if (methodCall.Arguments.Count > 2)
			{
				var groupsStr = (string)methodCall.Arguments[2].EvaluateExpression();
				groups = groupsStr.Split(',', ';');
			}

			var isExcept      = methodCall.IsSameGenericMethod(LinqExtensions.ApplyTableExpressionExceptMethodInfo);
			var expressionStr = (string)methodCall.Arguments[1].EvaluateExpression();

			context.SelectQuery.AddApplyTableExpression(isExcept, expressionStr, groups);

			return context;
		}

		protected override SequenceConvertInfo Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression param)
		{
			return null;
		}
	}
}
