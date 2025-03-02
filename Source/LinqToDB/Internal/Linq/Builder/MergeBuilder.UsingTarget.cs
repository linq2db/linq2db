using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

using static LinqToDB.Internal.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Internal.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.UsingTarget))]
		internal sealed class UsingTarget : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(UsingTargetMethodInfo);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var genericArguments = methodCall.Method.GetGenericArguments();

				var cloningContext      = new CloningContext();
				cloningContext.CloneElements(builder.GetCteClauses());
				var clonedTargetContext = cloningContext.CloneContext(mergeContext.TargetContext);

				var targetContextRef = new ContextRefExpression(genericArguments[0], mergeContext.TargetContext, "target");
				var sourceContextRef = new ContextRefExpression(genericArguments[0], clonedTargetContext, "source");

				var source                = new TableLikeQueryContext(builder.GetTranslationModifier(), targetContextRef, sourceContextRef);
				mergeContext.Sequences    = [mergeContext.Sequence, source];
				mergeContext.Merge.Source = source.Source;

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
