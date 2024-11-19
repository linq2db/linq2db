using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;

	[BuildsMethodCall(nameof(LinqExtensions.IgnoreFilters))]
	sealed class IgnoreFiltersBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(Methods.LinqToDB.IgnoreFilters);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var types = builder.EvaluateExpression<Type[]>(methodCall.Arguments[1])!;

			builder.PushDisabledQueryFilters(types);
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopDisabledFilter();

			return sequence;
		}
	}
}
