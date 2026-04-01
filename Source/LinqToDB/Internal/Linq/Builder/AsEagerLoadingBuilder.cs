using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.AsUnionQuery), nameof(LinqExtensions.AsSeparateQuery), nameof(LinqExtensions.AsKeyedQuery))]
	sealed class AsEagerLoadingBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.AsUnionQueryEnumerable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.AsUnionQueryQueryable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.AsSeparateQueryEnumerable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.AsSeparateQueryQueryable)
			|| call.IsSameGenericMethod(Methods.LinqToDB.AsKeyedQueryQueryable);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var strategy = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.AsUnionQuery)    => EagerLoadingStrategy.CteUnion,
				nameof(LinqExtensions.AsSeparateQuery) => EagerLoadingStrategy.Default,
				_                                      => EagerLoadingStrategy.PostQuery, // AsKeyedQuery
			};

			var currentModifier = builder.GetTranslationModifier();
			builder.PushTranslationModifier(currentModifier.WithEagerLoadingStrategy(strategy), true);
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopTranslationModifier();

			return sequence;
		}
	}
}
