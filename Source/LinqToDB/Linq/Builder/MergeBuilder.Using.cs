using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.Using))]
		internal sealed class Using : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = { UsingMethodInfo1, UsingMethodInfo2 };

			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(_supportedMethods);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var sourceContext =
					builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

				var genericArgs = methodCall.Method.GetGenericArguments();

				var source = new TableLikeQueryContext(
					new ContextRefExpression(genericArgs[0], mergeContext.TargetContext, "target"),
					new ContextRefExpression(genericArgs[1], sourceContext, "source"));

				mergeContext.Sequences    = new[] { mergeContext.Sequence, source };
				mergeContext.Merge.Source = source.Source;

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
