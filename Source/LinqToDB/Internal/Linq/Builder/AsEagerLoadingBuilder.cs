using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.AsEagerLoadUnionQuery), nameof(LinqExtensions.AsEagerLoadSeparateQuery), nameof(LinqExtensions.AsEagerLoadKeyedQuery))]
	sealed class AsEagerLoadingBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.AsEagerLoadUnionQueryQueryable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.AsEagerLoadSeparateQueryQueryable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.AsEagerLoadKeyedQueryQueryable);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var strategy = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.AsEagerLoadUnionQuery)    => EagerLoadingStrategy.CteUnion,
				nameof(LinqExtensions.AsEagerLoadSeparateQuery) => EagerLoadingStrategy.Default,
				_                                      => EagerLoadingStrategy.KeyedQuery, // AsEagerLoadKeyedQuery
			};

			var currentModifier = builder.GetTranslationModifier();
			builder.PushTranslationModifier(currentModifier.WithEagerLoadingStrategy(strategy), true);
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopTranslationModifier();

			return sequence;
		}
	}
}
