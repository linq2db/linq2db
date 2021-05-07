using System.Linq.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	using System.Reflection;
	using LinqToDB.Expressions;

	class PassThroughBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] SupportedMethods = new [] { Methods.Enumerable.AsQueryable, Methods.LinqToDB.AsQueryable, Methods.LinqToDB.SqlExt.Alias };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(SupportedMethods);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
