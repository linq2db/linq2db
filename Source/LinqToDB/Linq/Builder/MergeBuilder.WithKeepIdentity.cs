using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;

using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.WithKeepIdentity))]
		internal sealed class WithKeepIdentity : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(WithKeepIdentityMethodInfo);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				mergeContext.Merge.KeepIdentity = true;

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
