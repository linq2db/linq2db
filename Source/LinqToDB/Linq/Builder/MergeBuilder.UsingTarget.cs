using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class UsingTarget : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& UsingTargetMethodInfo == methodCall.Method.GetGenericMethodDefinition();
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				// is it ok to reuse context like that?
				var source                = new TableLikeQueryContext(mergeContext.TargetContext);
				mergeContext.Sequences    = new IBuildContext[] { mergeContext.Sequence, source };
				mergeContext.Merge.Source = source.Source;

				return mergeContext;
			}

			protected override SequenceConvertInfo? Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			{
				return null;
			}
		}
	}
}
