using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;

	sealed class IgnoreFiltersBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(Methods.LinqToDB.IgnoreFilters);
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var types = methodCall.Arguments[1].EvaluateExpression<Type[]>(builder.DataContext)!;

			builder.PushDisabledQueryFilters(types);
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopDisabledFilter();

			return sequence;
		}
	}
}
