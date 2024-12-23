using System.Linq.Expressions;

using LinqToDB.Internals.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.InlineParameters))]
	sealed class InlineParametersBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(Methods.LinqToDB.InlineParameters);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var saveInline = builder.DataContext.InlineParameters;
			builder.DataContext.InlineParameters = true;
			buildInfo.InlineParameters           = true;

			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			builder.DataContext.InlineParameters = saveInline;

			return sequence;
		}
	}
}
