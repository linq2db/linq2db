using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.IgnoreFilters))]
	sealed class IgnoreFiltersBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.IgnoreFilters)
			|| call.IsSameGenericMethod(Methods.LinqToDB.IgnoreFiltersByKey)
			|| call.IsSameGenericMethod(Methods.LinqToDB.IgnoreFiltersByKeyAndType);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.IsSameGenericMethod(Methods.LinqToDB.IgnoreFiltersByKey))
			{
				var keys = builder.EvaluateExpression<string[]>(methodCall.Arguments[1])!;
				builder.PushDisabledQueryFilters(keys, []);
			}
			else if (methodCall.IsSameGenericMethod(Methods.LinqToDB.IgnoreFiltersByKeyAndType))
			{
				var keys  = builder.EvaluateExpression<string[]>(methodCall.Arguments[1])!;
				var types = builder.EvaluateExpression<Type[]>  (methodCall.Arguments[2])!;
				builder.PushDisabledQueryFilters(keys, types);
			}
			else
			{
				var types = builder.EvaluateExpression<Type[]>(methodCall.Arguments[1])!;
				builder.PushDisabledQueryFilters(types);
			}

			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopDisabledFilter();

			return sequence;
		}
	}
}
