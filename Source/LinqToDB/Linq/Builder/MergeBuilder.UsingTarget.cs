using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

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

				var source                = new TableLikeQueryContext(targetContextRef, sourceContextRef);
				mergeContext.Sequences    = new IBuildContext[] { mergeContext.Sequence, source };
				mergeContext.Merge.Source = source.Source;

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
