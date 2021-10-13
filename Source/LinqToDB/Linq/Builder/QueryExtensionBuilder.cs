using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class QueryExtensionBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema).Length > 0;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var attrs = Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			return sequence;
		}
	}
}
