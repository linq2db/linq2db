using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

using static LinqToDB.Internal.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Internal.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.Using))]
		internal sealed class Using : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = { UsingMethodInfo1, UsingMethodInfo2 };

			public static bool CanBuildMethod(MethodCallExpression call)
				=> call.IsSameGenericMethod(_supportedMethods);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var sourceContext =
					builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

				var genericArgs = methodCall.Method.GetGenericArguments();
				var sourceRef   = new ContextRefExpression(genericArgs[1], sourceContext, "source");
				var allFields   = builder.BuildExtractExpression(sourceContext, sourceRef);

				var source = new TableLikeQueryContext(
					sourceContext.TranslationModifier,
					new ContextRefExpression(genericArgs[0], mergeContext.TargetContext, "target"),
					sourceRef);

				mergeContext.Sequences    = new[] { mergeContext.Sequence, source };
				mergeContext.Merge.Source = source.Source;

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
