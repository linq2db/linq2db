using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.WithUnionLoadStrategy), nameof(LinqExtensions.WithSeparateLoadStrategy), nameof(LinqExtensions.WithKeyedLoadStrategy))]
	sealed class WithLoadStrategyBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.WithUnionLoadStrategyQueryable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.WithSeparateLoadStrategyQueryable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.WithKeyedLoadStrategyQueryable);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var strategy = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.WithUnionLoadStrategy)    => EagerLoadingStrategy.CteUnion,
				nameof(LinqExtensions.WithSeparateLoadStrategy) => EagerLoadingStrategy.Default,
				_                                               => EagerLoadingStrategy.KeyedQuery, // WithKeyedLoadStrategy
			};

			var currentModifier = builder.GetTranslationModifier();

			// Last-wins precedence: the outermost marker (built first) wins. A marker nested closer to the
			// source must not override a strategy an outer marker already set, so skip the push in that case.
			if (currentModifier.EagerLoadingStrategy != null)
				return builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			builder.PushTranslationModifier(currentModifier.WithEagerLoadingStrategy(strategy), true);
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopTranslationModifier();

			return sequence;
		}
	}
}
