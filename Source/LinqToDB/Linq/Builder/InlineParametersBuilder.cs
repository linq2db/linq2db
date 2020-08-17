using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;

	class InlineParametersBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(Methods.LinqToDB.InlineParameters);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var saveInline = builder.DataContext.InlineParameters;
			builder.DataContext.InlineParameters = true;

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			builder.DataContext.InlineParameters = saveInline;
			
			return sequence;
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression? param)
		{
			return null;
		}
	}
}
